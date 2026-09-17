using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Core.Services;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Reflection.Metadata;
using System.Text;
using ZXing;

namespace Empire_ERP.Infrastructure.Repositories
{
    public class DeliveryOrderRepository : IDeliveryOrderRepository
    {
        public IMenuRepository _menuRepository { get; set; }
        public ICommonRepository _commonRepository { get; set; }
        public ICommonService _commonService { get; set; }
        public IBranchRepository _branchRepository { get; set; }
        public IPeriodRepository _periodRepository { get; set; }

        public DeliveryOrderRepository(IMenuRepository menuRepository, IBranchRepository branchRepository, ICommonRepository commonRepository, ICommonService commonService, IPeriodRepository periodRepository)
        {
            _menuRepository = menuRepository;
            _branchRepository = branchRepository;
            _commonRepository = commonRepository;
            _commonService = commonService;
            _periodRepository = periodRepository;
        }

        public MyHttpResponseMessage GetLastRateByBarcode(CustomDeliveryOrder model, string? dctype, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table = string.Empty;
                string? detailTable = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE1;
                    detailTable = menu.TABLE2;
                }
                //var BARCODE = model.Detail.First().;
                if (!String.IsNullOrWhiteSpace(table))
                {
                    List<object> jsonDataResult = new List<object>();

                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {

                        string query = $@"DECLARE @DC_TYPE VARCHAR(5);
                            SET @DC_TYPE = 'PB';
                            DECLARE @PARTY_CODE INT = '{model.Master.PARTY_CODE}';
                            DECLARE @ACT_CODE INT = '{model.Master.ACT_CODE}';
                            DECLARE @BARCODE INT = '{model.Master.BARCODE_ID}';

                            SELECT
                                A.ITEM_CODE AS CODE,
                                '' AS BARCODE,
                                '' AS BLABEL,
                                A.ITEM_NAME AS ITEM_ID,
                                '' AS COLOR,
                                '' AS SIZE,
                                CASE
                                    WHEN @DC_TYPE IN('PB','PR') 
                                        THEN ISNULL(NULLIF(LAST_RATE.RATE,0), A.PURCHASE_RATE)

                                    WHEN @DC_TYPE IN('SB', 'SR')
                                        THEN ISNULL(NULLIF(LAST_RATE.RATE,0), A.SALE_RATE)

                                    ELSE 0
                                END AS SRATE,

                                LAST_RATE.RATE AS LAST_RATE,

                                M.PARTY_CODE,
                                M.ACT_CODE

                            FROM TBL_ITEMSMASTER A
							                          
						   OUTER APPLY(
                                SELECT TOP 1
                                    D.RATE
                                FROM TBL_SB_DETAIL D
                                INNER JOIN TBL_SB_MASTER M2
                                    ON M2.TRAN_ID = D.TRAN_ID
                                    AND M2.BCODE = D.BCODE
                                    AND M2.PERIOD_ID = D.PERIOD_ID
                                WHERE
                                    D.ITEM_CODE = A.ITEM_CODE
                                    AND M2.PARTY_CODE = @PARTY_CODE
                                    AND M2.ACT_CODE = @ACT_CODE
                                ORDER BY M2.TRAN_ID DESC
                            ) LAST_RATE



                            OUTER APPLY(
                                SELECT TOP 1
                                    M3.PARTY_CODE,
                                    M3.ACT_CODE
                                FROM TBL_SB_MASTER M3
                                WHERE
                                    M3.PARTY_CODE = @PARTY_CODE
                                    AND M3.ACT_CODE = @ACT_CODE
                                    AND EXISTS(
                                        SELECT 1
                                        FROM TBL_SB_DETAIL D3
                                        WHERE D3.TRAN_ID = M3.TRAN_ID
                                        AND D3.ITEM_CODE = A.ITEM_CODE
                                    )
                                ORDER BY M3.TRAN_ID DESC
                            ) M

                            WHERE
                                A.DLT = 'T'
                                AND A.ASTATUS = 'Y'
                                AND A.ITEM_CODE = @BARCODE;";

                        SqlCommand command = new SqlCommand(query, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            var row = new
                            {
                                rate = Convert.ToString(reader["SRATE"]),
                            };
                            jsonDataResult.Add(row);
                        }
                        reader.Close();
                    }
                    response.data = jsonDataResult;
                    response.msg = "";
                    response.msgType = 1;
                }

            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;


        }

        public MyHttpResponseMessage QuickSearch(Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table = string.Empty;
                string? detailTable = string.Empty;
                string? pickMaster = string.Empty;
                string? pickDetail = string.Empty;
                string? search = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE1;
                    detailTable = menu.TABLE2;
                    pickMaster = menu.PICK_TABLE_MASTER;
                    pickDetail = menu.PICK_TABLE_DETAIL;
                    search = menu.SEARCH;
                }

                if (!String.IsNullOrWhiteSpace(table))
                {
                    List<object> jsonDataResult = new List<object>();

                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {
                        if (search == "M")
                        {

                            string query = $@"SELECT M.TRAN_ID AS TRAN_ID, M.TRAN_ID AS CODE,M.V_DATE,M.VOUCHER_NO,M.BTYPE,
                                                M.PARTY_CODE,M.ACT_CODE,M.REF,M.REMARKS,M.BCODE,
                                                M.PERIOD_ID,M.ADD_USER_ID,M.ADD_DATE,M.ADD_COMPUTER_NAME,M.ADD_IP_ADDRESS,M.EDIT_USER_ID,
                                                M.EDIT_DATE,M.EDIT_COMPUTER_NAME,M.EDIT_IP_ADDRESS,PT.PARTY_NAME,M.ADD_POSTALCODE,M.EDIT_POSTALCODE,
                                                CASE WHEN M.ASTATUS = 'Y' THEN 'Active' ELSE 'In-Active' END AS ASTATUS,
                                                PM.TRAN_ID AS PTRAN_ID, PM.VOUCHER_NO AS PVOUCHER_NO, PM.MENU_ID AS PMENU_ID, MB.MENU_PAGE AS MENU_PAGE, MB.MENU_PARENT_CODE
                                                FROM {table} M 
                                                LEFT OUTER JOIN {detailTable} D ON D.TRAN_ID = M.TRAN_ID AND D.BCODE = M.BCODE AND D.PERIOD_ID = M.PERIOD_ID
                                                LEFT OUTER JOIN {pickDetail} PD ON PD.DT_CODE = D.PICK_ID_D AND PD.BCODE = D.BCODE AND PD.PERIOD_ID = D.PERIOD_ID
                                                LEFT OUTER JOIN {pickMaster} PM ON PM.TRAN_ID = PD.TRAN_ID AND PM.BCODE = PD.BCODE AND PM.PERIOD_ID = PD.PERIOD_ID
                                                LEFT OUTER JOIN TBL_MENU_BUILDER MB ON MB.ID = PM.MENU_ID
                                                LEFT OUTER JOIN TBL_PARTY_TYPES PT ON PT.PARTY_CODE = M.PARTY_CODE AND PT.ACT_CODE = M.ACT_CODE 
                                                WHERE  M.DLT = 'T' AND M.BCODE = '{common.Branch}' AND M.PERIOD_ID = '{common.Period}' ORDER BY M.TRAN_ID DESC";

                            SqlCommand command = new SqlCommand(query, connection);
                            connection.Open();
                            SqlDataReader reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                var row = new
                                {
                                    ID = Convert.ToInt32(reader["TRAN_ID"]),
                                    CODE = Convert.ToInt32(reader["CODE"]),
                                    ASTATUS = Convert.ToString(reader["ASTATUS"]),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                    VOUCHER_NO = Convert.ToString(reader["VOUCHER_NO"]),
                                    PARTY_CODE = reader["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PARTY_CODE"]),
                                    ACT_CODE = reader["ACT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ACT_CODE"]),
                                    REF = Convert.ToString(reader["REF"]),
                                    BTYPE = Convert.ToString(reader["BTYPE"]),
                                    REMARKS = Convert.ToString(reader["REMARKS"]),

                                    ADD_USER_ID = Convert.ToString(reader["ADD_USER_ID"]),
                                    ADD_DATE = reader["ADD_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["ADD_DATE"]).ToString("yyyy-MM-dd"),
                                    ADD_COMPUTER_NAME = Convert.ToString(reader["ADD_COMPUTER_NAME"]),
                                    ADD_IP_ADDRESS = Convert.ToString(reader["ADD_IP_ADDRESS"]),
                                    EDIT_USER_ID = Convert.ToString(reader["EDIT_USER_ID"]),
                                    EDIT_DATE = reader["EDIT_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["EDIT_DATE"]).ToString("yyyy-MM-dd"),
                                    EDIT_COMPUTER_NAME = Convert.ToString(reader["EDIT_COMPUTER_NAME"]),
                                    EDIT_IP_ADDRESS = Convert.ToString(reader["EDIT_IP_ADDRESS"]),
                                    PARTY_NAME = reader["PARTY_NAME"],
                                    ADD_POSTALCODE = Convert.ToString(reader["ADD_POSTALCODE"]),
                                    EDIT_POSTALCODE = Convert.ToString(reader["EDIT_POSTALCODE"]),
                                    PVOUCHER_NO = reader["PVOUCHER_NO"] == DBNull.Value ? "" : Convert.ToString(reader["PVOUCHER_NO"]),
                                    PTRAN_ID = reader["PTRAN_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PTRAN_ID"]),
                                    LINK = "/" + Convert.ToString(reader["MENU_PAGE"]) + "?MOID=" + Convert.ToString(reader["MENU_PARENT_CODE"]) + "&Code=" + Convert.ToString(reader["PMENU_ID"]),
                                };
                                jsonDataResult.Add(row);
                            }
                            reader.Close();


                        }
                        else
                        {

                            var query = $@"SELECT M.TRAN_ID AS TRAN_ID, M.TRAN_ID AS CODE,M.V_DATE,M.VOUCHER_NO,
                                            CASE 
                                            WHEN M.BTYPE = 'CR' THEN 'Credit' 
                                            WHEN M.BTYPE = 'C' THEN 'Cash' 
                                            WHEN M.BTYPE = 'A' THEN 'Advance' 
                                            END AS BTYPE ,
                                            IT.ITEM_NAME, W.DESCR AS WAREHOUSE, LOT.GROUP_NAME AS LOT,
                                            PT.PARTY_NAME, M.REF, D.DT_DESC,
                                            D.QTY, D.RATE, D.NET_AMT AS AMT, 
                                            CASE WHEN M.ASTATUS = 'Y' THEN 'Active' ELSE 'In-Active' END AS ASTATUS,
                                            PM.TRAN_ID AS PTRAN_ID, PM.VOUCHER_NO AS PVOUCHER_NO, PM.MENU_ID AS PMENU_ID, MB.MENU_PAGE AS MENU_PAGE, MB.MENU_PARENT_CODE
                                            FROM {table} M 
                                            LEFT OUTER JOIN TBL_PARTY_TYPES PT ON PT.PARTY_CODE = M.PARTY_CODE AND PT.ACT_CODE = M.ACT_CODE 
                                            LEFT OUTER JOIN {detailTable} D
                                            ON D.TRAN_ID = M.TRAN_ID AND D.PERIOD_ID = M.PERIOD_ID AND D.BCODE = M.BCODE
                                            LEFT OUTER JOIN {pickDetail} PD ON PD.DT_CODE = D.PICK_ID_D AND PD.BCODE = D.BCODE AND PD.PERIOD_ID = D.PERIOD_ID
                                            LEFT OUTER JOIN {pickMaster} PM ON PM.TRAN_ID = PD.TRAN_ID AND PM.BCODE = PD.BCODE AND PM.PERIOD_ID = PD.PERIOD_ID
                                            LEFT OUTER JOIN TBL_MENU_BUILDER MB ON MB.ID = PM.MENU_ID
                                            LEFT OUTER JOIN TBL_ITEMSMASTER IT ON IT.ITEM_CODE = D.ITEM_CODE
                                            LEFT OUTER JOIN TBL_WAREHOUSE W ON W.CODE = D.WAREHOUSE
                                            LEFT OUTER JOIN TBL_LOT_REG LOT ON LOT.GROUP_CODE = D.LOT_REG
                                            WHERE  M.DLT = 'T' AND M.BCODE = {common.Branch} AND M.PERIOD_ID = {common.Period} AND  D.DLT = 'T'
                                            ORDER BY M.TRAN_ID DESC";

                            SqlCommand command = new SqlCommand(query, connection);
                            connection.Open();
                            SqlDataReader reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                var row = new
                                {
                                    ID = Convert.ToInt32(reader["TRAN_ID"]),
                                    CODE = Convert.ToInt32(reader["CODE"]),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                    VOUCHER_NO = Convert.ToString(reader["VOUCHER_NO"]),
                                    BTYPE = Convert.ToString(reader["BTYPE"]),
                                    ITEM_NAME = reader["ITEM_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["ITEM_NAME"]),
                                    WAREHOUSE = reader["WAREHOUSE"] == DBNull.Value ? "" : Convert.ToString(reader["WAREHOUSE"]),
                                    LOT = reader["LOT"] == DBNull.Value ? "" : Convert.ToString(reader["LOT"]),
                                    PARTY_NAME = reader["PARTY_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["PARTY_NAME"]),
                                    REMARKS = Convert.ToString(reader["DT_DESC"]),
                                    QTY = Convert.ToString(reader["QTY"]),
                                    RATE = Convert.ToString(reader["RATE"]),
                                    AMT = Convert.ToString(reader["AMT"]),
                                    ASTATUS = Convert.ToString(reader["ASTATUS"]),
                                    PVOUCHER_NO = reader["PVOUCHER_NO"] == DBNull.Value ? "" : Convert.ToString(reader["PVOUCHER_NO"]),
                                    PTRAN_ID = reader["PTRAN_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PTRAN_ID"]),
                                    LINK = "/" + Convert.ToString(reader["MENU_PAGE"]) + "?MOID=" + Convert.ToString(reader["MENU_PARENT_CODE"]) + "&Code=" + Convert.ToString(reader["PMENU_ID"]),

                                };
                                jsonDataResult.Add(row);
                            }
                            reader.Close();
                        }
                    }

                    response.data = jsonDataResult;
                    response.msg = "";
                    response.msgType = 1;
                }
                else
                {
                    response.data = "";
                    response.msg = "Something went wrong! please try again later.";
                    response.msgType = 2;
                }
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }
        public MyHttpResponseMessage GetDeliveryOrderByCode(int code, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE1;
                }

                if (!String.IsNullOrWhiteSpace(table))
                {
                    List<object> jsonDataResult = new List<object>();
                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {
                        string query = $"SELECT TRAN_ID, V_DATE, VOUCHER_NO, BTYPE, DOC, PARTY_CODE, ACT_CODE, REF, REMARKS, BCODE, PERIOD_ID, ORDER_TYPE," +
                                       "ADD_USER_ID,ADD_DATE,ADD_COMPUTER_NAME,ADD_IP_ADDRESS," +
                                       "EDIT_USER_ID,EDIT_DATE,EDIT_COMPUTER_NAME,EDIT_IP_ADDRESS," +
                                       "ADD_POSTALCODE,EDIT_POSTALCODE,ASTATUS " +
                                       $"FROM {table} WHERE DLT = 'T' AND BCODE = '" + common.Branch + "' AND PERIOD_ID = '" + common.Period + "' AND TRAN_ID = '" + code + "'";

                        SqlCommand command = new SqlCommand(query, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            var row = new
                            {
                                ID = reader["TRAN_ID"] == DBNull.Value ? "" : Convert.ToString(reader["TRAN_ID"]),
                                ASTATUS = reader["ASTATUS"] == DBNull.Value ? "" : Convert.ToString(reader["ASTATUS"]),
                                V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                VOUCHER_NO = reader["VOUCHER_NO"] == DBNull.Value ? "" : Convert.ToString(reader["VOUCHER_NO"]),
                                PARTY_CODE = reader["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PARTY_CODE"]),
                                ACT_CODE = reader["ACT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ACT_CODE"]),
                                BTYPE = reader["BTYPE"] == DBNull.Value ? "" : Convert.ToString(reader["BTYPE"]),
                                ORDER_TYPE = reader["ORDER_TYPE"] == DBNull.Value ? "" : Convert.ToString(reader["ORDER_TYPE"]),
                                DOC = reader["DOC"] == DBNull.Value ? "" : Convert.ToString(reader["DOC"]),
                                REF = reader["REF"] == DBNull.Value ? "" : Convert.ToString(reader["REF"]),
                                REMARKS = reader["REMARKS"] == DBNull.Value ? "" : Convert.ToString(reader["REMARKS"]),

                            };
                            jsonDataResult.Add(row);
                        }
                        reader.Close();
                    }

                    response.data = jsonDataResult;
                    response.msg = "";
                    response.msgType = 1;
                }
                else
                {
                    response.data = "";
                    response.msg = "Something went wrong! please try again later.";
                    response.msgType = 2;
                }
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }

        public MyHttpResponseMessage GetDeliveryOrderDetailByCode(int code, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);

                string? tableMaster = string.Empty;
                string? tableDetail = string.Empty;
                string? pickMaster = string.Empty;
                string? pickDetail = string.Empty;

                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    tableMaster = menu.TABLE1;
                    tableDetail = menu.TABLE2;
                    pickMaster = menu.PICK_TABLE_MASTER;
                    pickDetail = menu.PICK_TABLE_DETAIL;
                }

                if (!String.IsNullOrWhiteSpace(tableMaster) && !String.IsNullOrWhiteSpace(tableDetail))
                {
                    List<object> jsonDataResult = new List<object>();
                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {

                        string query = $@"SELECT D.TRAN_ID, D.DT_CODE, D.ITEM_CODE, D.QTY, D.BAGS, D.UNIT, D.RATE, D.AMT, D.NET_AMT, D.DT_DESC, D.VEHICLE, D.DEL_DATE, 
                                            D.PAY_TERM, D.DUE_DATE, D.INC_EXC, D.PARTY_CODE, D.ACT_CODE, D.COMM_TYPE, D.COMM_VAL, D.WAREHOUSE, D.LOT_REG, D.PICK_ID, D.PICK_ID_D,
                                            PM.VOUCHER_NO, PM.TRAN_ID AS PTRAN_ID,  PM.MENU_ID AS PMENU_ID, MB.MENU_PAGE AS MENU_PAGE, MB.MENU_PARENT_CODE
                                            FROM {tableDetail} D
                                            LEFT OUTER JOIN {pickDetail} PD ON PD.DT_CODE = D.PICK_ID_D AND PD.BCODE = D.BCODE AND PD.PERIOD_ID = D.PERIOD_ID
                                            LEFT OUTER JOIN {pickMaster} PM ON PM.TRAN_ID = PD.TRAN_ID AND PM.BCODE = PD.BCODE AND PM.PERIOD_ID = PD.PERIOD_ID
                                            LEFT OUTER JOIN TBL_MENU_BUILDER MB ON MB.ID = PM.MENU_ID
                                            WHERE  D.DLT = 'T' AND D.TRAN_ID = '{code}' AND D.BCODE = '1' AND D.PERIOD_ID = '1' ORDER BY D.DT_CODE DESC";

                        SqlCommand command = new SqlCommand(query, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            var partyCodeObj = reader["PARTY_CODE"];
                            var actCodeObj = reader["ACT_CODE"];

                            string bPartyValue = (partyCodeObj != DBNull.Value && !string.IsNullOrEmpty(partyCodeObj.ToString()) &&
                                         actCodeObj != DBNull.Value && !string.IsNullOrEmpty(actCodeObj.ToString())) ? $"{partyCodeObj}0123456789{actCodeObj}" : null;

                            var row = new
                            {



                                TRAN_ID = reader["TRAN_ID"] == DBNull.Value ? "" : Convert.ToString(reader["TRAN_ID"]),
                                DT_CODE = reader["DT_CODE"] == DBNull.Value ? "" : Convert.ToString(reader["DT_CODE"]),
                                ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ITEM_CODE"]),
                                QTY = reader["QTY"] == DBNull.Value ? "" : Convert.ToString(reader["QTY"]),
                                BAGS = reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["BAGS"]),
                                UNIT = reader["UNIT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UNIT"]),
                                RATE = reader["RATE"] == DBNull.Value ? "" : Convert.ToString(reader["RATE"]),
                                AMT = reader["AMT"] == DBNull.Value ? "" : Convert.ToString(reader["AMT"]),
                                NET_AMT = reader["NET_AMT"] == DBNull.Value ? "" : Convert.ToString(reader["NET_AMT"]),
                                DT_DESC = reader["DT_DESC"] == DBNull.Value ? "" : Convert.ToString(reader["DT_DESC"]),
                                VEHICLE = reader["VEHICLE"] == DBNull.Value ? "" : Convert.ToString(reader["VEHICLE"]),
                                DEL_DATE = reader["DEL_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["DEL_DATE"]).ToString("yyyy-MM-dd"),
                                DUE_DATE = reader["DUE_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["DUE_DATE"]).ToString("yyyy-MM-dd"),
                                PAY_TERM = reader["UNIT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PAY_TERM"]),
                                INC_EXC = reader["INC_EXC"] == DBNull.Value ? "" : Convert.ToString(reader["INC_EXC"]),
                                BPARTY_CODE = reader["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PARTY_CODE"]),
                                BACT_CODE = reader["ACT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ACT_CODE"]),
                                BPARTY = bPartyValue,
                                COMM_TYPE = reader["COMM_TYPE"] == DBNull.Value ? "" : Convert.ToString(reader["COMM_TYPE"]),
                                COMM_VALUE = ((reader["COMM_VAL"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["COMM_VAL"])),
                                WAREHOUSE = reader["WAREHOUSE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["WAREHOUSE"]),
                                LOT = reader["LOT_REG"] == DBNull.Value ? 0 : Convert.ToInt32(reader["LOT_REG"]),
                                PICK_ID = reader["PICK_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PICK_ID"]),
                                PICK_ID_D = reader["PICK_ID_D"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PICK_ID_D"]),
                                VOUCHER_NO = reader["VOUCHER_NO"] == DBNull.Value ? "" : Convert.ToString(reader["VOUCHER_NO"]),
                                ID = reader["PTRAN_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PTRAN_ID"]),
                                LINK = "/" + Convert.ToString(reader["MENU_PAGE"]) + "?MOID=" + Convert.ToString(reader["MENU_PARENT_CODE"]) + "&Code=" + Convert.ToString(reader["PMENU_ID"]),
                            };
                            jsonDataResult.Add(row);
                        }
                        reader.Close();
                    }

                    response.data = jsonDataResult;
                    response.msg = "";
                    response.msgType = 1;
                }
                else
                {
                    response.data = "";
                    response.msg = "Something went wrong! please try again later.";
                    response.msgType = 2;
                }
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }

        private int GenerateNextId(Common common, SqlCommand command, string Commission = "")
        {
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE1;
                }

                if (!String.IsNullOrWhiteSpace(table))
                {
                    if (Commission == "")
                    {
                        string maxIdQuery = $"SELECT ISNULL(MAX(TRAN_ID), 0) + 1 FROM {table} WHERE BCODE = '{common.Branch}' AND PERIOD_ID = '{common.Period}'";
                        command.CommandText = maxIdQuery;
                        object result = command.ExecuteScalar();
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        string maxIdQuery = $"SELECT ISNULL(MAX(TRAN_ID), 0) + 1 FROM TBL_COMM_GEN WHERE BCODE = '{common.Branch}' AND PERIOD_ID = '{common.Period}'";
                        command.CommandText = maxIdQuery;
                        object result = command.ExecuteScalar();
                        return Convert.ToInt32(result);
                    }

                }
            }
            catch (Exception ex)
            {

            }
            return 0;
        }

        private string GenerateVoucherNo(Common common, int code, string vDate)
        {
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? prefix = string.Empty, shortName = string.Empty;
                int voucherLength = 0;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    voucherLength = Convert.ToInt32(menu.VOUCHER_LEN);
                    prefix = menu.PERFIX;
                }

                var branchData = _branchRepository.GetBranchByCode(common.Branch);
                if (branchData.data != null)
                {
                    var branch = (Branch)branchData.data;
                    shortName = branch.B_SHORT_NAME;
                }

                if (!String.IsNullOrWhiteSpace(shortName) && !String.IsNullOrWhiteSpace(prefix) && voucherLength > 0 && code > 0)
                {
                    //string paddedVoucherValue = "0".ToString().PadLeft(voucherLength - 1, '0') + code;
                    string paddedVoucherValue = code.ToString().PadLeft(voucherLength, '0');
                    return $"{shortName}/{prefix}/{Convert.ToDateTime(vDate).ToString("yy-MM")}/{paddedVoucherValue}";
                }
            }
            catch (Exception ex)
            {

            }
            return string.Empty;
        }

        private int GenerateNextDetailId(Common common, SqlCommand command, string Commission = "")
        {
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE2;
                }

                if (!String.IsNullOrWhiteSpace(table))
                {
                    if (Commission == "")
                    {
                        string maxIdQuery = $"SELECT ISNULL(MAX(DT_CODE), 0) FROM {table}";
                        command.CommandText = maxIdQuery;
                        object result = command.ExecuteScalar();
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        string maxIdQuery = $"SELECT ISNULL(MAX(DT_CODE), 0) + 1 FROM TBL_COMM_GEN";
                        command.CommandText = maxIdQuery;
                        object result = command.ExecuteScalar();
                        return Convert.ToInt32(result);
                    }

                }
            }
            catch (Exception ex)
            {

            }
            return 0;
        }

        public MyHttpResponseMessage Save(CustomDeliveryOrder modelRecord, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table = string.Empty, detailTable = string.Empty, b_i = string.Empty, stk_status = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE1;
                    detailTable = menu.TABLE2;
                    b_i = menu.B_I;
                    stk_status = menu.STK_STATUS;
                }

                if (!String.IsNullOrWhiteSpace(table) && !String.IsNullOrWhiteSpace(detailTable))
                {
                    var Ip = common.IPAddress;
                    var Computer = common.ComputerName;
                    var Postal = common.PostalCode;
                    var username = common.Username;
                    var branch = common.Branch;
                    var period = common.Period;
                    var periodInfo = _periodRepository.GetPeriodById(Convert.ToInt32(period));
                    string startDate = ((Period)periodInfo.data).START_D.Value.ToString("yyyy-MM-dd");
                    string endDate = ((Period)periodInfo.data).START_E.Value.ToString("yyyy-MM-dd");
                    var menuID = common.MenuID;
                    string connectionString = new SQLService().getconnstring();
                    Dictionary<int?, (double? Balance, string ItemName)> stockBalance = new Dictionary<int?, (double?, string)>();

                    if (stk_status == "Y")
                    {
                        string stkQuery = $@"EXEC STKPROC 71,'{startDate}','{endDate}','{common.Branch}',{common.Period},'','','','',''";

                        using (SqlConnection stkconnection = new SqlConnection(connectionString))
                        {
                            SqlCommand command = new SqlCommand(stkQuery, stkconnection);
                            stkconnection.Open();

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int? itemCode = reader["ITEM_ID"] as int?;
                                    double? balance = reader["BALANCE"] == DBNull.Value ? 0 : Convert.ToDouble(reader["BALANCE"]);
                                    string itemName = reader["BARCODE"] == DBNull.Value ? string.Empty : reader["BARCODE"].ToString();

                                    if (itemCode != null)
                                    {
                                        if (!stockBalance.ContainsKey(itemCode))
                                        {
                                            stockBalance.Add(itemCode, (balance, itemName));
                                        }
                                        else
                                        {
                                            stockBalance[itemCode] = (balance, itemName);
                                        }
                                    }
                                }
                            }
                        }
                    }


                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        SqlTransaction transaction = connection.BeginTransaction();
                        SqlCommand command = connection.CreateCommand();
                        command.Transaction = transaction;
                        try
                        {

                            string query = "", commQuery = "", detailQuery = "", voucherNo = string.Empty;
                            bool IsMasterAdded = true, IsNew = false;
                            int code = 0;
                            if (modelRecord.Master.TRAN_ID == null || modelRecord.Master.TRAN_ID == 0)
                            {
                                IsNew = true;
                                code = GenerateNextId(common, command);

                                if (code > 0)
                                {
                                    modelRecord.Master.TRAN_ID = code;
                                    voucherNo = GenerateVoucherNo(common, code, CommonService.GetDateTime("Pakistan Standard Time"));
                                    if (String.IsNullOrWhiteSpace(voucherNo))
                                    {
                                        IsMasterAdded = false;
                                    }
                                }
                                else
                                {
                                    IsMasterAdded = false;
                                }

                                query = $"INSERT INTO {table}" +
                                        "(TRAN_ID, V_DATE, VOUCHER_NO, PARTY_CODE, ACT_CODE, BTYPE, REF, REMARKS, BCODE, PERIOD_ID, ORDER_TYPE," +
                                        "ADD_USER_ID, ADD_DATE, ADD_COMPUTER_NAME, ADD_IP_ADDRESS, EDIT_USER_ID, EDIT_DATE," +
                                        "EDIT_COMPUTER_NAME, EDIT_IP_ADDRESS, ADD_POSTALCODE, EDIT_POSTALCODE, ASTATUS, MENU_ID, DLT, DOC)" +
                                        "VALUES" +
                                        "('" + code + "','" + modelRecord.Master.V_DATE + "','" + voucherNo + "','" + modelRecord.Master.PARTY_CODE + "','" + modelRecord.Master.ACT_CODE + "'," +
                                        "'" + modelRecord.Master.BTYPE + "','" + modelRecord.Master.REF + "','" + modelRecord.Master.REMARKS + "','" + branch + "','" + period + "','" + modelRecord.Master.ORDER_TYPE + "'," +
                                        "'" + username + "','" + CommonService.GetDateTime("Pakistan Standard Time") + "','" + Computer + "'," +
                                        "'" + Ip + "','" + username + "','" + CommonService.GetDateTime("Pakistan Standard Time") + "'," +
                                        "'" + Computer + "','" + Ip + "','" + Postal + "','" + Postal + "','" + modelRecord.Master.ASTATUS + "','" + menuID + "','T','" + modelRecord.Master.DOC + "')";
                                command.CommandText = query;
                                command.ExecuteNonQuery();
                            }
                            else
                            {
                                query = $"UPDATE {table} SET V_DATE = '" + modelRecord.Master.V_DATE + @"',
                                                    PARTY_CODE = '" + modelRecord.Master.PARTY_CODE + @"',
                                                    ACT_CODE = '" + modelRecord.Master.ACT_CODE + @"',
                                                    ORDER_TYPE = '" + modelRecord.Master.ORDER_TYPE + @"',
                                                    BTYPE = '" + modelRecord.Master.BTYPE + @"',
                                                    REF = '" + modelRecord.Master.REF + @"',
                                                    REMARKS = '" + modelRecord.Master.REMARKS + @"',
                                                    DOC = '" + modelRecord.Master.DOC + @"',
                                                    EDIT_USER_ID = '" + username + @"',
                                                    EDIT_DATE = '" + CommonService.GetDateTime("Pakistan Standard Time") + @"',
                                                    EDIT_COMPUTER_NAME = '" + Computer + @"',
                                                    EDIT_IP_ADDRESS = '" + Ip + @"',
                                                    EDIT_POSTALCODE = '" + Postal + @"',
                                                    ASTATUS = '" + modelRecord.Master.ASTATUS + @"'
                                                    WHERE TRAN_ID = '" + modelRecord.Master.TRAN_ID + "' AND BCODE = '" + branch + "' AND PERIOD_ID = '" + period + "'";
                                command.CommandText = query;
                                command.ExecuteNonQuery();
                            }

                            var isDetailAdded = true;

                            StringBuilder insertQueryBuilder = new StringBuilder();
                            StringBuilder insertCommQueryBuilder = new StringBuilder();
                            StringBuilder updateQueryBuilder = new StringBuilder();
                            StringBuilder updateCommQueryBuilder = new StringBuilder();

                            bool allowInserts = true;
                            bool hasInserts = false;
                            bool hasUpdates = false;
                            int detailCode = GenerateNextDetailId(common, command);
                            foreach (var item in modelRecord.Detail.ToList())
                            {
                                try
                                {
                                    var amt = item.QTY * item.RATE;
                                    item.AMT = amt;

                                    if (item.RATE > 0 && (item.AMT == null || item.AMT == 0))
                                    {
                                        response.msg = "Something went wrong";
                                        response.msgType = 2;
                                        return response;
                                    }

                                    if (item.DT_CODE == null || item.DT_CODE == 0)
                                    {
                                        detailCode++;
                                        if (detailCode > 0)
                                        {
                                            if (!hasInserts)
                                            {
                                                hasInserts = true;
                                            }

                                            if (stk_status == "Y")
                                            {
                                                if (item.ITEM_CODE != null)
                                                {
                                                    double procQty = 0;
                                                    string itemName = "";

                                                    if (stockBalance.ContainsKey(item.ITEM_CODE))
                                                    {
                                                        procQty = stockBalance[item.ITEM_CODE].Balance ?? 0;
                                                        itemName = stockBalance[item.ITEM_CODE].ItemName;
                                                    }

                                                    if ((item.QTY ?? 0) > procQty)
                                                    {
                                                        isDetailAdded = false;
                                                        response.msg = $@"Stock unavailable in {itemName}";
                                                        response.msgType = 2;
                                                        return response;
                                                    }

                                                    double totalSum = (item.QTY ?? 0) + procQty;
                                                }
                                            }

                                            insertQueryBuilder.AppendLine(
                                                $"INSERT INTO {detailTable} (TRAN_ID, DT_CODE, ITEM_CODE, PARTY_CODE, ACT_CODE, QTY, COMP, " +
                                                $"UNIT, BAGS, RATE, AMT, NET_AMT, DT_DESC, DEL_DATE, PAY_TERM, DUE_DATE, INC_EXC, COMM_TYPE, COMM_VAL, WAREHOUSE, LOT_REG, VEHICLE, BCODE, PERIOD_ID, ADD_USER_ID, " +
                                                $"ADD_DATE, ADD_COMPUTER_NAME, ADD_IP_ADDRESS, EDIT_USER_ID, " +
                                                $"EDIT_DATE, EDIT_COMPUTER_NAME, EDIT_IP_ADDRESS, " +
                                                $"ADD_POSTALCODE, EDIT_POSTALCODE, MENU_ID, DLT, PICK_ID, PICK_ID_D) VALUES " +
                                                $"('{modelRecord.Master.TRAN_ID}', '{detailCode}', '{item.ITEM_CODE}', '{item.BPARTY_CODE}', '{item.BACT_CODE}','{item.QTY}', '0', " +
                                                $"'{item.UNIT}', '{item.BAGS}', '{item.RATE}', '{item.AMT}', '{item.AMT}', '{item.DT_DESC}', " +
                                                $"'{item.DEL_DATE}', '{item.PAY_TERM}', '{item.DUE_DATE}','{item.INC_EXC}', '{item.COMM_TYPE}', '{item.COMM_VALUE}', '{item.WAREHOUSE}', '{item.LOT}', '{item.VEHICLE}', '{branch}', '{period}', '{username}', " +
                                                $"'{CommonService.GetDateTime("Pakistan Standard Time")}', '{Computer}', '{Ip}', '{username}', " +
                                                $"'{CommonService.GetDateTime("Pakistan Standard Time")}', '{Computer}', '{Ip}', " +
                                                $"'{Postal}', '{Postal}', '{menuID}', 'T', '{item.PICK_ID}', '{item.PICK_ID_D}');");
                                        }
                                        else
                                        {
                                            isDetailAdded = false;
                                        }
                                    }
                                    else
                                    {
                                        if (!hasUpdates)
                                        {
                                            hasUpdates = true;
                                        }

                                        updateQueryBuilder.AppendLine(
                                            $"UPDATE {detailTable} SET " +
                                            $"ITEM_CODE = '{item.ITEM_CODE}', " +
                                            $"PARTY_CODE = '{item.BPARTY_CODE}', " +
                                            $"ACT_CODE = '{item.BACT_CODE}', " +
                                            $"QTY = '{item.QTY}', " +
                                            $"UNIT = '{item.UNIT}', " +
                                            $"BAGS = '{item.BAGS}', " +
                                            $"RATE = '{item.RATE}', " +
                                            $"AMT = '{item.AMT}', " +
                                            $"NET_AMT = '{item.NET_AMT}', " +
                                            $"DT_DESC = '{item.DT_DESC}', " +
                                            $"DEL_DATE = '{item.DEL_DATE}', " +
                                            $"PAY_TERM = '{item.PAY_TERM}', " +
                                            $"DUE_DATE = '{item.DUE_DATE}', " +
                                            $"INC_EXC = '{item.INC_EXC}', " +
                                            $"COMM_TYPE = '{item.COMM_TYPE}'," +
                                            $"COMM_VAL = '{item.COMM_VALUE}'," +
                                            $"WAREHOUSE = '{item.WAREHOUSE}', " +
                                            $"LOT_REG = '{item.LOT}', " +
                                            $"VEHICLE = '{item.VEHICLE}', " +
                                            $"EDIT_USER_ID = '{username}', " +
                                            $"EDIT_DATE = '{CommonService.GetDateTime("Pakistan Standard Time")}', " +
                                            $"EDIT_COMPUTER_NAME = '{Computer}', " +
                                            $"EDIT_IP_ADDRESS = '{Ip}', " +
                                            $"EDIT_POSTALCODE = '{Postal}', " +
                                            $"DLT = 'T' " +
                                            $"WHERE TRAN_ID = '{modelRecord.Master.TRAN_ID}' AND DT_CODE = '{item.DT_CODE}' " +
                                            $"AND BCODE = '{branch}' AND PERIOD_ID = '{period}';");
                                    }
                                }
                                catch (Exception)
                                {
                                    isDetailAdded = false;
                                }
                            }



                            if (hasInserts)
                            {
                                command.CommandText = insertQueryBuilder.ToString() + ";" + insertCommQueryBuilder.ToString();
                                command.ExecuteNonQuery();
                            }
                            if (hasUpdates)
                            {
                                command.CommandText = updateQueryBuilder.ToString() + ";" + updateCommQueryBuilder.ToString();
                                command.ExecuteNonQuery();
                            }
                            if (IsMasterAdded && isDetailAdded)
                            {
                                transaction.Commit();
                                response.data = new
                                {
                                    code = IsNew ? code : modelRecord.Master.TRAN_ID,
                                    voucherNo = IsNew ? voucherNo : modelRecord.Master.VOUCHER_NO,
                                };
                                response.msgType = 1;
                                response.msg = IsNew ? "Record Added Successfully" : "Record Updated Successfully";
                            }
                            else
                            {
                                transaction.Rollback();
                                response.data = "";
                                response.msg = "Something went wrong! please try again later.";
                                response.msgType = 2;
                            }


                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            string _catchMessage = ex.Message;
                            if (ex.InnerException != null)
                            {
                                _catchMessage += "<br/>" + ex.InnerException.Message;
                            }
                            response.msg = _catchMessage;
                            response.msgType = 2;
                        }
                    }

                }
                else
                {
                    response.data = "";
                    response.msg = "Something went wrong! please try again later.";
                    response.msgType = 2;
                }
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msgType = 2;
                response.msg = _catchMessage;
            }
            return response;
        }


        public MyHttpResponseMessage Delete(int code, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            response.msgType = 2;
            response.msg = "Data not found in our records";
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table = string.Empty;
                string? detailTable = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE1;
                    detailTable = menu.TABLE2;
                }

                if (!String.IsNullOrWhiteSpace(table))
                {
                    if (code == 0)
                    {
                        response.msg = "ID is not in numeric format";
                        response.msgType = 2;
                    }
                    else
                    {
                        string connectionString = new SQLService().getconnstring();
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = $"UPDATE {table} SET DLT = 'F' WHERE TRAN_ID = '{code}' AND BCODE = '{common.Branch}' AND PERIOD_ID = '{common.Period}'";
                            query += $" UPDATE {detailTable} SET DLT = 'F' WHERE TRAN_ID = '{code}' AND BCODE = '{common.Branch}' AND PERIOD_ID = '{common.Period}'";
                            SqlCommand command = new SqlCommand(query, connection);
                            command.ExecuteNonQuery();
                            response.msgType = 1;
                            response.msg = "Record Deleted Successfully";
                        }
                    }
                }
                else
                {
                    response.data = "";
                    response.msg = "Something went wrong! please try again later.";
                    response.msgType = 2;
                }
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }

        public MyHttpResponseMessage CopyRecord(CopyRecord record, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            response.msgType = 2;
            response.msg = "Data not found in our records";
            try
            {
                string? table = menu.TABLE1;
                string? table2 = menu.TABLE2;
                string connectionString = new SQLService().getconnstring();

                DeliveryOrder DeliveryOrder = new DeliveryOrder();
                List<DeliveryOrderDetail> DeliveryOrderDetailList = new List<DeliveryOrderDetail>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = $@"SELECT * FROM {table} WHERE TRAN_ID = {record.TRAN_ID} AND DLT = 'T' AND BCODE = {common.Branch} AND PERIOD_ID = {common.Period}";
                    string detailQuery = $@"SELECT * FROM {table2} WHERE TRAN_ID = {record.TRAN_ID} AND DLT = 'T' AND BCODE = {common.Branch} AND PERIOD_ID = {common.Period}";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        DeliveryOrder = new DeliveryOrder
                        {
                            V_DATE = Convert.ToDateTime(reader["V_DATE"]),
                            PARTY_CODE = Convert.ToInt32(reader["PARTY_CODE"]),
                            ACT_CODE = Convert.ToInt32(reader["ACT_CODE"]),
                            SCODE = Convert.ToInt32(reader["SCODE"]),
                            SACODE = Convert.ToInt32(reader["SACODE"]),
                            COMM = Convert.ToDouble(reader["COMM"]),
                            REF = Convert.ToString(reader["REF"]),
                            REMARKS = Convert.ToString(reader["REMARKS"]),
                            BTYPE = Convert.ToString(reader["BTYPE"]),
                            BCODE = Convert.ToInt32(reader["BCODE"]),
                            ASTATUS = Convert.ToString(reader["ASTATUS"]),
                            DISC = Convert.ToDouble(reader["DISC"]),
                            //HS_CODE = Convert.ToString(reader["HS_CODE"]),
                            DOC = Convert.ToString(reader["DOC"]),
                            CURR_CODE = Convert.ToInt32(reader["CURR_CODE"]),
                            CRATE = Convert.ToDouble(reader["CRATE"]),
                        };
                    }

                    reader.Close();

                    SqlCommand detail_Command = new SqlCommand(detailQuery, connection);
                    SqlDataReader detail_Reader = detail_Command.ExecuteReader();
                    while (detail_Reader.Read())
                    {
                        var row = new DeliveryOrderDetail
                        {
                            ITEM_CODE = Convert.ToInt32(detail_Reader["ITEM_CODE"]),
                            QTY = Convert.ToDouble(detail_Reader["QTY"]),
                            HS_CODE = Convert.ToString(detail_Reader["HS_CODE"]),
                            UNIT = Convert.ToInt32(detail_Reader["UNIT"]),
                            QTY2 = Convert.ToDouble(detail_Reader["QTY2"]),
                            BAL_QTY = Convert.ToDouble(detail_Reader["BAL_QTY"]),
                            RATE = Convert.ToDouble(detail_Reader["RATE"]),
                            AMT = Convert.ToDouble(detail_Reader["AMT"]),
                            DISC = Convert.ToDouble(detail_Reader["DISC"]),
                            DISC_AMT = Convert.ToDouble(detail_Reader["DISC_AMT"]),
                            TAX = Convert.ToDouble(detail_Reader["TAX"]),
                            TAX_AMT = Convert.ToDouble(detail_Reader["TAX_AMT"]),
                            NET_AMT = Convert.ToDouble(detail_Reader["NET_AMT"]),
                            DT_DESC = Convert.ToString(detail_Reader["DT_DESC"]),
                            COLOR = Convert.ToInt32(detail_Reader["COLOR"]),
                            SIZE = Convert.ToInt32(detail_Reader["SIZE"]),
                            GRADE = Convert.ToInt32(detail_Reader["GRADE"]),
                            WAREHOUSE = Convert.ToInt32(detail_Reader["WAREHOUSE"]),
                            DEL_DATE = Convert.ToDateTime(detail_Reader["DEL_DATE"]),
                            DUE_DATE = Convert.ToDateTime(detail_Reader["DUE_DATE"]),
                            DUE_DAYS = Convert.ToInt32(detail_Reader["DUE_DAYS"]),
                            VEH = Convert.ToString(detail_Reader["VEH"]),
                            CHK = detail_Reader["CHK"] == DBNull.Value ? 0 : Convert.ToInt32(detail_Reader["CHK"]),
                            PICK_ID = Convert.ToInt32(detail_Reader["PICK_ID"]),
                            PICK_ID_D = detail_Reader["PICK_ID_D"] == DBNull.Value ? 0 : Convert.ToInt32(detail_Reader["PICK_ID_D"]),
                            ADV = Convert.ToDouble(detail_Reader["ADV"]),
                            ADV_AMT = Convert.ToDouble(detail_Reader["ADV_AMT"]),
                            BPARTY_CODE = Convert.ToInt32(detail_Reader["PARTY_CODE"]),
                            BACT_CODE = Convert.ToInt32(detail_Reader["ACT_CODE"]),
                        };
                        DeliveryOrderDetailList.Add(row);
                    }

                    detail_Reader.Close();
                    connection.Close();
                }

                var customRequisition = new CustomDeliveryOrder
                {
                    Master = DeliveryOrder,
                    Detail = DeliveryOrderDetailList
                };

                response = this.Save(customRequisition, common);

                if (response.msgType == 1)
                {
                    response.msg = "Record Copied Successfully";
                }

            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }

        public MyHttpResponseMessage GetPickDataByParty(int partyCode, int actCode, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table = string.Empty;
                string? detailTable = string.Empty;
                string? pickTable = string.Empty;
                string? pickDetailTable = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE1;
                    detailTable = menu.TABLE2;
                    pickTable = menu.PICK_TABLE_MASTER;
                    pickDetailTable = menu.PICK_TABLE_DETAIL;
                }

                if (!String.IsNullOrWhiteSpace(pickTable))
                {
                    List<object> jsonDataResult = new List<object>();
                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {


                        string query = $@"SELECT 
                                                A.TRAN_ID, A.V_DATE AS LB_DATE, A.VOUCHER_NO,
                                                A.PARTY_CODE, I.HS_CODE, A.ACT_CODE, AC.PARTY_NAME AS PARTY_NAME, A.REF, A.BTYPE, A.ORDER_TYPE,
                                         A.REMARKS,
                                                -- Issue Qty
                                                ISNULL((SELECT SUM(ISNULL(QTY,0)) FROM {pickDetailTable} WHERE DT_CODE = B.DT_CODE), 0) AS ISSUE_QTY,
                                                -- Returned Qty
                                                ISNULL((SELECT SUM(ISNULL(QTY,0)) FROM {detailTable} D
                                                        LEFT OUTER JOIN {table} M ON D.TRAN_ID = M.TRAN_ID
                                                        WHERE D.PICK_ID_D = B.DT_CODE AND D.DLT = 'T' AND M.DLT = 'T'), 0) AS R_QTY,
                                                -- Balance Qty
                                                ISNULL((SELECT SUM(ISNULL(QTY,0)) FROM {pickDetailTable} WHERE DT_CODE = B.DT_CODE), 0) - 
                                                ISNULL((SELECT SUM(ISNULL(QTY,0)) FROM {detailTable} D
                                                        LEFT OUTER JOIN {table} M ON D.TRAN_ID = M.TRAN_ID
                                                        WHERE D.PICK_ID_D = B.DT_CODE AND D.DLT = 'T' AND M.DLT = 'T'), 0) AS B_QTY,
                                                U.GROUP_CODE AS UNIT, U.GROUP_NAME AS UNIT_NAME, B.RATE, B.AMT,
                                                B.QTY, B.DT_DESC,
                                                B.DT_CODE, B.TAX, B.TAX_AMT , B.NET_AMT,
                                               W.DESCR AS WAREHOUSE_NAME, LOT.GROUP_NAME AS LOT_NAME,
                                               B.ITEM_CODE, B.BAGS, I.ITEM_NAME, B.DEL_DATE, B.PAY_TERM, B.DUE_DATE, B.INC_EXC, B.PARTY_CODE AS BPARTY_CODE, B.ACT_CODE AS BACT_CODE,
											   B.COMM_TYPE, B.COMM_VAL, B.WAREHOUSE, B.LOT_REG, MB.MENU_PAGE, MB.MENU_PARENT_CODE, A.MENU_ID, B.VEHICLE
                                            FROM {pickTable} A
                                            LEFT OUTER JOIN {pickDetailTable} B ON B.TRAN_ID = A.TRAN_ID AND B.DLT = 'T' AND B.BCODE = A.BCODE AND B.PERIOD_ID = A.PERIOD_ID
                                            LEFT OUTER JOIN {detailTable} VC ON VC.PICK_ID_D = B.DT_CODE AND VC.BCODE = B.BCODE AND VC.DLT = 'T' AND VC.PERIOD_ID = B.PERIOD_ID
                                            LEFT OUTER JOIN {table} SBM ON VC.TRAN_ID = SBM.TRAN_ID AND SBM.DLT = 'T' 
                                            LEFT OUTER JOIN TBL_PARTY_TYPES AC ON AC.PARTY_CODE = A.PARTY_CODE AND AC.ACT_CODE = A.ACT_CODE
                                            LEFT OUTER JOIN TBL_WAREHOUSE W ON W.CODE = B.WAREHOUSE
											LEFT OUTER JOIN TBL_LOT_REG LOT ON LOT.GROUP_CODE = B.LOT_REG
                                            LEFT OUTER JOIN TBL_UNIT U ON U.GROUP_CODE = B.UNIT
                                            LEFT OUTER JOIN TBL_ITEMSMASTER I ON I.ITEM_CODE = B.ITEM_CODE
                                            LEFT OUTER JOIN TBL_MENU_BUILDER MB ON MB.ID = A.MENU_ID
                                            WHERE A.BCODE = '{common.Branch}' 
                                              AND A.PERIOD_ID = '{common.Period}' 
                                              AND B.COMP = 0 
                                              AND ('{actCode}' = '0' OR AC.ACT_CODE = '{actCode}')
											  AND ('{partyCode}' = '0' OR AC.PARTY_CODE = '{partyCode}')
                                              AND A.DLT = 'T' AND A.ASTATUS = 'Y' AND B.DLT = 'T'
                                            GROUP BY 
                                                A.TRAN_ID, A.V_DATE, A.VOUCHER_NO, A.PARTY_CODE,I.HS_CODE,B.QTY, A.BTYPE, A.ORDER_TYPE, B.VEHICLE,
                                                W.DESCR, LOT.GROUP_NAME,
                                                A.ACT_CODE, AC.PARTY_NAME, A.REF, U.GROUP_CODE, U.GROUP_NAME, B.RATE, B.AMT,
                                                B.DT_CODE, B.TAX, B.TAX_AMT, B.NET_AMT,
                                                B.ITEM_CODE, B.BAGS, B.DEL_DATE, B.PAY_TERM, B.DUE_DATE, B.INC_EXC, B.PARTY_CODE, B.ACT_CODE, 
											   B.COMM_TYPE, B.COMM_VAL, B.WAREHOUSE, B.LOT_REG,
                                                I.ITEM_NAME, MB.MENU_PAGE, MB.MENU_PARENT_CODE, A.MENU_ID, VC.PICK_ID_D, A.REMARKS, B.DT_DESC
                                                        order by DT_CODE desc";

                        SqlCommand command = new SqlCommand(query, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            var partyCodeObj = reader["BPARTY_CODE"];
                            var actCodeObj = reader["BACT_CODE"];

                            string bPartyValue = (partyCodeObj != DBNull.Value && !string.IsNullOrEmpty(partyCodeObj.ToString()) &&
                                         actCodeObj != DBNull.Value && !string.IsNullOrEmpty(actCodeObj.ToString())) ? $"{partyCodeObj}0123456789{actCodeObj}" : null;

                            var row = new
                            {
                                ID = Convert.ToString(reader["TRAN_ID"]),
                                LB_DATE = ((reader["LB_DATE"] == DBNull.Value) ? null : Convert.ToDateTime(reader["LB_DATE"]).ToString("dd-MM-yyyy")),
                                VOUCHER_NO = ((reader["VOUCHER_NO"] == DBNull.Value) ? "" : Convert.ToString(reader["VOUCHER_NO"])),
                                VEHICLE = ((reader["VEHICLE"] == DBNull.Value) ? "" : Convert.ToString(reader["VEHICLE"])),
                                BTYPE = ((reader["BTYPE"] == DBNull.Value) ? "" : Convert.ToString(reader["BTYPE"])),
                                ORDER_TYPE = ((reader["ORDER_TYPE"] == DBNull.Value) ? "" : Convert.ToString(reader["ORDER_TYPE"])),
                                PARTY_CODE = ((reader["PARTY_CODE"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["PARTY_CODE"])),
                                ACT_CODE = ((reader["ACT_CODE"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["ACT_CODE"])),
                                PARTY_DDL = Convert.ToString(reader["PARTY_CODE"]) + Convert.ToString(reader["ACT_CODE"]),
                                PARTY_NAME = ((reader["PARTY_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["PARTY_NAME"])),
                                REMARKS = ((reader["REMARKS"] == DBNull.Value) ? "" : Convert.ToString(reader["REMARKS"])),
                                DT_DESC = ((reader["DT_DESC"] == DBNull.Value) ? "" : Convert.ToString(reader["DT_DESC"])),
                                REF = ((reader["REF"] == DBNull.Value) ? "" : Convert.ToString(reader["REF"])),
                                HS_CODE = ((reader["HS_CODE"] == DBNull.Value) ? "" : Convert.ToString(reader["HS_CODE"])),
                                IQTY = ((reader["ISSUE_QTY"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["ISSUE_QTY"])),
                                TOTAL_PACK = ((reader["B_QTY"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["B_QTY"])),
                                RQTY = ((reader["R_QTY"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["R_QTY"])),
                                UNIT = ((reader["UNIT"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["UNIT"])),
                                UNIT_NAME = ((reader["UNIT_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["UNIT_NAME"])),
                                RATE = ((reader["RATE"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["RATE"])),
                                TAX = ((reader["TAX"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["TAX"])),
                                TAX_AMT = ((reader["TAX_AMT"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["TAX_AMT"])),
                                QTY = ((reader["B_QTY"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["B_QTY"])),
                                IssueQty = ((reader["B_QTY"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["B_QTY"])),
                                AMT = ((reader["AMT"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["AMT"])),
                                PICK_ID_D = ((reader["DT_CODE"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["DT_CODE"])),
                                PICK_ID = ((reader["TRAN_ID"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["TRAN_ID"])),
                                ITEM_CODE = ((reader["ITEM_CODE"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["ITEM_CODE"])),
                                ITEM_NAME = ((reader["ITEM_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["ITEM_NAME"])),
                                WAREHOUSE_NAME = ((reader["WAREHOUSE_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["WAREHOUSE_NAME"])),
                                LOT_NAME = ((reader["LOT_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["LOT_NAME"])),
                                LINK = "/" + Convert.ToString(reader["MENU_PAGE"]) + "?MOID=" + Convert.ToString(reader["MENU_PARENT_CODE"]) + "&Code=" + Convert.ToString(reader["MENU_ID"]),

                                PAY_TERM = ((reader["PAY_TERM"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["PAY_TERM"])),
                                //DEL_DATE = reader["DEL_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["DEL_DATE"]).ToString("yyyy-MM-dd"),
                                //DUE_DATE = reader["DUE_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["DUE_DATE"]).ToString("yyyy-MM-dd"),

                                DEL_DATE = reader["DEL_DATE"] == DBNull.Value || Convert.ToDateTime(reader["DEL_DATE"]).Year <= 1900 ? null : Convert.ToDateTime(reader["DEL_DATE"]).ToString("dd-MMM-yy"),
                                DUE_DATE = reader["DUE_DATE"] == DBNull.Value || Convert.ToDateTime(reader["DUE_DATE"]).Year <= 1900 ? null : Convert.ToDateTime(reader["DUE_DATE"]).ToString("dd-MMM-yy"),

                                INC_EXC = ((reader["INC_EXC"] == DBNull.Value) ? "" : Convert.ToString(reader["INC_EXC"])),
                                BPARTY = bPartyValue,
                                BPARTY_CODE = reader["BPARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["BPARTY_CODE"]),
                                BACT_CODE = reader["BACT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["BACT_CODE"]),
                                COMM_TYPE = ((reader["COMM_TYPE"] == DBNull.Value) ? "" : Convert.ToString(reader["COMM_TYPE"])),
                                COMM_VALUE = ((reader["COMM_VAL"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["COMM_VAL"])),
                                BAGS = ((reader["BAGS"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["BAGS"])),
                                WAREHOUSE = ((reader["WAREHOUSE"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["WAREHOUSE"])),
                                LOT = ((reader["LOT_REG"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["LOT_REG"])),



                            };
                            jsonDataResult.Add(row);
                        }
                        reader.Close();
                    }

                    response.data = jsonDataResult;
                    response.msg = "";
                    response.msgType = 1;
                }
                else
                {
                    response.data = "";
                    response.msg = "Something went wrong! please try again later.";
                    response.msgType = 2;
                }
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }
        public MyHttpResponseMessage DeleteDeliveryOrderDetailByCode(int code, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            response.msgType = 2;
            response.msg = "Data not found in our records";
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE2;
                }
                if (!String.IsNullOrWhiteSpace(table))
                {
                    var branch = common.Branch;
                    var period = common.Period;
                    if (code == 0)
                    {
                        response.msg = "ID is not in numeric format";
                        response.msgType = 2;
                    }
                    else
                    {
                        string connectionString = new SQLService().getconnstring();
                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = $"UPDATE {table} SET DLT = 'F'" +
                                $" WHERE DT_CODE = '{code}' AND BCODE = '{branch}' AND PERIOD_ID = '{period}'";
                            SqlCommand command = new SqlCommand(query, connection);
                            command.ExecuteNonQuery();
                            response.msgType = 1;
                            response.msg = "Record Deleted Successfully";
                        }
                    }
                }
                else
                {
                    response.data = "";
                    response.msg = "Something went wrong! please try again later.";
                    response.msgType = 2;
                }
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }

        public MyHttpResponseMessage GetDataForReport(PurchaseBillRDLCReport modelRecord, DataTable dataTable, CustomMenuDetail menuDetails, Company currentCompany, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            PurchaseBillRDLCReport masterData = new PurchaseBillRDLCReport();
            CustomPurchaseBillForPrintReport reportData = new CustomPurchaseBillForPrintReport();
            var Menu = _menuRepository.GetMenu(common.MenuID);
            string? table = string.Empty, detailTable = string.Empty;
            string? pickMaster = string.Empty, pickDetail = string.Empty;
            if (Menu.data != null)
            {
                var menu = (Menu)Menu.data;
                table = menu.TABLE1;
                detailTable = menu.TABLE2;
                pickMaster = menu.PICK_TABLE_MASTER;
                pickDetail = menu.PICK_TABLE_DETAIL;
            }
            try
            {
                string query = $@"EXEC PROC_PRINT '{table}','{detailTable}','{pickMaster}','{pickDetail}','{common.Branch}','{common.Period}','{modelRecord.TRAN_ID}','','','{menuDetails.REPORT_NAME}'";

                using (SqlConnection connection7 = new SqlConnection(new SQLService().getconnstring()))
                {
                    SqlCommand sqlCommand7 = new SqlCommand(query, connection7);
                    connection7.Open();
                    SqlDataReader reader5 = sqlCommand7.ExecuteReader();
                    masterData.HEADER_NAME = (menuDetails.MD_NAME ?? "");
                    masterData.REPORT_NAME = (menuDetails.REPORT_NAME ?? "");
                    masterData.COMPANY_LOGO = currentCompany.C_LOGO;
                    masterData.RCOMPANY_LOGO = currentCompany.RC_LOGO;
                    if (reader5.Read())
                    {
                        masterData.COMPANY_NAME = ((reader5["C_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader5["C_NAME"]));
                        masterData.B_NAME = ((reader5["B_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader5["B_NAME"]));
                        masterData.B_TERMS = ((reader5["B_TERMS"] == DBNull.Value) ? "" : Convert.ToString(reader5["B_TERMS"]));
                        masterData.COMPANY_ADDRESS = ((reader5["B_ADDRESS"] == DBNull.Value) ? "" : Convert.ToString(reader5["B_ADDRESS"]));
                        masterData.COMPANY_PHONE = ((reader5["B_TEL"] == DBNull.Value) ? "" : Convert.ToString(reader5["B_TEL"]));
                        masterData.B_WEBSITE = ((reader5["B_WEBSITE"] == DBNull.Value) ? "" : Convert.ToString(reader5["B_WEBSITE"]));
                        masterData.EMAIL = ((reader5["EMAIL"] == DBNull.Value) ? "" : Convert.ToString(reader5["EMAIL"]));
                        masterData.B_GST = ((reader5["B_GST"] == DBNull.Value) ? "" : Convert.ToString(reader5["B_GST"]));
                        masterData.B_NTN = ((reader5["B_NTN"] == DBNull.Value) ? "" : Convert.ToString(reader5["B_NTN"]));
                        masterData.SIG1 = ((reader5["MENU_SIG1"] == DBNull.Value) ? "" : Convert.ToString(reader5["MENU_SIG1"]));
                        masterData.SIG2 = ((reader5["MENU_SIG2"] == DBNull.Value) ? "" : Convert.ToString(reader5["MENU_SIG2"]));
                        masterData.SIG3 = ((reader5["MENU_SIG3"] == DBNull.Value) ? "" : Convert.ToString(reader5["MENU_SIG3"]));
                        masterData.SIG4 = ((reader5["MENU_SIG4"] == DBNull.Value) ? "" : Convert.ToString(reader5["MENU_SIG4"]));
                        masterData.MENU_TERMS = ((reader5["MENU_TERMS"] == DBNull.Value) ? "" : Convert.ToString(reader5["MENU_TERMS"]));
                        masterData.INVOICE_NUMBER = Convert.ToString(reader5["VOUCHER_NO"]);
                        masterData.DATE = reader5["V_DATE"] == DBNull.Value || Convert.ToDateTime(reader5["V_DATE"]).Year <= 1900 ? null : Convert.ToDateTime(reader5["V_DATE"]).ToString("dd-MMM-yy");
                        masterData.USER = common.Username;
                        masterData.STATUS = Convert.ToString(reader5["ASTATUS"]);
                        masterData.BT_CUSTOMER = ((reader5["BT_CUSTOMER"] == DBNull.Value) ? "" : Convert.ToString(reader5["BT_CUSTOMER"]));
                        //masterData.ORDER_TYPE = ((reader5["ORDER_TYPE"] == DBNull.Value) ? "" : Convert.ToString(reader5["ORDER_TYPE"]));
                        masterData.PARTY_NAME = ((reader5["PARTY_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader5["PARTY_NAME"]));
                        masterData.PADDRESS = ((reader5["PADDRESS"] == DBNull.Value) ? "" : Convert.ToString(reader5["PADDRESS"]));
                        masterData.VEHICLE = ((reader5["VEHICLE"] == DBNull.Value) ? "" : Convert.ToString(reader5["VEHICLE"]));
                        masterData.WAREHOUSE = ((reader5["WAREHOUSE"] == DBNull.Value) ? "" : Convert.ToString(reader5["WAREHOUSE"]));
                        masterData.REF = ((reader5["REF"] == DBNull.Value) ? "" : Convert.ToString(reader5["REF"]));
                        masterData.COMMENT = ((reader5["REMARKS"] == DBNull.Value) ? "" : Convert.ToString(reader5["REMARKS"]));
                        masterData.PNTN = ((reader5["PNTN"] == DBNull.Value) ? "" : Convert.ToString(reader5["PNTN"]));
                        masterData.DEL_DATE = reader5["DEL_DATE"] == DBNull.Value || Convert.ToDateTime(reader5["DEL_DATE"]).Year <= 1900 ? null : Convert.ToDateTime(reader5["DEL_DATE"]).ToString("dd-MMM-yy");
                        masterData.DUE_DATE = reader5["DUE_DATE"] == DBNull.Value || Convert.ToDateTime(reader5["DUE_DATE"]).Year <= 1900 ? null : Convert.ToDateTime(reader5["DUE_DATE"]).ToString("dd-MMM-yy");
                        masterData.PICK_DATE = reader5["SO_DATE"] == DBNull.Value || Convert.ToDateTime(reader5["SO_DATE"]).Year <= 1900 ? null : Convert.ToDateTime(reader5["SO_DATE"]).ToString("dd-MMM-yy");
                        masterData.PAY_TERM = ((reader5["PAY_TERM"] == DBNull.Value) ? 0 : Convert.ToInt32(reader5["PAY_TERM"]));
                        masterData.PICK_VOUCHER = ((reader5["SO_VOUCHER_NO"] == DBNull.Value) ? "" : Convert.ToString(reader5["SO_VOUCHER_NO"]));
                        masterData.BROKER = ((reader5["BROKER"] == DBNull.Value) ? "" : Convert.ToString(reader5["BROKER"]));
                        masterData.PAY_TYPE = ((reader5["PAY_TYPE"] == DBNull.Value) ? "" : Convert.ToString(reader5["PAY_TYPE"]));

                    }
                    reader5.Close();
                }
                using (SqlConnection connection8 = new SqlConnection(new SQLService().getconnstring()))
                {
                    SqlCommand sqlCommand8 = new SqlCommand(query, connection8);
                    connection8.Open();
                    SqlDataReader reader6 = sqlCommand8.ExecuteReader();
                    while (reader6.Read())
                    {
                        DataRow dataRow3 = dataTable.NewRow();
                        dataRow3["Item"] = reader6["ITEM"]?.ToString() ?? "";
                        dataRow3["Qty"] = reader6["QTY"]?.ToString() ?? "";
                        dataRow3["Bags"] = reader6["BAGS"]?.ToString() ?? "";
                        dataRow3["Unit"] = reader6["UNIT"]?.ToString() ?? "";
                        dataRow3["Rate"] = reader6["RATE"]?.ToString() ?? "";
                        dataRow3["Amount"] = reader6["AMT"]?.ToString() ?? "";
                        dataRow3["Desc"] = reader6["DT_DESC"]?.ToString() ?? "";
                        dataRow3["LOT"] = reader6["LOT"] == DBNull.Value ? "" : Convert.ToString(reader6["LOT"]);
                        dataRow3["WAREHOUSE"] = reader6["WAREHOUSE"] == DBNull.Value ? "" : Convert.ToString(reader6["WAREHOUSE"]);
                        dataRow3["PICK_TRANS"] = reader6["PICK_VOUCHER"]?.ToString() ?? "";
                        dataRow3["DelDate"] = reader6["DEL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader6["DEL_DATE"]).ToString("dd-MMM-yy") : "";
                        dataRow3["DueDate"] = reader6["DUE_DATE"] != DBNull.Value ? Convert.ToDateTime(reader6["DUE_DATE"]).ToString("dd-MMM-yy") : "";

                        dataTable.Rows.Add(dataRow3);
                    }
                    reader6.Close();
                }

                reportData.Master = masterData;
                reportData.Detail = dataTable;
                response.data = reportData;
                response.msg = "";
                response.msgType = 1;
            }
            catch (Exception ex)
            {
                string _catchMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    _catchMessage += "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }


    }
}
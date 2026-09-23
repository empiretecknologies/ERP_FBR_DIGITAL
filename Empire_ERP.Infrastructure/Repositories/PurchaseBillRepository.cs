using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Core.Services;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Reflection.Metadata;
using System.Text;
using ZXing;

namespace Empire_ERP.Infrastructure.Repositories
{
    public class PurchaseBillRepository : IPurchaseBillRepository
    {
        public IMenuRepository _menuRepository { get; set; }
        public ICommonRepository _commonRepository { get; set; }
        public ICommonService _commonService { get; set; }
        public IBranchRepository _branchRepository { get; set; }
        public IPeriodRepository _periodRepository { get; set; }

        public PurchaseBillRepository(IMenuRepository menuRepository, IBranchRepository branchRepository, ICommonRepository commonRepository, ICommonService commonService, IPeriodRepository periodRepository)
        {
            _menuRepository = menuRepository;
            _branchRepository = branchRepository;
            _commonRepository = commonRepository;
            _commonService = commonService;
            _periodRepository = periodRepository;
        }

        public MyHttpResponseMessage GetLastRateByBarcode(CustomPurchaseBill model, string? dctype, Common common)
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
                string? search = string.Empty;
                string? pickMaster = string.Empty;
                string? pickDetail = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE1;
                    detailTable = menu.TABLE2;
                    search = menu.SEARCH;
                    pickMaster = menu.PICK_TABLE_MASTER;
                    pickDetail = menu.PICK_TABLE_DETAIL;
                }

                if (!String.IsNullOrWhiteSpace(table))
                {
                    List<object> jsonDataResult = new List<object>();

                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {
                        if (search == "M")
                        {

                            string query = $@"SELECT M.TRAN_ID AS TRAN_ID,
                                                M.V_DATE,
                                                M.VOUCHER_NO,
                                                PT.PARTY_NAME,
                                                CASE 
                                                WHEN M.BTYPE = 'CR' THEN 'Credit' WHEN M.BTYPE = 'C' THEN 'Cash' 
                                                WHEN M.BTYPE = 'A' THEN 'Advance' 
                                                END AS BTYPE ,
                                                M.FBR_NO,
                                                M.REF,
                                                M.REMARKS,
                                                CASE WHEN M.ASTATUS = 'Y' THEN 'Active' ELSE 'In-Active' END AS ASTATUS
                                                FROM {table} M 
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
                                    CODE = Convert.ToInt32(reader["TRAN_ID"]),
                                    ASTATUS = Convert.ToString(reader["ASTATUS"]),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                    VOUCHER_NO = Convert.ToString(reader["VOUCHER_NO"]),
                                    FBR_NO = reader["FBR_NO"] == DBNull.Value ? "" : Convert.ToString(reader["FBR_NO"]),
                                    REF = Convert.ToString(reader["REF"]),
                                    BTYPE = Convert.ToString(reader["BTYPE"]),
                                    REMARKS = Convert.ToString(reader["REMARKS"]),
                                    PARTY_NAME = reader["PARTY_NAME"],
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
                                            CASE WHEN M.ASTATUS = 'Y' THEN 'Active' ELSE 'In-Active' END AS ASTATUS ,
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
        public MyHttpResponseMessage GetPurchaseBillByCode(int code, Common common)
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
                        string query = $"SELECT TRAN_ID, V_DATE, VOUCHER_NO, BTYPE, DOC, PARTY_CODE, ACT_CODE, REF, REMARKS, ORDER_TYPE, BCODE, PERIOD_ID," +
                                       "ADD_USER_ID,ADD_DATE,ADD_COMPUTER_NAME,ADD_IP_ADDRESS," +
                                       "EDIT_USER_ID,EDIT_DATE,EDIT_COMPUTER_NAME,EDIT_IP_ADDRESS," +
                                       "ADD_POSTALCODE,EDIT_POSTALCODE,ASTATUS,FBR_NO " +
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
                                FBR_NO = Convert.ToString(reader["FBR_NO"]),
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

        public MyHttpResponseMessage GetPurchaseBillDetailByCode(int code, Common common)
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

                        string query = $@"SELECT D.TRAN_ID, D.DT_CODE, D.ITEM_CODE, D.QTY, D.BAGS, D.UNIT, D.RATE, D.AMT, D.W_RATE, D.W_AMT, D.TAX, D.TAX_AMT, D.NET_AMT, D.DT_DESC, D.VEHICLE, D.DEL_DATE, D.HS_CODE,
                                            D.PAY_TERM, D.DUE_DATE, D.INC_EXC, D.PARTY_CODE, D.ACT_CODE, D.COMM_TYPE, D.COMM_VAL, D.WAREHOUSE, D.LOT_REG, D.PICK_ID, D.PICK_ID_D,
                                            PM.VOUCHER_NO, PM.TRAN_ID AS PTRAN_ID,  PM.MENU_ID AS PMENU_ID, MB.MENU_PAGE AS MENU_PAGE, MB.MENU_PARENT_CODE,D.FBR_TYPE,D.SCHEDULE_NO,D.SERIAL_NO,D.ITEM_SNO
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
                                HS_CODE = reader["HS_CODE"] == DBNull.Value ? "" : Convert.ToString(reader["HS_CODE"]),

                                BAGS = reader["BAGS"] == DBNull.Value ? 0 : Convert.ToInt32(reader["BAGS"]),
                                UNIT = reader["UNIT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UNIT"]),

                                RATE = reader["RATE"] == DBNull.Value ? "" : Convert.ToString(reader["RATE"]),
                                AMT = reader["AMT"] == DBNull.Value ? "" : Convert.ToString(reader["AMT"]),
                                W_RATE = reader["W_RATE"] == DBNull.Value ? "" : Convert.ToString(reader["W_RATE"]),
                                W_AMT = reader["W_AMT"] == DBNull.Value ? "" : Convert.ToString(reader["W_AMT"]),
                                TAX = reader["TAX"] == DBNull.Value ? "" : Convert.ToString(reader["TAX"]),
                                TAX_AMT = reader["TAX_AMT"] == DBNull.Value ? "" : Convert.ToString(reader["TAX_AMT"]),
                                NET_AMT = reader["NET_AMT"] == DBNull.Value ? "" : Convert.ToString(reader["NET_AMT"]),

                                DT_DESC = reader["DT_DESC"] == DBNull.Value ? "" : Convert.ToString(reader["DT_DESC"]),
                                VEHICLE = reader["VEHICLE"] == DBNull.Value ? "" : Convert.ToString(reader["VEHICLE"]),

                                DEL_DATE = reader["DEL_DATE"] == DBNull.Value
    ? null
    : Convert.ToDateTime(reader["DEL_DATE"]).ToString("yyyy-MM-dd"),

                                DUE_DATE = reader["DUE_DATE"] == DBNull.Value
    ? null
    : Convert.ToDateTime(reader["DUE_DATE"]).ToString("yyyy-MM-dd"),

                                PAY_TERM = reader["PAY_TERM"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PAY_TERM"]),

                                FBR_TYPE = reader["FBR_TYPE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["FBR_TYPE"]),

                                INC_EXC = reader["INC_EXC"] == DBNull.Value ? "" : Convert.ToString(reader["INC_EXC"]),

                                BPARTY_CODE = reader["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PARTY_CODE"]),
                                BACT_CODE = reader["ACT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ACT_CODE"]),

                                BPARTY = bPartyValue,

                                COMM_TYPE = reader["COMM_TYPE"] == DBNull.Value ? "" : Convert.ToString(reader["COMM_TYPE"]),
                                SCHEDULE_NO = reader["SCHEDULE_NO"] == DBNull.Value ? "" : Convert.ToString(reader["SCHEDULE_NO"]),
                                SERIAL_NO = reader["SERIAL_NO"] == DBNull.Value ? "" : Convert.ToString(reader["SERIAL_NO"]),
                                ITEM_SNO = reader["ITEM_SNO"] == DBNull.Value ? "" : Convert.ToString(reader["ITEM_SNO"]),

                                COMM_VALUE = reader["COMM_VAL"] == DBNull.Value
    ? 0.0
    : Convert.ToDouble(reader["COMM_VAL"]),

                                WAREHOUSE = reader["WAREHOUSE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["WAREHOUSE"]),
                                LOT = reader["LOT_REG"] == DBNull.Value ? 0 : Convert.ToInt32(reader["LOT_REG"]),
                                PICK_ID = reader["PICK_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PICK_ID"]),
                                PICK_ID_D = reader["PICK_ID_D"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PICK_ID_D"]),

                                VOUCHER_NO = reader["VOUCHER_NO"] == DBNull.Value
    ? ""
    : Convert.ToString(reader["VOUCHER_NO"]),

                                ID = reader["PTRAN_ID"] == DBNull.Value
    ? 0
    : Convert.ToInt32(reader["PTRAN_ID"]),

                                LINK = "/" + Convert.ToString(reader["MENU_PAGE"])
    + "?MOID=" + Convert.ToString(reader["MENU_PARENT_CODE"])
    + "&Code=" + Convert.ToString(reader["PMENU_ID"]),
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

        public MyHttpResponseMessage GetPurchaseBillChargesByCode(int code, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);

                List<object> jsonDataResult = new List<object>();
                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {

                    string query = $@"SELECT CODE, TRAN_ID, ACT_CODE, SIGN, AMT FROM TBL_CHARGES_ADDED WHERE DLT = 'T' AND TRAN_ID = '{code}' AND MENU_ID = '{common.MenuID}' ";

                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new
                        {
                            CODE = reader["CODE"] == DBNull.Value ? "" : Convert.ToString(reader["CODE"]),
                            TRAN_ID = reader["TRAN_ID"] == DBNull.Value ? "" : Convert.ToString(reader["TRAN_ID"]),
                            CHARGES_CODE = reader["ACT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ACT_CODE"]),
                            SIGN = reader["SIGN"] == DBNull.Value ? "" : Convert.ToString(reader["SIGN"]),
                            AMT = reader["AMT"] == DBNull.Value ? 0 : Convert.ToDouble(reader["AMT"]),
                        };
                        jsonDataResult.Add(row);
                    }
                    reader.Close();
                }

                response.data = jsonDataResult;
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

        public MyHttpResponseMessage Save(CustomPurchaseBill modelRecord, Common common)
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


                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        SqlTransaction transaction = connection.BeginTransaction();
                        SqlCommand command = connection.CreateCommand();
                        command.Transaction = transaction;
                        try
                        {
                            //if (modelRecord.Master.SCODE != 0 && modelRecord.Master.SCODE != null)
                            //{
                            //    string maxIdQuery1 = "SELECT SACT_CODE FROM TBL_PARTY_TYPES WHERE PARTY_CODE = '" + modelRecord.Master.PARTY_CODE + "' AND SACT_CODE IS NOT NULL";
                            //    using (SqlConnection connectionNew = new SqlConnection(new SQLService().getconnstring()))
                            //    {
                            //        SqlCommand commandNew1 = new SqlCommand(maxIdQuery1, connectionNew);
                            //        connectionNew.Open();
                            //        object result1 = commandNew1.ExecuteScalar();
                            //        modelRecord.Master.SACODE = result1 == DBNull.Value ? 0 : Convert.ToInt32(result1);
                            //    }
                            //}
                            //else
                            //{
                            //    modelRecord.Master.SACODE = 0;
                            //}

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
                                        "(TRAN_ID, V_DATE, VOUCHER_NO, PARTY_CODE, ACT_CODE, BTYPE, REF, ORDER_TYPE, REMARKS, BCODE, PERIOD_ID," +
                                        "ADD_USER_ID, ADD_DATE, ADD_COMPUTER_NAME, ADD_IP_ADDRESS, EDIT_USER_ID, EDIT_DATE," +
                                        "EDIT_COMPUTER_NAME, EDIT_IP_ADDRESS, ADD_POSTALCODE, EDIT_POSTALCODE, ASTATUS, MENU_ID, DLT, DOC)" +
                                        "VALUES" +
                                        "('" + code + "','" + modelRecord.Master.V_DATE + "','" + voucherNo + "','" + modelRecord.Master.PARTY_CODE + "','" + modelRecord.Master.ACT_CODE + "'," +
                                        "'" + modelRecord.Master.BTYPE + "','" + modelRecord.Master.REF + "','" + modelRecord.Master.ORDER_TYPE + "','" + modelRecord.Master.REMARKS + "','" + branch + "','" + period + "'," +
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
                                                    BTYPE = '" + modelRecord.Master.BTYPE + @"',
                                                    ORDER_TYPE = '" + modelRecord.Master.ORDER_TYPE + @"',
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

                                            insertQueryBuilder.AppendLine(
                                                $"INSERT INTO {detailTable} (TRAN_ID, DT_CODE, ITEM_CODE, PARTY_CODE, ACT_CODE, QTY, COMP, VEHICLE," +
                                                $"UNIT, BAGS, RATE, AMT, W_RATE, W_AMT, TAX, TAX_AMT, NET_AMT, DT_DESC, DEL_DATE, PAY_TERM, DUE_DATE, INC_EXC, COMM_TYPE, COMM_VAL, WAREHOUSE, LOT_REG, HS_CODE, BCODE, PERIOD_ID, ADD_USER_ID, " +
                                                $"ADD_DATE, ADD_COMPUTER_NAME, ADD_IP_ADDRESS, EDIT_USER_ID, " +
                                                $"EDIT_DATE, EDIT_COMPUTER_NAME, EDIT_IP_ADDRESS, " +
                                                $"ADD_POSTALCODE, EDIT_POSTALCODE, MENU_ID, DLT, PICK_ID, PICK_ID_D,FBR_TYPE,SCHEDULE_NO,SERIAL_NO,ITEM_SNO) VALUES " +
                                                $"('{modelRecord.Master.TRAN_ID}', '{detailCode}', '{item.ITEM_CODE}', '{item.BPARTY_CODE}', '{item.BACT_CODE}','{item.QTY}', '0', '{item.VEHICLE}'," +
                                                $"'{item.UNIT}', '{item.BAGS}', '{item.RATE}', '{item.AMT}', '{item.W_RATE}', '{item.W_AMT}', '{item.TAX ?? 0}', '{item.TAX_AMT ?? 0}', '{item.NET_AMT ?? item.AMT}', '{item.DT_DESC}', " +
                                                $"'{item.DEL_DATE}', '{item.PAY_TERM}', '{item.DUE_DATE}','{item.INC_EXC}', '{item.COMM_TYPE}', '{item.COMM_VALUE}', '{item.WAREHOUSE}', '{item.LOT}', '{item.HS_CODE}', '{branch}', '{period}', '{username}', " +
                                                $"'{CommonService.GetDateTime("Pakistan Standard Time")}', '{Computer}', '{Ip}', '{username}', " +
                                                $"'{CommonService.GetDateTime("Pakistan Standard Time")}', '{Computer}', '{Ip}', " +
                                                $"'{Postal}', '{Postal}', '{menuID}', 'T', '{item.PICK_ID}', '{item.PICK_ID_D}','{item.FBR_TYPE}','{item.SCHEDULE_NO}','{item.SERIAL_NO}','{item.ITEM_SNO}');");
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
                                            $"HS_CODE = '{item.HS_CODE}', " +
                                            $"RATE = '{item.RATE}', " +
                                            $"AMT = '{item.AMT}', " +
                                            $"W_RATE = '{item.W_RATE}', " +
                                            $"W_AMT = '{item.W_AMT}', " +
                                            $"TAX = '{item.TAX ?? 0}', " +
                                            $"TAX_AMT = '{item.TAX_AMT ?? 0}', " +
                                            $"NET_AMT = '{item.NET_AMT ?? item.AMT}', " +
                                            $"DT_DESC = '{item.DT_DESC}', " +
                                            $"DEL_DATE = '{item.DEL_DATE}', " +
                                            $"PAY_TERM = '{item.PAY_TERM}', " +
                                            $"DUE_DATE = '{item.DUE_DATE}', " +
                                            $"INC_EXC = '{item.INC_EXC}', " +
                                            $"VEHICLE = '{item.VEHICLE}', " +
                                            $"COMM_TYPE = '{item.COMM_TYPE}'," +
                                            $"COMM_VAL = '{item.COMM_VALUE}'," +
                                            $"WAREHOUSE = '{item.WAREHOUSE}', " +
                                            $"LOT_REG = '{item.LOT}', " +
                                            $"FBR_TYPE = '{item.FBR_TYPE}', " +
                                            $"SCHEDULE_NO = '{item.SCHEDULE_NO}', " +
                                            $"SERIAL_NO = '{item.SERIAL_NO}', " +
                                            $"ITEM_SNO = '{item.ITEM_SNO}', " +
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

        public MyHttpResponseMessage SaveCharges(ChargesModel modelRecord, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            string connectionString = new SQLService().getconnstring();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                SqlCommand command = connection.CreateCommand();
                command.Transaction = transaction;

                try
                {
                    foreach (var item in modelRecord.Charges)
                    {
                        string query = "";
                        if (string.IsNullOrEmpty(item.CODE) || item.CODE == "0")
                        {


                            int nextCode = GenerateNextChargesCode(command, common);

                            query = @$"INSERT INTO TBL_CHARGES_ADDED ([CODE], [TRAN_ID], [ACT_CODE], [SIGN], [AMT], [MENU_ID],  [DLT]) 
                                    VALUES ('{nextCode}', '{modelRecord.TRAN_ID}', '{item.CHARGES_CODE}', '{item.SIGN}', '{item.AMT}', '{common.MenuID}', 'T');";
                        }
                        else
                        {
                            query = @$"UPDATE TBL_CHARGES_ADDED SET 
                                    [ACT_CODE] = '{item.CHARGES_CODE}', 
                                    [SIGN] = '{item.SIGN}',
                                    [AMT] = '{item.AMT}'
                                    WHERE [CODE] = '{item.CODE}' 
                                    AND [MENU_ID] = '{common.MenuID}' 
                                    AND [TRAN_ID] = '{modelRecord.TRAN_ID}';";
                        }

                        command.CommandText = query;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    response.msg = "Charges Saved Successfully";
                    response.msgType = 1;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    response.msg = ex.Message;
                    response.msgType = 2;
                }
            }
            return response;
        }

        private int GenerateNextChargesCode(SqlCommand command, Common common)
        {
            try
            {
                string table = "TBL_CHARGES_ADDED";
                string maxIdQuery = $"SELECT ISNULL(MAX(CODE), 0) + 1 FROM {table}";

                command.CommandText = maxIdQuery;
                object result = command.ExecuteScalar();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                return 0;
            }
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

        public MyHttpResponseMessage DeleteCharges(int code, int tranId, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            response.msgType = 2;
            response.msg = "Data not found in our records";
            try
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
                        string query = $"UPDATE TBL_CHARGES_ADDED SET DLT = 'F' WHERE CODE = '{code}' AND TRAN_ID = '{tranId}'";
                        SqlCommand command = new SqlCommand(query, connection);
                        command.ExecuteNonQuery();
                        response.msgType = 1;
                        response.msg = "Record Deleted Successfully";
                    }
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

                PurchaseBill PurchaseBill = new PurchaseBill();
                List<PurchaseBillDetail> PurchaseBillDetailList = new List<PurchaseBillDetail>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = $@"SELECT * FROM {table} WHERE TRAN_ID = {record.TRAN_ID} AND DLT = 'T' AND BCODE = {common.Branch} AND PERIOD_ID = {common.Period}";
                    string detailQuery = $@"SELECT * FROM {table2} WHERE TRAN_ID = {record.TRAN_ID} AND DLT = 'T' AND BCODE = {common.Branch} AND PERIOD_ID = {common.Period}";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        PurchaseBill = new PurchaseBill
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
                        var row = new PurchaseBillDetail
                        {
                            ITEM_CODE = Convert.ToInt32(detail_Reader["ITEM_CODE"]),
                            QTY = Convert.ToDouble(detail_Reader["QTY"]),
                            HS_CODE = Convert.ToString(detail_Reader["HS_CODE"]),
                            UNIT = Convert.ToInt32(detail_Reader["UNIT"]),
                            QTY2 = Convert.ToDouble(detail_Reader["QTY2"]),
                            BAL_QTY = Convert.ToDouble(detail_Reader["BAL_QTY"]),
                            RATE = Convert.ToDouble(detail_Reader["RATE"]),
                            AMT = Convert.ToDouble(detail_Reader["AMT"]),
                            W_RATE = detail_Reader["W_RATE"] == DBNull.Value ? 0 : Convert.ToDouble(detail_Reader["W_RATE"]),
                            W_AMT = detail_Reader["W_AMT"] == DBNull.Value ? 0 : Convert.ToDouble(detail_Reader["W_AMT"]),
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
                        PurchaseBillDetailList.Add(row);
                    }

                    detail_Reader.Close();
                    connection.Close();
                }

                var customRequisition = new CustomPurchaseBill
                {
                    Master = PurchaseBill,
                    Detail = PurchaseBillDetailList
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
                                                A.PARTY_CODE, I.HS_CODE, A.ACT_CODE, AC.PARTY_NAME AS PARTY_NAME, A.REF, A.BTYPE, A.ORDER_TYPE, A.DOC,
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
                                               B.ITEM_CODE, B.BAGS, I.ITEM_NAME, A.V_DATE AS DEL_DATE, B.PAY_TERM, B.DUE_DATE, B.INC_EXC, B.PARTY_CODE AS BPARTY_CODE, B.ACT_CODE AS BACT_CODE, B.VEHICLE,
											   B.COMM_TYPE, B.COMM_VAL, B.WAREHOUSE, B.LOT_REG, MB.MENU_PAGE, MB.MENU_PARENT_CODE, A.MENU_ID
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
                                              --AND AC.PARTY_CODE = '{partyCode}' 
                                              --AND AC.ACT_CODE = '{actCode}' 
                                              AND ('{actCode}' = '0' OR AC.ACT_CODE = '{actCode}')
											  AND ('{partyCode}' = '0' OR AC.PARTY_CODE = '{partyCode}')
                                              AND A.DLT = 'T' AND A.ASTATUS = 'Y' AND B.DLT = 'T'
                                            GROUP BY 
                                                A.TRAN_ID, A.V_DATE, A.VOUCHER_NO, A.PARTY_CODE,I.HS_CODE,B.QTY, A.BTYPE, A.DOC, A.ORDER_TYPE,
                                                W.DESCR, LOT.GROUP_NAME,
                                                A.ACT_CODE, AC.PARTY_NAME, A.REF, U.GROUP_CODE, U.GROUP_NAME, B.RATE, B.AMT,
                                                B.DT_CODE, B.TAX, B.TAX_AMT, B.NET_AMT,
                                                B.ITEM_CODE, B.BAGS, B.DEL_DATE, B.PAY_TERM, B.DUE_DATE, B.INC_EXC, B.PARTY_CODE, B.ACT_CODE, B.VEHICLE,
											   B.COMM_TYPE, B.COMM_VAL, B.WAREHOUSE, B.LOT_REG,
                                                I.ITEM_NAME, MB.MENU_PAGE, MB.MENU_PARENT_CODE, A.MENU_ID, VC.PICK_ID_D, A.REMARKS, B.DT_DESC
                                            HAVING 
                                                (ISNULL((SELECT SUM(ISNULL(QTY,0)) FROM {pickDetailTable} WHERE DT_CODE = B.DT_CODE), 0) -
								            ISNULL((SELECT SUM(ISNULL(QTY,0)) FROM {detailTable} D
										     LEFT JOIN {table} M ON D.TRAN_ID = M.TRAN_ID
										     WHERE D.PICK_ID_D = B.DT_CODE AND D.DLT = 'T' AND M.DLT = 'T'), 0)) <> 0
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


                            // 1. Safe Values Parsing
                            int payTerms = (reader["PAY_TERM"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["PAY_TERM"]);

                            DateTime? deliveryDate = (reader["DEL_DATE"] == DBNull.Value || Convert.ToDateTime(reader["DEL_DATE"]).Year <= 1900)
                                ? null
                                : Convert.ToDateTime(reader["DEL_DATE"]);



                            var row = new
                            {
                                ID = Convert.ToString(reader["TRAN_ID"]),
                                LB_DATE = ((reader["LB_DATE"] == DBNull.Value) ? null : Convert.ToDateTime(reader["LB_DATE"]).ToString("dd-MM-yyyy")),
                                VOUCHER_NO = ((reader["VOUCHER_NO"] == DBNull.Value) ? "" : Convert.ToString(reader["VOUCHER_NO"])),
                                BTYPE = ((reader["BTYPE"] == DBNull.Value) ? "" : Convert.ToString(reader["BTYPE"])),
                                ORDER_TYPE = ((reader["ORDER_TYPE"] == DBNull.Value) ? "" : Convert.ToString(reader["ORDER_TYPE"])),
                                PARTY_CODE = ((reader["PARTY_CODE"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["PARTY_CODE"])),
                                ACT_CODE = ((reader["ACT_CODE"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["ACT_CODE"])),
                                PARTY_DDL = Convert.ToString(reader["PARTY_CODE"]) + Convert.ToString(reader["ACT_CODE"]),
                                VEHICLE = ((reader["VEHICLE"] == DBNull.Value) ? "" : Convert.ToString(reader["VEHICLE"])),
                                PARTY_NAME = ((reader["PARTY_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["PARTY_NAME"])),
                                REF = ((reader["REF"] == DBNull.Value) ? "" : Convert.ToString(reader["REF"])),
                                HS_CODE = ((reader["HS_CODE"] == DBNull.Value) ? "" : Convert.ToString(reader["HS_CODE"])),
                                REMARKS = ((reader["REMARKS"] == DBNull.Value) ? "" : Convert.ToString(reader["REMARKS"])),
                                DT_DESC = ((reader["DT_DESC"] == DBNull.Value) ? "" : Convert.ToString(reader["DT_DESC"])),
                                IQTY = ((reader["ISSUE_QTY"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["ISSUE_QTY"])),
                                TOTAL_PACK = ((reader["B_QTY"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["B_QTY"])),
                                RQTY = ((reader["R_QTY"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["R_QTY"])),
                                UNIT = ((reader["UNIT"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["UNIT"])),
                                UNIT_NAME = ((reader["UNIT_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["UNIT_NAME"])),
                                RATE = ((reader["RATE"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["RATE"])),
                                TAX = ((reader["TAX"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["TAX"])),
                                TAX_AMT = ((reader["TAX_AMT"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["TAX_AMT"])),
                                QTY = ((reader["B_QTY"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["B_QTY"])),
                                AMT = ((reader["AMT"] == DBNull.Value) ? 0.0 : Convert.ToDouble(reader["AMT"])),
                                PICK_ID_D = ((reader["DT_CODE"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["DT_CODE"])),
                                PICK_ID = ((reader["TRAN_ID"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["TRAN_ID"])),
                                ITEM_CODE = ((reader["ITEM_CODE"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["ITEM_CODE"])),
                                ITEM_NAME = ((reader["ITEM_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["ITEM_NAME"])),
                                WAREHOUSE_NAME = ((reader["WAREHOUSE_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["WAREHOUSE_NAME"])),
                                LOT_NAME = ((reader["LOT_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["LOT_NAME"])),
                                LINK = "/" + Convert.ToString(reader["MENU_PAGE"]) + "?MOID=" + Convert.ToString(reader["MENU_PARENT_CODE"]) + "&Code=" + Convert.ToString(reader["MENU_ID"]),

                                PAY_TERM = ((reader["PAY_TERM"] == DBNull.Value) ? 0 : Convert.ToInt32(reader["PAY_TERM"])),
                                DEL_DATE = reader["DEL_DATE"] == DBNull.Value || Convert.ToDateTime(reader["DEL_DATE"]).Year <= 1900 ? null : Convert.ToDateTime(reader["DEL_DATE"]).ToString("yyyy-MM-dd"),
                                //DUE_DATE = reader["DUE_DATE"] == DBNull.Value || Convert.ToDateTime(reader["DUE_DATE"]).Year <= 1900 ? null : Convert.ToDateTime(reader["DUE_DATE"]).ToString("yyyy-MM-dd"),

                                DUE_DATE = deliveryDate.HasValue ? deliveryDate.Value.AddDays(payTerms).ToString("yyyy-MM-dd") : null,


                                INC_EXC = ((reader["INC_EXC"] == DBNull.Value) ? "" : Convert.ToString(reader["INC_EXC"])),
                                BPARTY = bPartyValue,
                                BPARTY_CODE = reader["BPARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["BPARTY_CODE"]),
                                BACT_CODE = reader["BACT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["BACT_CODE"]),
                                COMM_TYPE = ((reader["COMM_TYPE"] == DBNull.Value) ? "" : Convert.ToString(reader["COMM_TYPE"])),
                                DOC = ((reader["DOC"] == DBNull.Value) ? "" : Convert.ToString(reader["DOC"])),
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
        public MyHttpResponseMessage DeletePurchaseBillDetailByCode(int code, Common common)
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

                var query = $@"EXEC PROC_PRINT '{table}','{detailTable}','','','{common.Branch}','{common.Period}','{modelRecord.TRAN_ID}','{common.Username}','','{menuDetails.REPORT_NAME}'";

                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        masterData.HEADER_NAME = $"{menuDetails.MD_NAME}";
                        masterData.REPORT_NAME = $"{menuDetails.REPORT_NAME}";
                        masterData.INVOICE_NUMBER = Convert.ToString(reader["VOUCHER_NO"]);
                        masterData.DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("dd-MM-yyyy");
                        masterData.COMPANY_NAME = reader["C_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["C_NAME"]);
                        masterData.PARTY_NAME = reader["PARTY_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["PARTY_NAME"]);
                        masterData.PADDRESS = reader["PADDRESS"] == DBNull.Value ? "" : Convert.ToString(reader["PADDRESS"]);
                        masterData.COMPANY_LOGO = currentCompany.C_LOGO;
                        masterData.B_NTN = reader["B_NTN"] == DBNull.Value ? "" : Convert.ToString(reader["B_NTN"]);
                        masterData.STRN = reader["STRN"] == DBNull.Value ? "" : Convert.ToString(reader["STRN"]);
                        masterData.SIG1 = reader["MENU_SIG1"] == DBNull.Value ? "" : Convert.ToString(reader["MENU_SIG1"]);
                        masterData.SIG2 = reader["MENU_SIG2"] == DBNull.Value ? "" : Convert.ToString(reader["MENU_SIG2"]);
                        masterData.SIG3 = reader["MENU_SIG3"] == DBNull.Value ? "" : Convert.ToString(reader["MENU_SIG3"]);
                        masterData.SIG4 = reader["MENU_SIG4"] == DBNull.Value ? "" : Convert.ToString(reader["MENU_SIG4"]);
                        masterData.MENU_TERMS = reader["MENU_TERMS"] == DBNull.Value ? "" : Convert.ToString(reader["MENU_TERMS"]);
                        masterData.B_WEBSITE = reader["B_WEBSITE"] == DBNull.Value ? "" : Convert.ToString(reader["B_WEBSITE"]);
                        masterData.EMAIL = reader["EMAIL"] == DBNull.Value ? "" : Convert.ToString(reader["EMAIL"]);
                        masterData.BTYPE = reader["BTYPE"] == DBNull.Value ? "" : Convert.ToString(reader["BTYPE"]);
                        masterData.TELL = reader["TELL"] == DBNull.Value ? "" : Convert.ToString(reader["TELL"]);
                        masterData.PT_NTN = reader["PT_NTN"] == DBNull.Value ? "" : Convert.ToString(reader["PT_NTN"]);
                        masterData.C_NAME = reader["C_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["C_NAME"]);
                        masterData.B_ADDRESS = reader["B_ADDRESS"] == DBNull.Value ? "" : Convert.ToString(reader["B_ADDRESS"]);
                        masterData.B_TEL = reader["B_TEL"] == DBNull.Value ? "" : Convert.ToString(reader["B_TEL"]);
                        masterData.FBR_NO = reader["FBR_NO"] == DBNull.Value ? "" : Convert.ToString(reader["FBR_NO"]);
                    }
                    reader.Close();
                }
                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        DataRow dataRow = dataTable.NewRow();
                        dataRow["ItemName"] = Convert.ToString(reader["ITEM_NAME"]);
                        dataRow["HSCode"] = Convert.ToString(reader["HS_CODE"]);
                        dataRow["Unit"] = Convert.ToString(reader["UNIT"]);
                        dataRow["Qty"] = reader["QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY"]);
                        dataRow["Rate"] = reader["RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["RATE"]);
                        dataRow["Amt"] = reader["AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["AMT"]);
                        dataRow["Disc"] = reader["DISC"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DISC"]);
                        dataRow["DiscAmt"] = reader["DISC_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DISC_AMT"]);
                        dataRow["Tax"] = reader["TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TAX"]);
                        dataRow["TaxAmt"] = reader["TAX_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TAX_AMT"]);
                        dataRow["NetAmt"] = reader["NET_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["NET_AMT"]);

                        dataTable.Rows.Add(dataRow);
                    }
                    reader.Close();
                }



                reportData.Master = masterData;

                reportData.Detail = dataTable;
                //reportData.ChargesDetail = reportChargesDetails;


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
        public MyHttpResponseMessage FBRApi_Status(string code, string apiResponce, Common common)
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
                        string query = $"UPDATE {table} SET FBR_NO = '{apiResponce}' WHERE TRAN_ID = '{code}' AND DLT = 'T' AND BCODE = '{common.Branch}' AND PERIOD_ID = '{common.Period}'";
                        SqlCommand command = new SqlCommand(query, connection);
                        connection.Open();
                        command.ExecuteNonQuery();

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

        public MyHttpResponseMessage GetDataForApi(int code, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                List<SaleTaxInvoice> jsonDataResult = new List<SaleTaxInvoice>();
                var Menu = _menuRepository.GetMenu(common.MenuID);
                var table = string.Empty;
                var detailTable = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table = menu.TABLE1;
                    detailTable = menu.TABLE2;
                }


                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {

                    string query = $@"SELECT 
                                        M.V_DATE AS INVOICE_DATE, 
                                        M.REF,
                                        B.FBR_NTN AS SELLER_NTN, 
                                        B.TOKEN As TOKEN,
                                        B.FBR_URL As FBR_URL,
                                        C.C_NAME AS SELLER_BNAME,
                                        B.PROVINCE AS SELLER_PROVINCE, 
                                        B.B_ADDRESS AS SELLER_ADDRESS,
                                        CASE WHEN P.NTN IS NULL OR P.NTN = '' THEN P.CNIC WHEN P.CNIC IS NULL OR P.CNIC = '' THEN P.NTN ELSE P.NTN END AS BUYER_NTN,
                                        P.PARTY_NAME AS BUYER_BNAME,
                                        P.REMARKS AS BUYER_PROVINCE,
                                        P.PADDRESS AS BUYER_ADDRESS,
                                        P.REG_STATUS AS BUYER_REG,
                                        D.ITEM_SNO AS SCENARIO_ID,
                                        P.REG_STATUS AS BUYER_REG_TYPE,
                                        P.PARTY_TYPE AS REG_TYPE
                                        FROM {table} M
                                        LEFT OUTER JOIN TBL_SB_DETAIL D ON D.TRAN_ID = M.TRAN_ID
                                        LEFT OUTER JOIN TBL_BRANCH B ON B.BCODE = M.BCODE
                                        LEFT OUTER JOIN TBL_COMPANY C ON C.CCODE = B.CCODE
                                        LEFT OUTER JOIN TBL_PARTY_TYPES P ON P.PARTY_CODE = M.PARTY_CODE AND P.ACT_CODE = M.ACT_CODE
                                        LEFT OUTER JOIN TBL_REGION R ON R.CODE = P.REGION
                                        WHERE M.TRAN_ID = {code} AND M.BCODE = {common.Branch} AND M.PERIOD_ID = {common.Period} AND D.BCODE = {common.Branch} AND D.PERIOD_ID = {common.Period}";
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new SaleTaxInvoice
                        {
                            INVOICE_DATE = SafeDate(reader["INVOICE_DATE"]),
                            SELLER_NTN = SafeString(reader["SELLER_NTN"]),
                            REF = SafeString(reader["REF"]),
                            TOKEN = SafeString(reader["TOKEN"]),
                            FBR_URL = SafeString(reader["FBR_URL"]),
                            SELLER_BNAME = SafeString(reader["SELLER_BNAME"]),
                            SELLER_PROVINCE = SafeString(reader["SELLER_PROVINCE"]),
                            SELLER_ADDRESS = SafeString(reader["SELLER_ADDRESS"]),
                            BUYER_NTN = SafeString(reader["BUYER_NTN"]),
                            BUYER_BNAME = SafeString(reader["BUYER_BNAME"]),
                            BUYER_PROVINCE = string.IsNullOrWhiteSpace(SafeString(reader["BUYER_NTN"]))
                            ? "Sindh"
                            : SafeString(reader["BUYER_PROVINCE"]),
                            BUYER_ADDRESS = SafeString(reader["BUYER_ADDRESS"]),
                            BUYER_REG = SafeString(reader["BUYER_REG"]),
                            SCENARIO_ID = SafeString(reader["SCENARIO_ID"]),
                            BUYER_REG_TYPE = SafeString(reader["BUYER_REG_TYPE"]),
                            REG_TYPE = SafeString(reader["REG_TYPE"]),

                        };


                        jsonDataResult.Add(row);

                    }
                    reader.Close();
                }

                response.data = jsonDataResult;
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

        public List<SaleTaxInvoiceDetail> GetDetailDataForApi(int code, Common common)
        {
            List<SaleTaxInvoiceDetail> detailList = new List<SaleTaxInvoiceDetail>();

            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? detailTable = string.Empty;

                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    detailTable = menu.TABLE2;
                }

                if (!string.IsNullOrWhiteSpace(detailTable))
                {
                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {

                        string query = $@"SELECT D.HS_CODE,
                                            I.ITEM_NAME,
                                            D.RATE,
                                            U.GROUP_NAME AS UOM,
                                            D.QTY,
                                            D.AMT AS TOTAL_VALUES,
                                            (D.AMT - D.DISC_AMT) AS VALUE_SALES_EXCLUDING,
                                            D.W_AMT,
                                            D.AMT AS FIXEDVALUE_RETAILPRICE,
                                            (D.AMT - D.DISC_AMT) * D.TAX / 100 AS ST_APPLICABLE,
                                            D.SCHEDULE_NO AS SRO_SCH_NO,
                                            D.NET_AMT As NET_AMT,
                                            FBR.S_NAME,
                                            D.SERIAL_NO,
                                            D.TAX,
                                            D.TAX_AMT
                                            FROM {detailTable} AS D 
                                            LEFT OUTER JOIN TBL_ITEMSMASTER I ON I.ITEM_CODE = D.ITEM_CODE
                                            LEFT OUTER JOIN TBL_UNIT U ON U.GROUP_CODE = D.UNIT
                                            LEFT OUTER JOIN TBL_FBR_TYPE FBR ON FBR.CODE = D.FBR_TYPE WHERE D.DLT = 'T' AND D.TRAN_ID = '{code}' 
                                            AND D.BCODE = '{common.Branch}' AND D.PERIOD_ID = '{common.Period}' ORDER BY DT_CODE DESC";

                        SqlCommand command = new SqlCommand(query, connection);
                        connection.Open();

                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            SaleTaxInvoiceDetail row = new SaleTaxInvoiceDetail
                            {
                                HS_CODE = SafeString(reader["HS_CODE"]),
                                ITEM_NAME = SafeString(reader["ITEM_NAME"]),
                                RATE = Math.Round(SafeDouble(reader["RATE"])),
                                UOM = SafeString(reader["UOM"]),
                                QTY = SafeDecimal(reader["QTY"]),
                                TOTAL_VALUES = Math.Round(SafeDouble(reader["TOTAL_VALUES"])),
                                NET_AMT = Math.Round(SafeDouble(reader["NET_AMT"])),
                                VALUE_SALES_EXCLUDING = Math.Round(SafeDouble(reader["VALUE_SALES_EXCLUDING"])),
                                W_AMT = Math.Round(SafeDouble(reader["W_AMT"])),
                                FIXEDVALUE_RETAILPRICE = Math.Round(SafeDouble(reader["FIXEDVALUE_RETAILPRICE"])),
                                ST_APPLICABLE = Math.Round(SafeDouble(reader["ST_APPLICABLE"]), 2),
                                SRO_SCH_NO = SafeString(reader["SRO_SCH_NO"]),
                                S_NAME = SafeString(reader["S_NAME"]),
                                SERIAL_NO = SafeInt(reader["SERIAL_NO"]),
                                TAX = Math.Round(SafeDouble(reader["TAX"])),
                                TAX_AMT = SafeDecimal(reader["TAX_AMT"]),

                            };

                            detailList.Add(row);
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return detailList;
        }

        public MyHttpResponseMessage GetDashboardData(Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            DashboardData dashboardData = new DashboardData();
            try
            {
                string validFbrFilter = @"M.FBR_NO IS NOT NULL
                    AND LTRIM(RTRIM(M.FBR_NO)) <> ''
                    AND M.FBR_NO NOT LIKE '%[^A-Z0-9]%'
                    AND M.FBR_NO LIKE '%[A-Z]%'
                    AND M.FBR_NO LIKE '%[0-9]%'
                    AND LEN(LTRIM(RTRIM(M.FBR_NO))) >= 15";

                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {
                    connection.Open();

                    string kpiQuery = $@"SELECT COUNT(DISTINCT M.TRAN_ID) AS FBR_COUNT,
                                            ISNULL(SUM(D.AMT), 0) AS TOTAL_AMT,
                                            ISNULL(SUM(D.TAX_AMT), 0) AS TOTAL_TAX
                                            FROM TBL_SB_MASTER M
                                            LEFT OUTER JOIN TBL_SB_DETAIL D ON D.TRAN_ID = M.TRAN_ID AND D.BCODE = M.BCODE AND D.PERIOD_ID = M.PERIOD_ID AND D.DLT = 'T'
                                            WHERE M.DLT = 'T' AND M.BCODE = '{common.Branch}' AND M.PERIOD_ID = '{common.Period}'
                                            AND {validFbrFilter}";

                    SqlCommand kpiCommand = new SqlCommand(kpiQuery, connection);
                    SqlDataReader kpiReader = kpiCommand.ExecuteReader();
                    if (kpiReader.Read())
                    {
                        dashboardData.FbrInvoiceCount = kpiReader["FBR_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(kpiReader["FBR_COUNT"]);
                        dashboardData.TotalAmount = kpiReader["TOTAL_AMT"] == DBNull.Value ? 0 : Convert.ToDouble(kpiReader["TOTAL_AMT"]);
                        dashboardData.TotalTaxAmount = kpiReader["TOTAL_TAX"] == DBNull.Value ? 0 : Convert.ToDouble(kpiReader["TOTAL_TAX"]);
                    }
                    kpiReader.Close();

                    string chartQuery = $@"SELECT YEAR(M.V_DATE) AS YR, MONTH(M.V_DATE) AS MTH,
                                            ISNULL(SUM(D.AMT), 0) AS TOTAL_AMT
                                            FROM TBL_SB_MASTER M
                                            LEFT OUTER JOIN TBL_SB_DETAIL D ON D.TRAN_ID = M.TRAN_ID AND D.BCODE = M.BCODE AND D.PERIOD_ID = M.PERIOD_ID AND D.DLT = 'T'
                                            WHERE M.DLT = 'T' AND M.BCODE = '{common.Branch}' AND M.PERIOD_ID = '{common.Period}' AND M.V_DATE IS NOT NULL AND {validFbrFilter}
                                            GROUP BY YEAR(M.V_DATE), MONTH(M.V_DATE) 
                                            ORDER BY YR, MTH";

                    SqlCommand chartCommand = new SqlCommand(chartQuery, connection);
                    SqlDataReader chartReader = chartCommand.ExecuteReader();
                    while (chartReader.Read())
                    {
                        int year = chartReader["YR"] == DBNull.Value ? 0 : Convert.ToInt32(chartReader["YR"]);
                        int month = chartReader["MTH"] == DBNull.Value ? 0 : Convert.ToInt32(chartReader["MTH"]);
                        if (year > 0 && month > 0)
                        {
                            dashboardData.SalesChart.Add(new DashboardSalesPoint
                            {
                                Month = new DateTime(year, month, 1).ToString("MMM yyyy"),
                                Amount = chartReader["TOTAL_AMT"] == DBNull.Value ? 0 : Convert.ToDouble(chartReader["TOTAL_AMT"])
                            });
                        }
                    }
                    chartReader.Close();

                    string dayChartQuery = $@"SELECT CONVERT(date, M.V_DATE) AS VDT,
                                            ISNULL(SUM(D.AMT), 0) AS TOTAL_AMT
                                            FROM TBL_SB_MASTER M
                                            LEFT OUTER JOIN TBL_SB_DETAIL D ON D.TRAN_ID = M.TRAN_ID AND D.BCODE = M.BCODE AND D.PERIOD_ID = M.PERIOD_ID AND D.DLT = 'T'
                                            WHERE M.DLT = 'T' AND M.BCODE = '{common.Branch}' AND M.PERIOD_ID = '{common.Period}' AND M.V_DATE IS NOT NULL
                                            AND {validFbrFilter}
                                            GROUP BY CONVERT(date, M.V_DATE)
                                            ORDER BY VDT";

                    SqlCommand dayChartCommand = new SqlCommand(dayChartQuery, connection);
                    SqlDataReader dayChartReader = dayChartCommand.ExecuteReader();
                    while (dayChartReader.Read())
                    {
                        if (dayChartReader["VDT"] == DBNull.Value)
                        {
                            continue;
                        }

                        DateTime voucherDate = Convert.ToDateTime(dayChartReader["VDT"]);
                        dashboardData.DayWiseSales.Add(new DashboardSalesPoint
                        {
                            Month = voucherDate.ToString("dd MMM yyyy"),
                            Amount = dayChartReader["TOTAL_AMT"] == DBNull.Value ? 0 : Convert.ToDouble(dayChartReader["TOTAL_AMT"])
                        });
                    }
                    dayChartReader.Close();

                    string sparkQuery = $@"SELECT CONVERT(date, M.V_DATE) AS VDT,
                                            COUNT(DISTINCT M.TRAN_ID) AS INV_COUNT,
                                            ISNULL(SUM(D.AMT), 0) AS TOTAL_AMT,
                                            ISNULL(SUM(D.TAX_AMT), 0) AS TOTAL_TAX
                                            FROM TBL_SB_MASTER M
                                            LEFT OUTER JOIN TBL_SB_DETAIL D ON D.TRAN_ID = M.TRAN_ID AND D.BCODE = M.BCODE AND D.PERIOD_ID = M.PERIOD_ID AND D.DLT = 'T'
                                            WHERE M.DLT = 'T' AND M.BCODE = '{common.Branch}' AND M.PERIOD_ID = '{common.Period}' AND M.V_DATE IS NOT NULL
                                            AND {validFbrFilter}
                                            GROUP BY CONVERT(date, M.V_DATE)
                                            ORDER BY VDT";

                    SqlCommand sparkCommand = new SqlCommand(sparkQuery, connection);
                    SqlDataReader sparkReader = sparkCommand.ExecuteReader();
                    while (sparkReader.Read())
                    {
                        if (sparkReader["VDT"] == DBNull.Value)
                        {
                            continue;
                        }

                        DateTime sparkDate = Convert.ToDateTime(sparkReader["VDT"]);
                        dashboardData.Sparkline.Add(new DashboardSalesPoint
                        {
                            Month = sparkDate.ToString("dd MMM yyyy"),
                            Count = sparkReader["INV_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(sparkReader["INV_COUNT"]),
                            Amount = sparkReader["TOTAL_AMT"] == DBNull.Value ? 0 : Convert.ToDouble(sparkReader["TOTAL_AMT"]),
                            TaxAmount = sparkReader["TOTAL_TAX"] == DBNull.Value ? 0 : Convert.ToDouble(sparkReader["TOTAL_TAX"])
                        });
                    }
                    sparkReader.Close();
                }

                response.data = dashboardData;
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
                response.data = dashboardData;
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }

        private double SafeDouble(object value)
        {
            if (value == DBNull.Value || value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return 0.0;

            return Convert.ToDouble(value);
        }
        private decimal SafeDecimal(object value)
        {
            if (value == DBNull.Value || value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return 0m;

            return Convert.ToDecimal(value);
        }


        private int SafeInt(object value)
        {
            if (value == DBNull.Value || value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return 0;

            return Convert.ToInt32(value);
        }

        private string SafeString(object value)
        {
            if (value == DBNull.Value || value == null)
                return "";

            return value.ToString().Trim();
        }

        private string SafeDate(object value)
        {
            if (value == DBNull.Value || value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return "";

            return Convert.ToDateTime(value).ToString("yyyy-MM-dd");
        }

    }
}
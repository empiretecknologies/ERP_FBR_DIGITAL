using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Core.Services;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Runtime.CompilerServices;

namespace Empire_ERP.Infrastructure.Repositories
{
    public class AdvanceProductionRepository : IAdvanceProductionRepository
    {
        public IBranchRepository _branchRepository { get; set; }
        public IMenuService _menuRepository { get; set; }
        public AdvanceProductionRepository(IBranchRepository branchRepository, IMenuService menuRepository)
        {
            _branchRepository = branchRepository;
            _menuRepository = menuRepository;
        }

        public MyHttpResponseMessage QuickSearch(Common common, Menu menu)
		{
			MyHttpResponseMessage response = new MyHttpResponseMessage();
			try
			{
				string? table = menu.TABLE1;
				List<object> jsonDataResult = new List<object>();
				using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
				{
                    string query = "SELECT A.TRAN_ID, A.V_DATE, A.VOUCHER_NO, IM.ITEM_CODE," +
                                    "IM.ITEM_NAME, A.REF, A.QTY, A.RATE, A.AMT, A.REMARKS," +
                                    "A.ADD_USER_ID, A.ADD_DATE, A.ADD_COMPUTER_NAME, A.ADD_IP_ADDRESS," +
                                    "A.EDIT_USER_ID, A.EDIT_DATE, A.EDIT_COMPUTER_NAME, A.EDIT_IP_ADDRESS," +
                                    "A.ADD_POSTALCODE, A.EDIT_POSTALCODE, CASE WHEN A.ASTATUS = 'Y' THEN 'Active' ELSE 'In-Active' END AS ASTATUS " +
									$"FROM {table} A " +
                                    "LEFT OUTER JOIN TBL_ITEMSMASTER IM ON A.ITEM_CODE = IM.ITEM_CODE " +
                                    $"WHERE A.DLT = 'T' AND A.ASTATUS = 'Y' AND A.BCODE = '" + common.Branch + "' AND A.PERIOD_ID = '" + common.Period + "' ORDER BY A.TRAN_ID DESC";
                    SqlCommand command = new SqlCommand(query, connection);
					connection.Open();
					SqlDataReader reader = command.ExecuteReader();
					while (reader.Read())
					{
						var row = new
						{
                            TRAN_ID = Convert.ToString(reader["TRAN_ID"]),
                            ASTATUS = Convert.ToString(reader["ASTATUS"]),
                            V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                            VOUCHER_NO = Convert.ToString(reader["VOUCHER_NO"]),
                            ITEM_CODE = Convert.ToInt32(reader["ITEM_CODE"]),
                            ITEM_NAME = Convert.ToString(reader["ITEM_NAME"]),
                            QTY = Convert.ToString(reader["QTY"]),
                            REF = Convert.ToString(reader["REF"]),
                            RATE = Convert.ToString(reader["RATE"]),
                            AMT = Convert.ToDecimal(reader["AMT"]),
                            REMARKS = Convert.ToString(reader["REMARKS"]),
                            ADD_USER_ID = Convert.ToString(reader["ADD_USER_ID"]),
                            ADD_DATE = Convert.ToDateTime(reader["ADD_DATE"]),
                            ADD_COMPUTER_NAME = Convert.ToString(reader["ADD_COMPUTER_NAME"]),
                            ADD_IP_ADDRESS = Convert.ToString(reader["ADD_IP_ADDRESS"]),
                            EDIT_USER_ID = Convert.ToString(reader["EDIT_USER_ID"]),
                            EDIT_DATE = Convert.ToDateTime(reader["EDIT_DATE"]),
                            EDIT_COMPUTER_NAME = Convert.ToString(reader["EDIT_COMPUTER_NAME"]),
                            EDIT_IP_ADDRESS = Convert.ToString(reader["EDIT_IP_ADDRESS"]),
                            ADD_POSTALCODE = Convert.ToString(reader["ADD_POSTALCODE"]),
                            EDIT_POSTALCODE = Convert.ToString(reader["EDIT_POSTALCODE"]),
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

        private int GenerateNextId(Common common, SqlCommand command, Menu menu)
        {
            try
            {
                string? table = menu.TABLE1;
                string maxIdQuery = $"SELECT ISNULL(MAX(TRAN_ID), 0) + 1 FROM {table} WHERE BCODE = '{common.Branch}' AND PERIOD_ID = '{common.Period}'";
                command.CommandText = maxIdQuery;
                object result = command.ExecuteScalar();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {

            }
            return 0;
        }

        private string GenerateVoucherNo(Common common, int code, string vDate, Menu menu)
        {
            try
            {
                string? prefix = menu.PERFIX, shortName = string.Empty;
                int voucherLength = Convert.ToInt32(menu.VOUCHER_LEN);
                var branchData = _branchRepository.GetBranchByCode(common.Branch);
                if (branchData.data != null)
                {
                    var branch = (Branch)branchData.data;
                    shortName = branch.B_SHORT_NAME;
                }

                if (!String.IsNullOrWhiteSpace(shortName) && !String.IsNullOrWhiteSpace(prefix) && voucherLength > 0 && code > 0)
                {
                    string paddedVoucherValue = code.ToString().PadLeft(voucherLength, '0');
                    return $"{shortName}/{prefix}/{Convert.ToDateTime(vDate).ToString("yy-MM")}/{paddedVoucherValue}";
                }
            }
            catch (Exception ex)
            {

            }
            return string.Empty;
        }

        private int GenerateNextDetailId(SqlCommand command, Menu menu)
        {
            try
            {
                string? table = menu.TABLE2;
                string maxIdQuery = $"SELECT ISNULL(MAX(DT_CODE), 0) + 1 FROM {table}";
                command.CommandText = maxIdQuery;
                object result = command.ExecuteScalar();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {

            }
            return 0;
        }

        public MyHttpResponseMessage Save(CustomAdvanceProduction modelRecord, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = menu.TABLE1, detailTable = menu.TABLE2;
                var ip = common.IPAddress;
                var computerName = common.ComputerName;
                var postalCode = common.PostalCode;
                var username = common.Username;
                var branch = common.Branch;
                var periodID = common.Period;
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
                        //string checkBatchQuery = "";
                        //if (modelRecord.Master.TRAN_ID == null || modelRecord.Master.TRAN_ID == 0)
                        //{
                        //    checkBatchQuery = $@"SELECT COUNT(*) FROM {table} WHERE BATCH_NAME = '{modelRecord.Master.BATCH_NAME}' AND ITEM_CODE = '{modelRecord.Master.ITEM_CODE}' AND BCODE = '{branch}' AND DLT = 'T'";
                        //}
                        //else
                        //{
                        //    checkBatchQuery = $@"SELECT COUNT(*) FROM {table} WHERE BATCH_NAME = '{modelRecord.Master.BATCH_NAME}' AND ITEM_CODE = '{modelRecord.Master.ITEM_CODE}' AND TRAN_ID != '{modelRecord.Master.TRAN_ID}' AND BCODE = '{branch}' AND DLT = 'T'";
                        //}

                        //command.CommandText = checkBatchQuery;
                        //int existingCount = (int)command.ExecuteScalar();

                        //if (existingCount > 0)
                        //{
                        //    response.msg = "Batch Name Already Exists! Please use a unique name.";
                        //    response.msgType = 2;
                        //    if (transaction != null) transaction.Rollback();
                        //    return response;
                        //}

                        string query = "", detailQuery = "", voucherNo = string.Empty;
                        bool IsMasterAdded = true, IsNew = false;
                        int code = 0;

                        if (modelRecord.Master.TRAN_ID == null || modelRecord.Master.TRAN_ID == 0)
                        {
                            IsNew = true;
                            code = GenerateNextId(common, command, menu);

                            if (code > 0)
                            {
                                modelRecord.Master.TRAN_ID = code;
                                voucherNo = GenerateVoucherNo(common, code, CommonService.GetDateTime("Pakistan Standard Time"), menu);
                                if (String.IsNullOrWhiteSpace(voucherNo))
                                {
                                    IsMasterAdded = false;
                                }
                            }
                            else
                            {
                                IsMasterAdded = false;
                            }

                            query = $"INSERT INTO {table} " +
                                    "(TRAN_ID, V_DATE, VOUCHER_NO, ITEM_CODE, UNIT, WAREHOUSE, LOT, QTY, RATE, AMT, REF, WASTAGE, WASTAGE_CODE, WASTAGE_QTY, COMP, " +
                                    "REMARKS, BCODE, " +
                                    "PERIOD_ID, ADD_USER_ID, ADD_DATE, ADD_COMPUTER_NAME, " +
                                    "ADD_IP_ADDRESS, EDIT_USER_ID, EDIT_DATE, EDIT_COMPUTER_NAME, " +
                                    "EDIT_IP_ADDRESS, ADD_POSTALCODE, EDIT_POSTALCODE, ASTATUS, " +
                                    "MENU_ID, DLT) " +
                                    $"VALUES " +
                                    $"('{code}', '{modelRecord.Master.V_DATE}', '{voucherNo}'," +
                                    $"'{modelRecord.Master.ITEM_CODE}','{modelRecord.Master.UNIT}','{modelRecord.Master.WAREHOUSE}','{modelRecord.Master.LOT}','{modelRecord.Master.QTY}'," +
                                    $"'{modelRecord.Master.RATE}','{modelRecord.Master.AMT}','{modelRecord.Master.REF}','{modelRecord.Master.WASTAGE}','{modelRecord.Master.WASTAGE_CODE}','{modelRecord.Master.WASTAGE_QTY}','{modelRecord.Master.COMP}'," +
                                    $"'{modelRecord.Master.REMARKS}','{branch}','{periodID}'," +
                                    $"'{username}','{CommonService.GetDateTime("Pakistan Standard Time")}', '{computerName}'," +
                                    $"'{ip}', '{username}','{CommonService.GetDateTime("Pakistan Standard Time")}'," +
                                    $"'{computerName}', '{ip}', " +
                                    $"'{postalCode}', '{postalCode}', " +
                                    $"'{modelRecord.Master.ASTATUS}', '{menuID}', 'T')";

                            command.CommandText = query;
                            command.ExecuteNonQuery();  
                        }
                        else
                        {
                            query = $"UPDATE {table} " +
                                    $"SET V_DATE = '{modelRecord.Master.V_DATE}', " +
                                    $"ASTATUS = '{modelRecord.Master.ASTATUS}', " +
                                    $"ITEM_CODE = '{modelRecord.Master.ITEM_CODE}', " +
                                    $"UNIT = '{modelRecord.Master.UNIT}', " +
                                    $"WAREHOUSE = '{modelRecord.Master.WAREHOUSE}', " +
                                    $"LOT = '{modelRecord.Master.LOT}', " +
                                    $"WASTAGE_CODE = '{modelRecord.Master.WASTAGE_CODE}', " +
                                    $"WASTAGE_QTY = '{modelRecord.Master.WASTAGE_QTY}', " +
                                    $"COMP = '{modelRecord.Master.COMP}', " +
                                    $"QTY = '{modelRecord.Master.QTY}', " +
                                    $"WASTAGE = '{modelRecord.Master.WASTAGE}', " +
                                    $"RATE = '{modelRecord.Master.RATE}', " +
                                    $"AMT = '{modelRecord.Master.AMT}', " +
                                    $"REF = '{modelRecord.Master.REF}', " +
                                    $"REMARKS = '{modelRecord.Master.REMARKS}', " +
                                    $"EDIT_USER_ID = '{modelRecord.Master.EDIT_USER_ID}', " +
                                    $"EDIT_DATE = '{modelRecord.Master.EDIT_DATE}', " +
                                    $"EDIT_COMPUTER_NAME = '{modelRecord.Master.EDIT_COMPUTER_NAME}', " +
                                    $"EDIT_IP_ADDRESS = '{modelRecord.Master.EDIT_IP_ADDRESS}', " +
                                    $"EDIT_POSTALCODE = '{modelRecord.Master.EDIT_POSTALCODE}' " +
                                    $"WHERE TRAN_ID = '{modelRecord.Master.TRAN_ID}' AND BCODE = '{branch}' AND PERIOD_ID = '{periodID}'";

                            command.CommandText = query;
                            command.ExecuteNonQuery();
                        }

                        var isDetailAdded = true;

                        if (modelRecord.Detail.Count > 0)
                        {
                            detailQuery = $"UPDATE {detailTable} SET DLT = 'F'" +
                            $" WHERE TRAN_ID = '{modelRecord.Master.TRAN_ID}' AND BCODE = '{branch}' AND PERIOD_ID = '{periodID}'";
                            command.CommandText = detailQuery;
                            command.ExecuteNonQuery();
                        }
                        foreach (var item in modelRecord.Detail.ToList())
                        {
                            try
                            {
                                if (item.DT_CODE == null || item.DT_CODE == 0)
                                {
                                    int detailCode = GenerateNextDetailId(command, menu);
                                    if (detailCode > 0)
                                    {
                                        detailQuery = $"INSERT INTO {detailTable} " +
                                                       "(TRAN_ID, DT_CODE, ITEM_CODE, QTY, WASTAGE_RATE, UNIT, " +
                                                       "RATE, AMT, DT_DESC, " +
                                                       "BCODE, PERIOD_ID, ADD_USER_ID, ADD_DATE, " +
                                                       "ADD_COMPUTER_NAME, ADD_IP_ADDRESS, EDIT_USER_ID, " +
                                                       "EDIT_DATE, EDIT_COMPUTER_NAME, EDIT_IP_ADDRESS, " +
                                                       "ADD_POSTALCODE, EDIT_POSTALCODE, MENU_ID, DLT) " +
                                                       $"VALUES " +
                                                       $"('{modelRecord.Master.TRAN_ID}', '{detailCode}', '{item.ITEM_CODE}', " +
                                                       $"'{item.QTY}', '{item.WASTAGE_RATE}', '{item.UNIT}', " +
                                                       $"'{item.RATE}','{item.AMT}', " +
                                                       $"'{item.DT_DESC}', '{branch}', '{periodID}', '{username}', " +
                                                       $"'{CommonService.GetDateTime("Pakistan Standard Time")}', '{computerName}', " +
                                                       $"'{ip}', '{username}', '{CommonService.GetDateTime("Pakistan Standard Time")}', " +
                                                       $"'{computerName}', '{ip}', '{postalCode}', " +
                                                       $"'{postalCode}', '{menuID}', 'T')";

                                        command.CommandText = detailQuery;
                                        command.ExecuteNonQuery();
                                    }
                                    else
                                    {
                                        isDetailAdded = false;
                                    }
                                }
                                else
                                {
                                    detailQuery = $"UPDATE {detailTable} " +
                                                  $"SET ITEM_CODE = '{item.ITEM_CODE}', " +
                                                  $"QTY = '{item.QTY}', " +
                                                  $"WASTAGE_RATE = '{item.WASTAGE_RATE}', " +
                                                  $"UNIT = '{item.UNIT}', " +
                                                  $"RATE = '{item.RATE}', " +
                                                  $"AMT = '{item.AMT}', " +
                                                  $"DT_DESC = '{item.DT_DESC}', " +
                                                  $"EDIT_USER_ID = '{username}', " +
                                                  $"EDIT_DATE = '{CommonService.GetDateTime("Pakistan Standard Time")}', " +
                                                  $"EDIT_COMPUTER_NAME = '{computerName}', " +
                                                  $"EDIT_IP_ADDRESS = '{ip}', " +
                                                  $"EDIT_POSTALCODE = '{postalCode}', " +
                                                  $"DLT = 'T'" +
                                                  $"WHERE TRAN_ID = '{modelRecord.Master.TRAN_ID}' AND DT_CODE = '{item.DT_CODE}' AND BCODE = '{branch}' AND PERIOD_ID = '{periodID}'";

                                    command.CommandText = detailQuery;
                                    command.ExecuteNonQuery();
                                }
                            }
                            catch (Exception)
                            {
                                isDetailAdded = false;
                            }
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

        //public MyHttpResponseMessage CopyRecord(CopyRecord record, Common common, Menu menu)
        //{
        //    MyHttpResponseMessage response = new MyHttpResponseMessage();
        //    response.msgType = 2;
        //    response.msg = "Data not found in our records";
        //    try
        //    {
        //        string? table = menu.TABLE1;
        //        string? table2 = menu.TABLE2;
        //        string connectionString = new SQLService().getconnstring();

        //        AdvanceProduction AdvanceProduction = new AdvanceProduction();
        //        List<AdvanceProductionDetail> AdvanceProductionDetailList = new List<AdvanceProductionDetail>();

        //        using (SqlConnection connection = new SqlConnection(connectionString))
        //        {
        //            connection.Open();
        //            string query = $@"SELECT * FROM {table} WHERE TRAN_ID = {record.TRAN_ID} AND DLT = 'T' AND BCODE = {common.Branch} AND PERIOD_ID = {common.Period}";
        //            string detailQuery = $@"SELECT * FROM {table2} WHERE TRAN_ID = {record.TRAN_ID} AND DLT = 'T' AND BCODE = {common.Branch} AND PERIOD_ID = {common.Period}";
        //            SqlCommand command = new SqlCommand(query, connection);
        //            SqlDataReader reader = command.ExecuteReader();
        //            if (reader.Read())
        //            {
        //                AdvanceProduction = new AdvanceProduction
        //                {
        //                    TRAN_ID = 0,
        //                    V_DATE = record.V_DATE,
        //                    PROCESS=record.PROCESS,
        //                    REF = Convert.ToString(reader["REF"]),
        //                    COST = Convert.ToDouble(reader["COST"]),
        //                    BQTY = Convert.ToDouble(reader["BQTY"]),
        //                    REMARKS = Convert.ToString(reader["REMARKS"]),
        //                    ASTATUS = Convert.ToString(reader["ASTATUS"]),
        //                };
        //            }

        //            reader.Close();

        //            SqlCommand detail_Command = new SqlCommand(detailQuery, connection);
        //            SqlDataReader detail_Reader = detail_Command.ExecuteReader();
        //            while (detail_Reader.Read())
        //            {
        //                var row = new AdvanceProductionDetail
        //                {
        //                    ITEM_CODE = Convert.ToInt32(detail_Reader["ITEM_CODE"]),
        //                    QTY = Convert.ToDouble(detail_Reader["QTY"]),
        //                    UNIT = Convert.ToInt32(detail_Reader["UNIT"]),
        //                    //QTY2 = Convert.ToDouble(detail_Reader["QTY2"]),
        //                    //BAL_QTY = Convert.ToDouble(detail_Reader["BAL_QTY"]),
        //                    RATE = Convert.ToInt32(detail_Reader["RATE"]),
        //                    AMT = Convert.ToInt32(detail_Reader["AMT"]),
        //                    DT_DESC = Convert.ToString(detail_Reader["DT_DESC"]),
        //                    LOSS = Convert.ToDouble(detail_Reader["LOSS"]),
        //                    //CHK = detail_Reader["CHK"] == DBNull.Value ? 0 : Convert.ToInt32(detail_Reader["CHK"]),
        //                };
        //                AdvanceProductionDetailList.Add(row);
        //            }

        //            detail_Reader.Close();
        //            connection.Close();
        //        }

        //        var customAdvanceProduction = new CustomAdvanceProduction
        //        {
        //            Master = AdvanceProduction,
        //            Detail = AdvanceProductionDetailList
        //        };

        //        response = this.Save(customAdvanceProduction, common, menu);

        //        if (response.msgType == 1)
        //        {
        //            response.msg = "Record Copied Successfully";
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        string _catchMessage = ex.Message;
        //        if (ex.InnerException != null)
        //        {
        //            _catchMessage += "<br/>" + ex.InnerException.Message;
        //        }
        //        response.msg = _catchMessage;
        //        response.msgType = 2;
        //    }
        //    return response;
        //}

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

                AdvanceProduction AdvanceProduction = new AdvanceProduction();
                List<AdvanceProductionDetail> AdvanceProductionDetailList = new List<AdvanceProductionDetail>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Query for Master
                    string query = $@"SELECT * FROM {table} WHERE TRAN_ID = {record.TRAN_ID} AND DLT = 'T' AND BCODE = {common.Branch} AND PERIOD_ID = {common.Period}";
                    // Query for Details
                    string detailQuery = $@"SELECT * FROM {table2} WHERE TRAN_ID = {record.TRAN_ID} AND DLT = 'T' AND BCODE = {common.Branch} AND PERIOD_ID = {common.Period}";

                    SqlCommand command = new SqlCommand(query, connection);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            AdvanceProduction = new AdvanceProduction
                            {
                                TRAN_ID = 0, 
                                V_DATE = record.V_DATE,
                                BATCH_NAME = record.BATCH_NAME,
                                //PROCESS = Convert.ToInt32(reader["PROCESS"]), 
                                //ITEM_CODE = Convert.ToInt32(reader["ITEM_CODE"]),
                                //REF = Convert.ToString(reader["REF"]),
                                //COST = Convert.ToDouble(reader["COST"]),
                                //BQTY = Convert.ToDouble(reader["BQTY"]),
                                //REMARKS = Convert.ToString(reader["REMARKS"]),
                                //ASTATUS = Convert.ToString(reader["ASTATUS"]),





                                PROCESS = reader["PROCESS"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PROCESS"]),
                                ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ITEM_CODE"]),
                                REF = reader["REF"] == DBNull.Value ? "" : Convert.ToString(reader["REF"]),
                                COST = reader["COST"] == DBNull.Value ? 0.0 : Convert.ToDouble(reader["COST"]),
                                BQTY = reader["BQTY"] == DBNull.Value ? 0.0 : Convert.ToDouble(reader["BQTY"]),
                                REMARKS = reader["REMARKS"] == DBNull.Value ? "" : Convert.ToString(reader["REMARKS"]),
                                ASTATUS = reader["ASTATUS"] == DBNull.Value ? "" : Convert.ToString(reader["ASTATUS"]),

                                // For detail_Reader
                                
                            };
                        }
                    }

                    SqlCommand detail_Command = new SqlCommand(detailQuery, connection);
                    using (SqlDataReader detail_Reader = detail_Command.ExecuteReader())
                    {
                        while (detail_Reader.Read())
                        {
                            var row = new AdvanceProductionDetail
                            {

                                ITEM_CODE = detail_Reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(detail_Reader["ITEM_CODE"]),
                                QTY = detail_Reader["QTY"] == DBNull.Value ? 0.0 : Convert.ToDouble(detail_Reader["QTY"]),
                                UNIT = detail_Reader["UNIT"] == DBNull.Value ? 0 : Convert.ToInt32(detail_Reader["UNIT"]),
                                DT_DESC = detail_Reader["DT_DESC"] == DBNull.Value ? "" : Convert.ToString(detail_Reader["DT_DESC"]),
                                LOSS = detail_Reader["LOSS"] == DBNull.Value ? 0.0 : Convert.ToDouble(detail_Reader["LOSS"]),
                                LOSS_WEIGHT = detail_Reader["LOSS_WEIGHT"] == DBNull.Value ? 0.0 : Convert.ToDouble(detail_Reader["LOSS_WEIGHT"]),


                                //ITEM_CODE = Convert.ToInt32(detail_Reader["ITEM_CODE"]),
                                //QTY = Convert.ToDouble(detail_Reader["QTY"]),
                                //UNIT = Convert.ToInt32(detail_Reader["UNIT"]),
                                //DT_DESC = Convert.ToString(detail_Reader["DT_DESC"]),
                                //LOSS = Convert.ToDouble(detail_Reader["LOSS"]),
                                //LOSS_WEIGHT = Convert.ToDouble(detail_Reader["LOSS_WEIGHT"]), 
                            };
                            AdvanceProductionDetailList.Add(row);
                        }
                    }
                    connection.Close();
                }

                var customAdvanceProduction = new CustomAdvanceProduction
                {
                    Master = AdvanceProduction,
                    Detail = AdvanceProductionDetailList
                };

                // Final Save call
                response = this.Save(customAdvanceProduction, common, menu);

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

        public MyHttpResponseMessage GetAdvanceProductionByCode(int code, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = menu.TABLE1;
                List<object> jsonDataResult = new List<object>();
                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {
                    string query = "SELECT TRAN_ID, V_DATE, VOUCHER_NO," +
                                   "ITEM_CODE, UNIT, WAREHOUSE, LOT, QTY, WASTAGE, WASTAGE_CODE, WASTAGE_QTY, RATE, AMT, REF, COMP, REMARKS, ASTATUS " +
                                   $"FROM {table} WHERE DLT = 'T' AND TRAN_ID = '{code}' AND BCODE = '{common.Branch}' " +
                                   $"AND PERIOD_ID = '{common.Period}'";
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new
                        {
                            TRAN_ID = reader["TRAN_ID"] == DBNull.Value ? "" : Convert.ToString(reader["TRAN_ID"]),
                            ASTATUS = reader["ASTATUS"] == DBNull.Value ? "" : Convert.ToString(reader["ASTATUS"]),
                            V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                            VOUCHER_NO = reader["VOUCHER_NO"] == DBNull.Value ? "" : Convert.ToString(reader["VOUCHER_NO"]),
                            ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? "" : Convert.ToString(reader["ITEM_CODE"]),
                            UNIT = reader["UNIT"] == DBNull.Value ? "" : Convert.ToString(reader["UNIT"]),
                            WAREHOUSE = reader["WAREHOUSE"] == DBNull.Value ? "" : Convert.ToString(reader["WAREHOUSE"]),
                            LOT = reader["LOT"] == DBNull.Value ? "" : Convert.ToString(reader["LOT"]),
                            QTY = reader["QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY"]),
                            WASTAGE = reader["WASTAGE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["WASTAGE"]),
                            WASTAGE_CODE = reader["WASTAGE_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["WASTAGE_CODE"]),
                            COMP = reader["COMP"] == DBNull.Value ? 0 : Convert.ToInt32(reader["COMP"]),
                            WASTAGE_QTY = reader["WASTAGE_QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["WASTAGE_QTY"]),
                            RATE = reader["RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["RATE"]),
                            REF = reader["REF"] == DBNull.Value ? "" : Convert.ToString(reader["REF"]),
                            AMT = reader["AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["AMT"]),
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

        public MyHttpResponseMessage GetAdvanceProductionDetailByCode(int code, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = menu.TABLE2;
                List<object> jsonDataResult = new List<object>();
                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {
                    

                    string query = $@"SELECT D.DT_CODE, D.ITEM_CODE, IT.ITEM_NAME, D.QTY, D.WASTAGE_RATE, D.UNIT, D.RATE, D.AMT, D.DT_DESC FROM {table} D 
                                      LEFT OUTER JOIN TBL_ITEMSMASTER IT ON IT.ITEM_CODE = D.ITEM_CODE
                                      WHERE D.DLT = 'T' AND D.TRAN_ID = '{code}' AND D.BCODE = '{common.Branch}' AND D.PERIOD_ID = '{common.Period}' ORDER BY IT.ITEM_NAME";
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new
                        {

                            DT_CODE = reader["DT_CODE"] == DBNull.Value ? "" : Convert.ToString(reader["DT_CODE"]),
                            ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ITEM_CODE"]),
                            QTY = reader["QTY"] == DBNull.Value ? "" : Convert.ToString(reader["QTY"]),
                            WASTAGE_RATE = reader["WASTAGE_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["WASTAGE_RATE"]),
                            UNIT = reader["UNIT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UNIT"]),
                            RATE = reader["RATE"] == DBNull.Value ? "" : Convert.ToString(reader["RATE"]),
                            AMT = reader["AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["AMT"]),
                            DT_DESC = reader["DT_DESC"] == DBNull.Value ? "" : Convert.ToString(reader["DT_DESC"])

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

        public MyHttpResponseMessage Delete(int code, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            response.msgType = 2;
            response.msg = "Data not found in our records";
            try
            {
                string? table = menu.TABLE1;
                string connectionString = new SQLService().getconnstring();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = $"UPDATE {table} SET DLT = 'F' WHERE TRAN_ID = '{code}' AND BCODE = '{common.Branch}' AND PERIOD_ID = '{common.Period}'";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.ExecuteNonQuery();
                    response.msgType = 1;
                    response.msg = "Record Deleted Successfully";
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

        public MyHttpResponseMessage DeleteAdvanceProductionDetailByCode(int code, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = menu.TABLE2;
                var branch = common.Branch;
                var period = common.Period;
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

        public MyHttpResponseMessage GetDataForReport(AdvanceProductionRDLCReport modelRecord, DataTable dataTable, CustomMenuDetail menuDetails, Company currentCompany, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            AdvanceProductionRDLCReport masterData = new AdvanceProductionRDLCReport();
            CustomAdvanceProductionForPrintReport reportData = new CustomAdvanceProductionForPrintReport();
            MyHttpResponseMessage Menu = this._menuRepository.GetMenu(common.MenuID);
            string table = string.Empty;
            string detailTable = string.Empty;
            string empty = string.Empty;
            string empty2 = string.Empty;
            string empty3 = string.Empty;
            if (Menu.data != null)
            {
                Menu menu = (Menu)Menu.data;
                table = menu.TABLE1;
                detailTable = menu.TABLE2;
                string pick_TABLE_MASTER = menu.PICK_TABLE_MASTER;
                string pick_TABLE_DETAIL = menu.PICK_TABLE_DETAIL;
                string dctype = menu.DCTYPE;
            }
            try
            {
                string query = "";
                if (menuDetails.REPORT_NAME == "AdvanceProduction")
                {
                    query = $@"EXEC PROC_PRINT '{table}','{detailTable}','','','{common.Branch}','{common.Period}','{modelRecord.TRAN_ID}','','','{menuDetails.REPORT_NAME}'";

                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {
                        SqlCommand sqlCommand = new SqlCommand(query, connection);
                        connection.Open();
                        SqlDataReader reader = sqlCommand.ExecuteReader();
                        if (reader.Read())
                        {
                            masterData.HEADER_NAME = (menuDetails.MD_NAME ?? "");
                            masterData.REPORT_NAME = (menuDetails.REPORT_NAME ?? "");
                            masterData.COMPANY_NAME = ((reader["C_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["C_NAME"]));
                            masterData.B_NAME = ((reader["B_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["B_NAME"]));
                            masterData.B_TERMS = ((reader["B_TERMS"] == DBNull.Value) ? "" : Convert.ToString(reader["B_TERMS"]));
                            masterData.COMPANY_ADDRESS = ((reader["B_ADDRESS"] == DBNull.Value) ? "" : Convert.ToString(reader["B_ADDRESS"]));
                            masterData.COMPANY_PHONE = ((reader["B_TEL"] == DBNull.Value) ? "" : Convert.ToString(reader["B_TEL"]));
                            masterData.B_WEBSITE = ((reader["B_WEBSITE"] == DBNull.Value) ? "" : Convert.ToString(reader["B_WEBSITE"]));
                            masterData.EMAIL = ((reader["EMAIL"] == DBNull.Value) ? "" : Convert.ToString(reader["EMAIL"]));
                            masterData.B_GST = ((reader["B_GST"] == DBNull.Value) ? "" : Convert.ToString(reader["B_GST"]));
                            masterData.B_NTN = ((reader["B_NTN"] == DBNull.Value) ? "" : Convert.ToString(reader["B_NTN"]));
                            masterData.SIG1 = ((reader["MENU_SIG1"] == DBNull.Value) ? "" : Convert.ToString(reader["MENU_SIG1"]));
                            masterData.SIG2 = ((reader["MENU_SIG2"] == DBNull.Value) ? "" : Convert.ToString(reader["MENU_SIG2"]));
                            masterData.SIG3 = ((reader["MENU_SIG3"] == DBNull.Value) ? "" : Convert.ToString(reader["MENU_SIG3"]));
                            masterData.SIG4 = ((reader["MENU_SIG4"] == DBNull.Value) ? "" : Convert.ToString(reader["MENU_SIG4"]));
                            masterData.MENU_TERMS = ((reader["MENU_TERMS"] == DBNull.Value) ? "" : Convert.ToString(reader["MENU_TERMS"]));
                            masterData.COMPANY_LOGO = currentCompany.C_LOGO;
                            masterData.INVOICE_NUMBER = Convert.ToString(reader["VOUCHER_NO"]);
                            masterData.DATE = ((reader["V_DATE"] == DBNull.Value) ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("dd-MM-yyyy"));
                            masterData.USER = ((reader["USER_NAME"] == DBNull.Value) ? "" : Convert.ToString(reader["USER_NAME"]));
                            masterData.STATUS = Convert.ToString(reader["ASTATUS"]);
                            masterData.RAW_ITEM = ((reader["RAW_ITEM"] == DBNull.Value) ? "" : Convert.ToString(reader["RAW_ITEM"]));
                            masterData.WASTAGE_ITEM = ((reader["WASTAGE_ITEM"] == DBNull.Value) ? "" : Convert.ToString(reader["WASTAGE_ITEM"]));
                            masterData.REF = ((reader["REF"] == DBNull.Value) ? "" : Convert.ToString(reader["REF"]));
                            masterData.WAREHOUSE = ((reader["M_WAREHOUSE"] == DBNull.Value) ? "" : Convert.ToString(reader["M_WAREHOUSE"]));
                            masterData.LOT = ((reader["M_LOT"] == DBNull.Value) ? "" : Convert.ToString(reader["M_LOT"]));
                            //masterData.PROCESS = ((reader["PROCESS"] == DBNull.Value) ? "" : Convert.ToString(reader["PROCESS"]));
                            masterData.BQTY = ((reader["M_QTY"] == DBNull.Value) ? "" : Convert.ToString(reader["M_QTY"]));
                            masterData.REMARKS = ((reader["REMARKS"] == DBNull.Value) ? "" : Convert.ToString(reader["REMARKS"]));
                            masterData.WASTAGE = ((reader["WASTAGE"] == DBNull.Value) ? 0 : Convert.ToDecimal(reader["WASTAGE"]));
                            masterData.WASTAGE_QTY = ((reader["WASTAGE_QTY"] == DBNull.Value) ? 0 : Convert.ToDecimal(reader["WASTAGE_QTY"]));
                        }
                        reader.Close();
                    }
                    using (SqlConnection connection2 = new SqlConnection(new SQLService().getconnstring()))
                    {
                        SqlCommand sqlCommand2 = new SqlCommand(query, connection2);
                        connection2.Open();
                        SqlDataReader reader2 = sqlCommand2.ExecuteReader();
                        while (reader2.Read())
                        {
                            DataRow dataRow = dataTable.NewRow();
                            dataRow["ITEM"] = Convert.ToString(reader2["FINISH_ITEM"]);
                            dataRow["QTY"] = Convert.ToString(reader2["D_QTY"]);
                            dataRow["UNIT"] = Convert.ToString(reader2["UNIT"]);
                            //dataRow["WAREHOUSE"] = Convert.ToString(reader2["D_WAREHOUSE"]);
                            //dataRow["LOT"] = Convert.ToString(reader2["D_LOT"]);
                            //dataRow["LOSS"] = Convert.ToString(reader2["LOSS"]);
                            dataRow["DESC"] = Convert.ToString(reader2["DT_DESC"]);
                            dataRow["WASTAGE_RATE"] = Convert.ToString(reader2["WASTAGE_RATE"]);
                            dataTable.Rows.Add(dataRow);
                        }
                        reader2.Close();
                    }
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
                    _catchMessage = _catchMessage + "<br/>" + ex.InnerException.Message;
                }
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }

    }
}















using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Core.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Empire_ERP.Infrastructure.Repositories
{
    public class AccountingMappingRepository : IAccountingMappingRepository
    {

        public IBranchRepository _branchRepository { get; set; }
        public ICommonRepository _commonRepository { get; set; }
        public IMenuRepository _menuRepository { get; set; }
        public AccountingMappingRepository(IBranchRepository branchRepository, ICommonRepository commonRepository, IMenuRepository menuRepository)
        {
            _branchRepository = branchRepository;
            _commonRepository = commonRepository;
            _menuRepository = menuRepository;
        }


        public MyHttpResponseMessage QuickSearch(MyHttpResponseMessage Menu, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = string.Empty;
                int? pType;
                var menu = (Menu)Menu.data;
                table = menu.TABLE1;
                pType = menu.PTYPE;
                List<object> jsonDataResult = new List<object>();

                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {

                    string query = $@"SELECT Distinct S.TRAN_ID, IT.ITEM_NAME,  S.ITEM_CODE, 
                                        CASE WHEN S.ASTATUS = 'Y' THEN 'Active' ELSE 'In-Active' END AS ASTATUS, S.REMARKS
                                        FROM {table} S
                                        INNER JOIN TBL_ITEMSMASTER IT ON IT.ITEM_CODE = S.ITEM_CODE
                                        WHERE S.DLT = 'T' 
                                        ORDER BY S.TRAN_ID DESC";



                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new
                        {
                            TRAN_ID = Convert.ToString(reader["TRAN_ID"]),
                            ITEM_NAME = Convert.ToString(reader["ITEM_NAME"]),
                            REMARKS = Convert.ToString(reader["REMARKS"]),
                            ASTATUS = Convert.ToString(reader["ASTATUS"]),
                            ITEM_CODE = Convert.ToInt32(reader["ITEM_CODE"]),
                        };
                        jsonDataResult.Add(row);
                    }

                    reader.Close();
                }

                response.data = jsonDataResult;
                response.msg = "";
                response.msgType = 1;
                return response;
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
                string maxIdQuery = $"SELECT ISNULL(MAX(TRAN_ID), 0) + 1 FROM {table}";
                command.CommandText = maxIdQuery;
                object result = command.ExecuteScalar();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {

            }
            return 0;
        }


        private int GenerateNextDetailId(SqlCommand command, Menu menu)
        {
            try
            {
                string? table = menu.TABLE1;
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

        public MyHttpResponseMessage Save(List<AccountingMapping> modelRecord, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = menu.TABLE1;
                var ip = common.IPAddress;
                var computerName = common.ComputerName;
                var postalCode = common.PostalCode;
                var userid = common.Username;
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
                        string query = "", voucherNo = string.Empty;
                        bool IsMasterAdded = true, IsDetailAdded = true, IsNew = false;
                        int code = 0, dt_code = 0;
                        var isInserted = false;
                        var isFirst = true;
                        //int? lastParty = 0;
                        //int? lastAct = 0;
                        foreach (var item in modelRecord)
                        {
                            isInserted = false;

                            try
                            {
                                if (item.TRAN_ID == null || item.TRAN_ID == 0)
                                {
                                    if (isFirst)
                                    {
                                        string checkQuery = @$"SELECT COUNT(*) FROM {table} WHERE ITEM_CODE = '{item.ITEM_CODE}' AND DLT = 'T' AND ASTATUS = 'Y'";
                                        command.CommandText = checkQuery;
                                        int existingCounts = (int)command.ExecuteScalar();

                                        if (existingCounts > 0)
                                        {
                                            response.msg = "This Item already exists..! Please select another Item to proceed.";
                                            response.msgType = 2;
                                            return response;
                                        }
                                    }
                                    

                                    if (code == 0)
                                    {
                                        code = GenerateNextId(common, command, menu);
                                        item.TRAN_ID = code;
                                        if (code <= 0)
                                        {
                                            IsMasterAdded = false;
                                        }
                                    }
                                    if (dt_code == 0)
                                    {
                                        dt_code = GenerateNextDetailId(command, menu);
                                    }
                                    else
                                    {
                                        dt_code++;
                                    }


                                    query = $"INSERT INTO {table} " +
                                            "(TRAN_ID, ITEM_CODE, REMARKS, BCODE, ADD_USER_ID, ADD_DATE, ADD_COMPUTER_NAME, ADD_IP_ADDRESS, EDIT_USER_ID, EDIT_DATE, " +
                                            "EDIT_COMPUTER_NAME, EDIT_IP_ADDRESS, ADD_POSTALCODE, EDIT_POSTALCODE, ASTATUS, MENU_ID, DLT,DT_CODE, ACT_SETUP, COA) " +
                                            $"VALUES " +
                                            $"('{code}', '{item.ITEM_CODE}', '{item.REMARKS}', '{common.Branch}', " +
                                            $"'{userid}', '{CommonService.GetDateTime("Pakistan Standard Time")}', " +
                                            $"'{computerName}', '{ip}', " +
                                            $"'{userid}', '{CommonService.GetDateTime("Pakistan Standard Time")}', '{computerName}', " +
                                            $"'{ip}', '{postalCode}', '{postalCode}', " +
                                            $"'{item.ASTATUS}', '{menuID}', 'T',{dt_code}, '{item.ACT_SETUP}', '{item.COA}')";

                                    command.CommandText = query;
                                    command.ExecuteNonQuery();
                                }
                                else
                                {
                                    if (isFirst)
                                    {
                                        string checkQuery = @$"SELECT COUNT(*) FROM {table} WHERE ITEM_CODE = '{item.ITEM_CODE}' AND DLT = 'T' AND ASTATUS = 'Y'";
                                        command.CommandText = checkQuery;
                                        int existingCounts = (int)command.ExecuteScalar();

                                        if (existingCounts > 0)
                                        {
                                            response.msg = "This Item already exists..! Please select another Item to proceed.";
                                            response.msgType = 2;
                                            return response;
                                        }
                                    }

                                    if (item.DT_CODE == null || item.DT_CODE == 0)
                                    {

                                        string checkQuerys = @$"SELECT COUNT(*) FROM {table} WHERE ITEM_CODE = '{item.ITEM_CODE}' AND TRAN_ID = '{item.TRAN_ID}' AND DLT = 'T'";
                                        command.CommandText = checkQuerys;
                                        int existingCount = (int)command.ExecuteScalar();

                                        if (existingCount > 0)
                                        {
                                            throw new Exception("This item is already assigned to this Party");
                                        }
                                        dt_code = GenerateNextDetailId(command, menu);
                                        query = $"INSERT INTO {table} " +
                                                "(TRAN_ID, ITEM_CODE, REMARKS, BCODE, ADD_USER_ID, ADD_DATE, ADD_COMPUTER_NAME, ADD_IP_ADDRESS, EDIT_USER_ID, EDIT_DATE, " +
                                                "EDIT_COMPUTER_NAME, EDIT_IP_ADDRESS, ADD_POSTALCODE, EDIT_POSTALCODE, ASTATUS, MENU_ID, DLT,DT_CODE, ACT_SETUP, COA) " +
                                                $"VALUES " +
                                                $"('{code}', '{item.ITEM_CODE}', '{item.REMARKS}', '{common.Branch}', " +
                                                $"'{userid}', '{CommonService.GetDateTime("Pakistan Standard Time")}', " +
                                                $"'{computerName}', '{ip}', " +
                                                $"'{userid}', '{CommonService.GetDateTime("Pakistan Standard Time")}', '{computerName}', " +
                                                $"'{ip}', '{postalCode}', '{postalCode}', " +
                                                $"'{item.ASTATUS}', '{menuID}', 'T',{dt_code}, '{item.ACT_SETUP}', '{item.COA}')";

                                        command.CommandText = query;
                                        command.ExecuteNonQuery();
                                        isInserted = true;

                                    }
                                    else
                                    {
                                        var query2 = $"UPDATE {table} SET DLT = 'F' " +
                                                     $"WHERE TRAN_ID = '{item.TRAN_ID}' AND DT_CODE = '{item.DT_CODE}'";
                                        command.CommandText = query2;
                                        command.ExecuteNonQuery();

                                        if (!isInserted)
                                        {
                                            string checkQuery = $@"SELECT COUNT(*) FROM {table} WHERE ACT_SETUP = '{item.ACT_SETUP}' AND DLT = 'T' AND ASTATUS = 'Y' AND NOT (TRAN_ID = '{item.TRAN_ID}' AND DT_CODE = '{item.DT_CODE}')";
                                            command.CommandText = checkQuery;
                                            int existingCount = (int)command.ExecuteScalar();

                                            if (existingCount > 0)
                                            {
                                                throw new Exception("Accout Setup should be different in each record.");
                                            }
                                        }

                                        query = $"UPDATE {table} SET " +
                                                 $"COA = '{item.COA}', " +
                                                 $"ACT_SETUP = '{item.ACT_SETUP}', " +
                                                 $"EDIT_USER_ID = '{userid}', " +
                                                 $"EDIT_DATE = '{CommonService.GetDateTime("Pakistan Standard Time")}', " +
                                                 $"EDIT_COMPUTER_NAME = '{computerName}', " +
                                                 $"EDIT_IP_ADDRESS = '{ip}', " +
                                                 $"EDIT_POSTALCODE = '{postalCode}', " +
                                                 $"ASTATUS = '{item.ASTATUS}', " +
                                                 $"MENU_ID = '{menuID}', " +
                                                 $"DLT = 'T' " +
                                                 $"WHERE TRAN_ID = '{item.TRAN_ID}' AND DT_CODE = '{item.DT_CODE}'";

                                        command.CommandText = query;
                                        command.ExecuteNonQuery();
                                    }

                                }
                                isFirst = false;

                            }
                            catch (Exception ex)
                            {
                                IsDetailAdded = false;
                                response.msg = ex.Message;
                                response.msgType = 2;
                                transaction.Rollback();
                                return response;
                            }
                        }

                        if (IsMasterAdded && IsDetailAdded)
                        {
                            transaction.Commit();
                            response.data = new
                            {
                                code = IsNew ? code : 0,

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


        public MyHttpResponseMessage GetAccountingMappingByCode(int code, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = menu.TABLE1;
                List<object> jsonDataResult = new List<object>();
                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {
                    string query = $@"SELECT TRAN_ID, ASTATUS, ITEM_CODE, REMARKS FROM {table} WHERE TRAN_ID = '{code}' AND DLT = 'T' AND ASTATUS = 'Y'";

                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        var row = new
                        {
                            TRAN_ID = Convert.ToString(reader["TRAN_ID"]),
                            ASTATUS = Convert.ToString(reader["ASTATUS"]),
                            ITEM_CODE = Convert.ToInt32(reader["ITEM_CODE"]),
                            REMARKS = Convert.ToString(reader["REMARKS"]),

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

        public MyHttpResponseMessage GetAccountingMappingDetailsByCode(int code, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = menu.TABLE1;
                List<object> jsonDataResult = new List<object>();
                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {
                    string query = @$"SELECT C.ACT_SETUP, C.COA, C.DT_CODE
                                       FROM {table} C
                                       WHERE C.DLT = 'T' AND C.TRAN_ID = '{code}'
                                       ORDER BY C.DT_CODE DESC";


                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new
                        {
                            DT_CODE = reader["DT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DT_CODE"]),
                            ACT_SETUP = Convert.ToInt32(reader["ACT_SETUP"]),
                            COA = Convert.ToInt32(reader["COA"]),
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
                    string query = $"UPDATE {table} SET DLT = 'F' WHERE TRAN_ID = '{code}' AND BCODE = '{common.Branch}'";
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

        public MyHttpResponseMessage CopyRecord(CopyRecordSalesman record, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            response.msgType = 2;
            response.msg = "Data not found in our records";
            try
            {
                string? table = menu.TABLE1;
                string? table2 = menu.TABLE2;
                string connectionString = new SQLService().getconnstring();
                List<AccountingMapping> AccountingMapping = new List<AccountingMapping>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = $@"SELECT * FROM {table} WHERE TRAN_ID = {record.TRAN_ID}  AND DLT = 'T' AND ASTATUS = 'Y'";

                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new AccountingMapping
                        {
                            //TRAN_ID = 0,
                            ITEM_CODE = Convert.ToInt32(record.ITEM_CODE),
                            REMARKS = Convert.ToString(reader["REMARKS"]),
                            ACT_SETUP = Convert.ToInt32(reader["ACT_SETUP"]),
                            COA = Convert.ToInt32(reader["COA"]),
                            ASTATUS = "Y"
                        };
                        AccountingMapping.Add(row);
                    }
                    reader.Close();
                    connection.Close();
                }
                response = this.Save(AccountingMapping, common, menu);
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


        public MyHttpResponseMessage DeleteAccountingMappingDetailByCode(int gcode, int code, Common common, Menu menu)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = menu.TABLE1;
                var branch = common.Branch;
                var period = common.Period;
                string connectionString = new SQLService().getconnstring();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $"UPDATE {table} SET DLT = 'F' " +
                                   $"WHERE TRAN_ID = '{gcode}' AND DT_CODE = '{code}'";
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



    }
}

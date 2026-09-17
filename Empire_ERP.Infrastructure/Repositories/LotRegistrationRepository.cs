using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Core.Services;
using Microsoft.Data.SqlClient;

namespace Empire_ERP.Infrastructure.Repositories
{
    public class LotRegistrationRepository : ILotRegistrationRepository
    {
        public IMenuRepository _menuRepository { get; set; }
        public LotRegistrationRepository(IMenuRepository menuRepository)
        {
            _menuRepository = menuRepository;
        }
        public MyHttpResponseMessage QuickSearch(Common common)
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

                        string query = $@"SELECT A.GROUP_CODE, A.GROUP_NAME, A.GPIC, IT.ITEM_NAME, CC.DESCR AS CC_NAME, W.DESCR AS WAREHOUSE,
                                            CASE 
                                            WHEN A.ASTATUS = 'Y' THEN 'Active' 
                                            ELSE 'In-Active' END ASTATUS
                                            FROM {table} A
                                            LEFT JOIN TBL_WAREHOUSE W ON A.WAREHOUSE = W.CODE
                                            LEFT JOIN TBL_ITEMSMASTER IT ON IT.ITEM_CODE = A.ITEM_CODE
                                            LEFT JOIN TBL_COST_CENTER CC ON CC.CODE = A.CC_ID
                                            WHERE A.DLT = 'T'
                                            ORDER BY A.GROUP_CODE DESC";

                        SqlCommand command = new SqlCommand(query, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            var row = new 
                            {
                                GROUP_CODE = Convert.ToInt32(reader["GROUP_CODE"]),
                                GROUP_NAME = Convert.ToString(reader["GROUP_NAME"]),
                                GPIC = Convert.ToString(reader["GPIC"]),
                                ITEM = Convert.ToString(reader["ITEM_NAME"]),
                                CC_NAME = reader["CC_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["CC_NAME"]),
                                WAREHOUSE = Convert.ToString(reader["WAREHOUSE"]),
                                ASTATUS = Convert.ToString(reader["ASTATUS"]),
                            };

                            jsonDataResult.Add(row);
                        }
                        reader.Close();

                        response.data = jsonDataResult;
                        response.msg = "";
                        response.msgType = 1;
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

        public MyHttpResponseMessage Save(LotRegistration modelRecord, Common common)
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
                    var Ip = common.IPAddress;
                    var Computer = common.ComputerName;
                    var Postal = common.PostalCode;
                    var userid = common.Username;
                    string connectionString = new SQLService().getconnstring();
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        SqlTransaction transaction = connection.BeginTransaction();
                        SqlCommand command = connection.CreateCommand();
                        command.Transaction = transaction;

                        try
                        {
                            string Duplicationquery = "";

                            string query = "";
                            if (modelRecord.GROUP_CODE == 0)
                            {
                                Duplicationquery = $@"SELECT COUNT(*) FROM {table} WHERE GROUP_NAME = '{modelRecord.GROUP_NAME}' AND WAREHOUSE = '{modelRecord.WAREHOUSE}' 
                            AND ITEM_CODE = '{modelRecord.ITEM}' AND  MENU_ID = '{common.MenuID}' AND DLT = 'T'";

                                command.CommandText = Duplicationquery;
                                int count = Convert.ToInt32(command.ExecuteScalar());

                                if (count > 0)
                                {
                                    transaction.Rollback();
                                    response.msgType = 2;
                                    response.msg = "Record with same Group Name, Warehouse & Item already exists!";
                                    return response;
                                }

                                query = "INSERT INTO " + table + " " +
                                            "(GROUP_CODE, GROUP_NAME, GPIC, ITEM_CODE, CC_ID, ADD_USER_ID, ADD_DATE," +
                                            "ADD_IP_ADDRESS,EDIT_USER_ID,EDIT_DATE,EDIT_COMPUTER_NAME," +
                                            "EDIT_IP_ADDRESS,ADD_POSTALCODE,EDIT_POSTALCODE,ASTATUS,WAREHOUSE," +
                                            "ADD_COMPUTER_NAME,MENU_ID,DLT)" +
                                            "VALUES" +
                                            "('" + GenerateNextId(common) + "','" + modelRecord.GROUP_NAME + "','" + modelRecord.GPIC + "','" + modelRecord.ITEM + "','" + modelRecord.CC_ID + "','" + userid + "','" + CommonService.GetDateTime("Pakistan Standard Time") + "'," +
                                            "'" + Ip + "','" + userid + "','" + CommonService.GetDateTime("Pakistan Standard Time") + "','" + Computer + "'," +
                                            "'" + Ip + "','" + Postal + "','" + Postal + "','" + modelRecord.ASTATUS + "','" + modelRecord.WAREHOUSE + "'," +
                                            "'" + Computer + "','" + common.MenuID + "','T')";
                                
                                command.CommandText = query;
                                command.ExecuteNonQuery();


                                transaction.Commit();
                                response.msgType = 1;
                                response.msg = "Record Added Successfully";
                            }
                            else
                            {
                                Duplicationquery = $@"SELECT COUNT(*) FROM {table} WHERE GROUP_NAME = '{modelRecord.GROUP_NAME}' AND WAREHOUSE = '{modelRecord.WAREHOUSE}' 
                                                AND ITEM_CODE = '{modelRecord.ITEM}' AND  MENU_ID = '{common.MenuID}' AND DLT = 'T' AND GROUP_CODE <> '{modelRecord.GROUP_CODE}'";

                                command.CommandText = Duplicationquery;
                                int count = Convert.ToInt32(command.ExecuteScalar());

                                if (count > 0)
                                {
                                    transaction.Rollback();
                                    response.msgType = 2;
                                    response.msg = "Record with same Group Name, Warehouse & Item already exists!";
                                    return response;
                                }


                                query = "UPDATE " + table + " SET GROUP_NAME = '" + modelRecord.GROUP_NAME + @"',
                                            EDIT_USER_ID = '" + userid + @"',
                                            GPIC = '" + modelRecord.GPIC + @"',
                                            WAREHOUSE = '" + modelRecord.WAREHOUSE + @"',
                                            ITEM_CODE = '" + modelRecord.ITEM + @"',
                                            CC_ID = '" + modelRecord.CC_ID + @"',
                                            EDIT_DATE = '" + CommonService.GetDateTime("Pakistan Standard Time") + @"',
                                            EDIT_COMPUTER_NAME = '" + Computer + @"',
                                            EDIT_IP_ADDRESS = '" + Ip + @"',
                                            EDIT_POSTALCODE = '" + Postal + @"',
                                            ASTATUS = '" + modelRecord.ASTATUS + @"'
                                            WHERE GROUP_CODE = '" + modelRecord.GROUP_CODE + "' AND MENU_ID = '" + common.MenuID + "'";
                                command.CommandText = query;
                                command.ExecuteNonQuery();

                                transaction.Commit();
                                response.msgType = 1;
                                response.msg = "Record Updated Successfully";

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
                response.msg = _catchMessage;
                response.msgType = 2;
            }
            return response;
        }

        public string GenerateNextId(Common common)
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
                    string maxIdQuery = "SELECT ISNULL(MAX(GROUP_CODE), 0) + 1 FROM " + table;
                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {
                        SqlCommand command = new SqlCommand(maxIdQuery, connection);
                        connection.Open();
                        object result = command.ExecuteScalar();
                        int nextId = Convert.ToInt32(result);
                        return Convert.ToString(nextId);
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public MyHttpResponseMessage GetLotRegistrationById(int id, Common common)
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
                    using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                    {
                        string query = "SELECT GROUP_CODE,GROUP_NAME,GPIC, CC_ID, ITEM_CODE, ASTATUS,WAREHOUSE " +
                                       "FROM " + table + " " +
                                       "WHERE MENU_ID = '" + common.MenuID + "' AND DLT = 'T' AND GROUP_CODE = '" + id + "'";

                        SqlCommand command = new SqlCommand(query, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            var setupSubType = new
                            {
                                GROUP_CODE = Convert.ToInt32(reader["GROUP_CODE"]),
                                WAREHOUSE = Convert.ToInt32(reader["WAREHOUSE"]),
                                CC_ID = reader["CC_ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CC_ID"]),
                                ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ITEM_CODE"]),
                                GROUP_NAME = Convert.ToString(reader["GROUP_NAME"]),
                                GPIC = Convert.ToString(reader["GPIC"]),
                                ASTATUS = Convert.ToString(reader["ASTATUS"])
                            };

                            response.msg = "";
                            response.msgType = 1;
                            response.data = setupSubType;
                        }
                        reader.Close();
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

        public MyHttpResponseMessage Delete(int id, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            response.msg = "Data not found in our records";
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
                    if (id == 0)
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
                            string query = "UPDATE " + table + " SET DLT = 'F' WHERE GROUP_CODE = '" + id + "'";
                            SqlCommand command = new SqlCommand(query, connection);
                            command.ExecuteNonQuery();
                            response.msg = "Record Deleted Successfully";
                            response.msgType = 1;
                        }
                    }
                }
                else
                {
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
    }
}
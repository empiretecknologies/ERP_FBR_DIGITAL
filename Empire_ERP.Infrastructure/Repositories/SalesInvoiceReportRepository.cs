using Empire_ERP.Core.Entities;
using Empire_ERP.Core.Interfaces;
using Empire_ERP.Core.Services;
using Microsoft.Data.SqlClient;
using System.Drawing;
using ZXing;
using ZXing.QrCode.Internal;
using static iText.Svg.SvgConstants;

namespace Empire_ERP.Infrastructure.Repositories
{
    public class SalesInvoiceReportRepository : ISalesInvoiceReportRepository
    {
        public IMenuRepository _menuRepository { get; set; }
        public SalesInvoiceReportRepository(IMenuRepository menuRepository)
        {
            _menuRepository = menuRepository;
        }

        public MyHttpResponseMessage GetReportTypes(int menuID, string roleType, int? roleId)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                string? table = "TBL_REPORT_TYPES";
                List<object> jsonDataResult = new List<object>();
                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {
                    string query = "";
                    if (roleType == "A")
                    {
                        query = $"SELECT * FROM {table} " +
                                   "WHERE 1 = 1 " +
                                   $"AND M_ID = '{menuID}' " +
                                   "AND DLT = 'T' AND ASTATUS = 'Y' " +
                                   "ORDER BY SNO";
                    }
                    else
                    {
                        query = $"SELECT * FROM {table} " +
                                   "WHERE 1 = 1 " +
                                   $"AND M_ID = '{menuID}' " +
                                   $"AND DLT = 'T' AND ASTATUS = 'Y' AND R_ID IN (SELECT RMENU_ID from TBL_ROLE WHERE ROLE_ID = {roleId} AND MODULE_ID = 2) " +
                                   "ORDER BY SNO";
                    }
                    SqlCommand command = new SqlCommand(query, connection);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new
                        {
                            SNO = Convert.ToInt32(reader["SNO"]),
                            REPORT_NAME = Convert.ToString(reader["REPORT_NAME"]),
                            R_ID = Convert.ToInt32(reader["R_ID"]),
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

        public MyHttpResponseMessage GetReportData(SalesInvoiceReport report, Common common)
        {
            MyHttpResponseMessage response = new MyHttpResponseMessage();
            try
            {
                var Menu = _menuRepository.GetMenu(common.MenuID);
                string? table1 = string.Empty, table2 = string.Empty, table3 = string.Empty, table4 = string.Empty, b_i = string.Empty;
                if (Menu.data != null)
                {
                    var menu = (Menu)Menu.data;
                    table1 = menu.TABLE1;
                    table2 = menu.TABLE2;
                    table3 = menu.PICK_TABLE_MASTER;
                    table4 = menu.PICK_TABLE_DETAIL;
                    b_i = menu.B_I;
                }
                List<CustomSalesInvoiceReport> jsonDataResult = new List<CustomSalesInvoiceReport>();
                List<dynamic> jsonDetailDataResult = new List<dynamic>();
                using (SqlConnection connection = new SqlConnection(new SQLService().getconnstring()))
                {
                    if (report.ReportID == 154 || report.ReportID == 155 || report.ReportID == 157 || report.ReportID == 158)
                    {
                        string query = $"EXEC FBR_SB '{report.ReportID}','{report.FromDate.Value.ToString("yyyy-MM-dd")}','{report.ToDate.Value.ToString("yyyy-MM-dd")}','{common.Branch}','{common.Period}','{report.Item}','{report.PartyCode}','{report.ActCode}'";
                        SqlCommand command = new SqlCommand(query, connection);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        if (report.ReportID == 154 || report.ReportID == 157)
                        {
                            while (reader.Read())
                            {
                                var row = new CustomSalesInvoiceReport
                                {
                                    vDate = reader["V_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                    VoucherNo = reader["VOUCHER_NO"] == DBNull.Value ? "" : Convert.ToString(reader["VOUCHER_NO"]),
                                    PartyName = reader["PARTY_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["PARTY_NAME"]),
                                    Amt = reader["AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["AMT"]),
                                    TaxAmt = reader["TAX_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TAX_AMT"]),
                                    FBR_NO = reader["FBR_NO"] == DBNull.Value ? "" : Convert.ToString(reader["FBR_NO"]),
                                    FBR_TYPE = reader["FBR_TYPE"] == DBNull.Value ? "" : Convert.ToString(reader["FBR_TYPE"]),
                                    FBR_RESPONSE = reader["FBR_RESPONSE"] == DBNull.Value ? "" : Convert.ToString(reader["FBR_RESPONSE"]),
                                    PUSH_DATE = reader["PUSH_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["PUSH_DATE"]).ToString("yyyy-MM-dd"),
                                    PUSH_TIME = reader["PUSH_TIME"] == DBNull.Value ? "" : Convert.ToString(reader["PUSH_TIME"]),
                                    LINK = "/" + Convert.ToString(reader["MENU_PAGE"]) + "?MOID=" + Convert.ToString(reader["MENU_PARENT_CODE"]) + "&Code=" + Convert.ToString(reader["MENU_ID"]),
                                    TRAN_ID = Convert.ToInt32(reader["TRAN_ID"]),

                                };
                                jsonDataResult.Add(row);
                            }
                        }
                        else if (report.ReportID == 155)
                        {
                            while (reader.Read())
                            {
                                var row = new CustomSalesInvoiceReport
                                {
                                    MONTH = reader["MONTH_YEAR"] == DBNull.Value ? "" : Convert.ToString(reader["MONTH_YEAR"]),
                                    Qty = reader["INVOICE_COUNT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["INVOICE_COUNT"]),
                                    Amt = reader["TAXABLE_AMOUNT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TAXABLE_AMOUNT"]),
                                    TaxAmt = reader["SALES_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SALES_TAX"]),
                                    FTAX_AMT = (reader["TOTAL_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TOTAL_TAX"])) - (reader["SALES_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SALES_TAX"])),
                                    TAX_AMT = reader["TOTAL_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TOTAL_TAX"]),
                                    totalSales = reader["INVOICE_AMOUNT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["INVOICE_AMOUNT"]),
                                };
                                jsonDataResult.Add(row);
                            }
                        }
                        else if (report.ReportID == 158)
                        {
                            while (reader.Read())
                            {
                                var row = new CustomSalesInvoiceReport
                                {
                                    PartyName = reader["PARTY_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["PARTY_NAME"]),
                                    Qty = reader["INVOICES"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["INVOICES"]),
                                    Amt = reader["TAXABLE_SALES"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TAXABLE_SALES"]),
                                    TaxAmt = reader["SALES_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SALES_TAX"]),
                                    TAX_AMT = reader["TOTAL_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TOTAL_TAX"]),
                                    totalSales = reader["TOTAL_SALES"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TOTAL_SALES"]),
                                };
                                jsonDataResult.Add(row);
                            }
                        }
                        
                        reader.Close();
                    }

                }

                CalculateBalanceAmount(jsonDataResult, report.ReportID);

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

        public static void CalculateBalanceAmount(IEnumerable<CustomSalesInvoiceReport> jsonDataResult, int? reportId)
        {
            //if (reportId == 88)
            //{
            //    var distinctActCodes = jsonDataResult.Select(v => v.ItemName).Distinct();

            //    foreach (var accountCode in distinctActCodes)
            //    {
            //        var individualAccountVouchers = jsonDataResult.Where(v => v.ItemName == accountCode).ToList();
            //        decimal runningBalance = 0;

            //        foreach (var voucher in individualAccountVouchers)
            //        {
            //            runningBalance += Convert.ToDecimal(voucher.Debit) - Convert.ToDecimal(voucher.Credit);
            //            voucher.Balance2 = runningBalance;
            //        }
            //    }
            //}
        }


    }
}
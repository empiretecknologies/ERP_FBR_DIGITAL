var empr_SalesInvoiceReport = {
    IsCustomSelection: false,
    PartyData: [],
    SalesmanData: [],
    ItemGroupData: [],
    BranchData: [],
    ItemData: [],
    ControlData: [],

    InitEvents() {
        empr_SalesInvoiceReport.InitLotDDL();
        empr_SalesInvoiceReport.InitWarehouseDDL();
        empr_SalesInvoiceReport.InitItemMasterDDL();
        empr_SalesInvoiceReport.InitItemGroupDDL();
        empr_SalesInvoiceReport.InitBranchDDL(Branch);
        empr_SalesInvoiceReport.GetReportTypes();
        empr_SalesInvoiceReport.InitControlDDL();
        empr_SalesInvoiceReport.InitAccountDDL();
        empr_SalesInvoiceReport.InitOrderTypeDDL();

        $('body').on('click', '#BtnGenerate', function () {
            //debugger;
            $("#Loader").show();
            $("#Loader").css('display', 'flex');
            setTimeout(function () {
                if (empr_SalesInvoiceReport.ValidateInfo()) {
                    empr_SalesInvoiceReport.GenerateReport();
                    setTimeout(function () {
                        $("#Loader").hide();
                    }, 500);
                }
            }, 200);
        });

        $('body').on('click', '#BtnUpdate', function () {
            $("#Loader").show();
            $("#Loader").css('display', 'flex');
            setTimeout(function () {
                empr_SalesInvoiceReport.UpdateSodeBookFeedingReport();
                setTimeout(function () {
                    $("#Loader").hide();
                }, 500);
            }, 200);
        });
    },

    UpdateSodeBookFeedingReport() {
        debugger;
        var dataModel = empr_SalesInvoiceReport.UpdateDataToReport();
        console.log(dataModel);
        ajaxHelper.ajaxPostJsonData(dataModel, "/SalesInvoiceReport/UpdateSodeBookFeedingReport", function (data) {
            if (data.msgType == 1) {
                console.log(data);
                empr_SalesInvoiceReport.GenerateReport();
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },

    UpdateDataToReport() {

        var selectedRowKey = $('#GridContainer').dxDataGrid('instance').getSelectedRowKeys();
        var parties = empr_SalesInvoiceReport.PartyData;
        var reportID = '0', sellerPartyCode = '', salesmanCode;
        debugger;
        if (selectedRowKey.length > 0) {
            reportID = selectedRowKey[0].r_ID;
            empr_helper.reportName = selectedRowKey[0].reporT_NAME;
        }

        if (parties.length > 0) {
            var sellerParty = parties.filter(i => i.key == $('#PARTY_NAME').dxSelectBox('option', 'value'));
            if (sellerParty.length > 0) {
                sellerPartyCode = sellerParty[0].partyCode;
            }
        }

        var HIDDEN_FROM_DATE = $("#HIDDEN_FROM_DATE").val();
        var HIDDEN_TO_DATE = $("#HIDDEN_TO_DATE").val();
        var FROM_DATE = $("#FROM_DATE").val();
        var TO_DATE = $("#TO_DATE").val();
        var REPORT_ID = reportID;
        var ITEM = $('#ITEM_MASTER').dxSelectBox('option', 'value');
        var BRANCH = $('#Branch').dxSelectBox('option', 'value');
        var SALESMAN = salesmanCode;
        var PARTYCODE = sellerPartyCode;
        var ACTCODE = $('#CONTROL_NAME').dxSelectBox('option', 'value');
        var SACODE_CODE = $('#SCONTROL_NAME').dxSelectBox('option', 'value');
        var GROUP = $('#ITEM_GROUP').dxSelectBox('option', 'value');
        var CATEGORY = $('#CATEGORY').dxSelectBox('option', 'value');
        var SUBCATEGORY = $('#SUBCATEGORY').dxSelectBox('option', 'value');
        //var BARCODE = $('#BARCODE').dxSelectBox('option', 'value');
        var SIZE = $('#SIZE').dxSelectBox('option', 'value');
        var COLOR = $('#COLOR').dxSelectBox('option', 'value');
        var WAREHOUSE = $('#WAREHOUSE').dxSelectBox('option', 'value');
        var LOT = $('#LOT').dxSelectBox('option', 'value');
        var VC_TYPE = $('#VC_TYPE').dxSelectBox('option', 'value');
        var CONTACT = $("#CMOB").val();

        var record = {
            FROMDATE: FROM_DATE,
            TODATE: TO_DATE,
            HIDDENFROMDATE: HIDDEN_FROM_DATE,
            HIDDENTODATE: HIDDEN_TO_DATE,
            REPORTID: REPORT_ID,
            ITEM: ITEM,
            BRANCH: BRANCH,
            PARTYCODE: PARTYCODE,
            ACTCODE: ACTCODE,
            GROUP: GROUP,
            CATEGORY: CATEGORY,
            SUBCATEGORY: SUBCATEGORY,
            //BARCODE: BARCODE,
            VC_TYPE: VC_TYPE,
            SIZE: SIZE,
            WAREHOUSE: WAREHOUSE,
            LOT: LOT,
            COLOR: COLOR,
            CONTACT: CONTACT,
            SACTCODE: SACODE_CODE,
            SALESMAN: SALESMAN
        }

        var detailRecords = [];
        detailRecords = $('#ReportGridContainer').dxDataGrid('instance').getSelectedRowsData();

        console.log(detailRecords);
        var modelRecord = {
            Master: record,
            Detail: detailRecords
        };
        return modelRecord;
    },

    InitWarehouseDDL: function (selectedValue) {

        $('#WAREHOUSE').dxSelectBox({
            dataSource: Warehouse,
            displayExpr: 'value',
            valueExpr: 'key',
            value: selectedValue,
            searchEnabled: true,
            width: '100%',
            placeholder: 'Search',
            showClearButton: true,
            dropDownOptions: {
                height: 'auto',
            },
            pagingEnabled: true,
            searchTimeout: 500,
            onValueChanged: function (e) {
                let selectedWarehouse = e.value;
                let filteredLots = Lots.filter(x =>
                    x.warehouse === selectedWarehouse
                );
                $("#LOT").dxSelectBox("instance").option("dataSource", filteredLots);
            }

        });
    },
    InitLotDDL: function (selectedValue) {
        $('#LOT').dxSelectBox({
            //dataSource: Lots,
            dataSource: [{ key: null, value: null }],
            displayExpr: 'value',
            valueExpr: 'key',
            value: selectedValue,
            searchEnabled: true,
            width: '100%',
            placeholder: 'Search',
            showClearButton: true,
            dropDownOptions: {
                height: 'auto',
            },
            pagingEnabled: true,
            searchTimeout: 500,
        });
    },

    InitOrderTypeDDL: function (selectedValue) {

        $('#VC_TYPE').dxSelectBox({
            dataSource: empr_helper.ORDER_TYPE,
            displayExpr: 'value',
            valueExpr: 'key',
            value: selectedValue,
            searchEnabled: true,
            width: '100%',
            placeholder: 'Search',
            showClearButton: true,
            dropDownOptions: {
                height: 'auto',
            },
            pagingEnabled: true,
            searchTimeout: 500,
        });
    },

    InitControlDDL(selectedValue) {
        ajaxHelper.ajaxGetJson("/PartyReports/GetControls", function (data) {
            if (data.msgType == 1) {
                empr_SalesInvoiceReport.ControlData = data.data;
                $('#CONTROL_NAME').dxSelectBox({
                    dataSource: data.data,
                    displayExpr: 'value',
                    valueExpr: 'key',
                    value: selectedValue,
                    searchEnabled: true,
                    width: '100%',
                    placeholder: 'Search',
                    showClearButton: true,
                    dropDownOptions: {
                        height: 'auto',
                    },
                    pagingEnabled: true,
                    searchTimeout: 500,
                    onValueChanged: function (e) {
                        if (e.value != '' && e.value != null) {
                            if (!empr_SalesInvoiceReport.IsCustomSelection) {
                                $('#PARTY_NAME').dxSelectBox('instance').option('value', '');
                            }
                        }
                        else {
                            $('#PARTY_NAME').dxSelectBox('instance').option('value', '');
                        }
                    },
                });
            }
            else {
                empr_helper.notify(data.data, data.msgType);
            }
        }, false, true);
    },

    InitAccountDDL(selectedValue) {
        ajaxHelper.ajaxGetJson("/PartyReports/GetParties", function (data) {
            if (data.msgType == 1) {
                empr_SalesInvoiceReport.PartyData = data.data;
                $('#PARTY_NAME').dxSelectBox({
                    dataSource: data.data,
                    displayExpr: 'value',
                    valueExpr: 'key',
                    value: selectedValue,
                    searchEnabled: true,
                    width: '100%',
                    placeholder: 'Search',
                    showClearButton: true,
                    dropDownOptions: {
                        height: 'auto',
                    },
                    pagingEnabled: true,
                    searchTimeout: 500,
                    onValueChanged: function (e) {
                        if (e.value != '' && e.value != null) {
                            var items = e.component._dataSource._items;
                            var item = items.filter(i => i.key == e.value);
                            if (item.length > 0) {
                                var controls = empr_SalesInvoiceReport.ControlData;
                                var control = controls.filter(i => i.key == item[0].accountCode);
                                if (control.length > 0) {
                                    empr_SalesInvoiceReport.IsCustomSelection = true;
                                    $('#CONTROL_NAME').dxSelectBox('instance').option('value', control[0].key);
                                    setTimeout(function () {
                                        empr_SalesInvoiceReport.IsCustomSelection = false;
                                    }, 500);
                                } else {
                                    $('#CONTROL_NAME').dxSelectBox('instance').option('value', null);
                                }
                            }
                        }
                    },
                });
            }
            else {
                empr_helper.notify(data.data, data.msgType);
            }
        }, false, true);
    },

    GetReportTypes() {
        //debugger;
        ajaxHelper.ajaxGetJson("/PartyReports/GetReportTypes", function (data) {
            if (data.msgType == 1) {
                empr_SalesInvoiceReport.InitReportTypeGrid(data.data);
            }
            else {
                empr_helper.notify(data.data, data.msgType);
            }
        }, false, true);
    },

    InitReportTypeGrid(dataSrc) {

        var col = [
            { dataField: 'sno', caption: 'Code', width: '70px' },
            { dataField: 'reporT_NAME', caption: 'Name' },
        ];
        empr_helper.dxGridbindingForReports('#GridContainer', col, dataSrc, "ReportTypes", 'single');
        setTimeout(function () {
            $('#GridContainer').dxDataGrid('instance').selectRowsByIndexes([0]);
        }, 500);
    },

    GetDataToSave() {

        var selectedRowKey = $('#GridContainer').dxDataGrid('instance').getSelectedRowKeys();
        var parties = empr_SalesInvoiceReport.PartyData;
        var Salesmans = empr_SalesInvoiceReport.SalesmanData;
        var reportID = '0', sellerPartyCode = '', salesmanCode;
        debugger;
        if (selectedRowKey.length > 0) {
            reportID = selectedRowKey[0].r_ID;
            empr_helper.reportName = selectedRowKey[0].reporT_NAME;
        }

        if (parties.length > 0) {
            var sellerParty = parties.filter(i => i.key == $('#PARTY_NAME').dxSelectBox('option', 'value'));
            if (sellerParty.length > 0) {
                sellerPartyCode = sellerParty[0].partyCode;
            }
        }

        //if (Salesmans.length > 0) {
        //    var sellerParty = parties.filter(i => i.key == $('#SALESMAN_NAME').dxSelectBox('option', 'value'));
        //    if (sellerParty.length > 0) {
        //        salesmanCode = sellerParty[0].partyCode;
        //    }
        //}

        var ZERO = $('#ZERO').prop('checked') ? 1 : 0;
        var HIDDEN_FROM_DATE = $("#HIDDEN_FROM_DATE").val();
        var HIDDEN_TO_DATE = $("#HIDDEN_TO_DATE").val();
        var FROM_DATE = $("#FROM_DATE").val();
        var TO_DATE = $("#TO_DATE").val();
        var REPORT_ID = reportID;
        var ITEM = $('#ITEM_MASTER').dxSelectBox('option', 'value');
        var BRANCH = $('#Branch').dxSelectBox('option', 'value');
        var SALESMAN = salesmanCode;
        var PARTYCODE = sellerPartyCode;
        var ACTCODE = $('#CONTROL_NAME').dxSelectBox('option', 'value');
        var SACODE_CODE = $('#SCONTROL_NAME').dxSelectBox('option', 'value');
        var GROUP = $('#ITEM_GROUP').dxSelectBox('option', 'value');
        var CATEGORY = $('#CATEGORY').dxSelectBox('option', 'value');
        var SUBCATEGORY = $('#SUBCATEGORY').dxSelectBox('option', 'value');
        //var BARCODE = $('#BARCODE').dxSelectBox('option', 'value');
        var SIZE = $('#SIZE').dxSelectBox('option', 'value');
        var COLOR = $('#COLOR').dxSelectBox('option', 'value');
        var WAREHOUSE = $('#WAREHOUSE').dxSelectBox('option', 'value');
        var LOT = $('#LOT').dxSelectBox('option', 'value');
        var VC_TYPE = $('#VC_TYPE').dxSelectBox('option', 'value');
        var CONTACT = $("#CMOB").val();

        var record = {
            FROMDATE: FROM_DATE,
            TODATE: TO_DATE,
            HIDDENFROMDATE: HIDDEN_FROM_DATE,
            HIDDENTODATE: HIDDEN_TO_DATE,
            REPORTID: REPORT_ID,
            ITEM: ITEM,
            ZERO: ZERO,
            BRANCH: BRANCH,
            PARTYCODE: PARTYCODE,
            ACTCODE: ACTCODE,
            GROUP: GROUP,
            CATEGORY: CATEGORY,
            SUBCATEGORY: SUBCATEGORY,
            //BARCODE: BARCODE,
            VC_TYPE: VC_TYPE,
            SIZE: SIZE,
            WAREHOUSE: WAREHOUSE,
            LOT: LOT,
            COLOR: COLOR,
            CONTACT: CONTACT,
            SACTCODE: SACODE_CODE,
            SALESMAN: SALESMAN
        }
        return record;
    },

    GenerateReport() {
        //debugger;
        var dataModel = empr_SalesInvoiceReport.GetDataToSave();
        console.log("data model : ", dataModel)
        ajaxHelper.ajaxPostJsonData(dataModel, "/SalesInvoiceReport/GenerateReport", function (data) {
            if (data.msgType == 1) {
                empr_SalesInvoiceReport.InitReportGrid(data.data, dataModel.REPORTID);
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },

    InitReportGrid(dataSrc, reportId) {
        console.log('Report ID : ', reportId);
        console.log("Report Data : ", dataSrc);

        var col = [];

        if (reportId == 154) {
            col = [
                { dataField: 'vDate', caption: 'Date', dataType: 'date', format: 'dd/MM/yyyy', width: 100 },
                {
                    dataField: 'voucherNo', caption: 'Transaction #', 
                    cellTemplate: function (container, options) {
                        $('<a>')
                            .addClass('dx-link')
                            .text(options.value)
                            .attr('href', '#')
                            .attr('onclick', 'empr_SalesInvoiceReport.openVoucherPage(' + JSON.stringify(options.data.link) + ', ' + JSON.stringify(options.data.traN_ID) + ')')
                            .appendTo(container);
                    }
                },
                { dataField: 'partyName', caption: 'Party Name', groupIndex: 0 },
                { dataField: 'amt', caption: 'Amount', dataType: 'number', format: '#,##0.##', width: 100 },
                { dataField: 'taxAmt', caption: 'Sales Tax', dataType: 'number', format: '#,##0.##', width: 100 },
                { dataField: 'fbR_NO', caption: 'FBR Invoice#', },
                { dataField: 'fbR_TYPE', caption: 'Scenario',  },
                { dataField: 'pusH_DATE', caption: 'Push Date', dataType: 'date', format: 'dd/MM/yyyy', width: 100 },
                { dataField: 'pusH_TIME', caption: 'Push Time', width: 100 },

            ];
        }

        if (reportId == 155) {
            col = [
                { dataField: 'month', caption: 'Month' },
                { dataField: 'qty', caption: 'Invoices', dataType: 'number', format: '#,##0', width: 100 },
                { dataField: 'amt', caption: 'Taxable Sales', dataType: 'number', format: '#,##0.##' },
                { dataField: 'taxAmt', caption: 'Sales Tax', dataType: 'number', format: '#,##0.##' },
                { dataField: 'ftaX_AMT', caption: 'Further Tax', dataType: 'number', format: '#,##0.##' },
                { dataField: 'taX_AMT', caption: 'Total Tax', dataType: 'number', format: '#,##0.##' },
                { dataField: 'totalSales', caption: 'Total Sales', dataType: 'number', format: '#,##0.##' },
            ];
        }

        if (reportId == 157) {
            col = [
                { dataField: 'vDate', caption: 'Date', dataType: 'date', format: 'dd/MM/yyyy', width: 100 },
                {
                    dataField: 'voucherNo', caption: 'Transaction',
                    cellTemplate: function (container, options) {
                        $('<a>')
                            .addClass('dx-link')
                            .text(options.value)
                            .attr('href', '#')
                            .attr('onclick', 'empr_SalesInvoiceReport.openVoucherPage(' + JSON.stringify(options.data.link) + ', ' + JSON.stringify(options.data.traN_ID) + ')')
                            .appendTo(container);
                    }
                },
                { dataField: 'partyName', caption: 'Party Name', groupIndex: 0 },
                { dataField: 'amt', caption: 'Amount', dataType: 'number', format: '#,##0.##', width: 100 },
                { dataField: 'taxAmt', caption: 'Sales Tax', dataType: 'number', format: '#,##0.##', width: 100 },
                { dataField: 'fbR_RESPONSE', caption: 'FBR Response', },
                { dataField: 'fbR_TYPE', caption: 'Scenario', },
                { dataField: 'pusH_DATE', caption: 'Push Date', dataType: 'date', format: 'dd/MM/yyyy', width: 100 },
                { dataField: 'pusH_TIME', caption: 'Push Time', width: 100 },
            ];
        }

        if (reportId == 158) {
            col = [
                { dataField: 'partyName', caption: 'Party Name / NTN' },
                { dataField: 'qty', caption: 'Invoice Count', dataType: 'number', format: '#,##0', width: 100 },
                { dataField: 'amt', caption: 'Taxable Sales', dataType: 'number', format: '#,##0.##' },
                { dataField: 'taxAmt', caption: 'Sales Tax', dataType: 'number', format: '#,##0.##' },
                { dataField: 'taX_AMT', caption: 'Total Tax', dataType: 'number', format: '#,##0.##' },
                { dataField: 'totalSales', caption: 'Total Sales', dataType: 'number', format: '#,##0.##' },
            ];
        }

        var dynamicNumericFields = [];
        if (reportId == 159) {
            // columns are built dynamically from the columns returned by the DB (PARTY_NAME is always first)
            var dynamicColumns = dataSrc.columns || [];
            dataSrc = dataSrc.rows || [];
            col = dynamicColumns.map(function (name, index) {
                if (index == 0 && String(name).toUpperCase() == 'PARTY_NAME') {
                    return { dataField: 'c0', caption: 'Party Name' };
                }
                dynamicNumericFields.push('c' + index);
                return { dataField: 'c' + index, caption: name, dataType: 'number', format: '#,##0.##' };
            });
        }

        //debugger;
        if ($('#ReportGridContainer').data('dxDataGrid') != undefined) {
            $('#ReportGridContainer').data('dxDataGrid').dispose();
        }
        ////landscape
        if (reportId == 154 || reportId == 155 || reportId == 157 || reportId == 158 || reportId == 159) {
            empr_helper.DxGridBindingForReportsWithSetting_Aging('#ReportGridContainer', col, dataSrc, empr_helper.reportName, false, true);
            if (reportId == 159) {
                empr_SalesInvoiceReport.ApplyDynamicSummary(dynamicNumericFields);
            }
        }
        ////potrait
        if (reportId == 99999) {
            empr_helper.DxGridBindingForReportsWithSetting_Aging('#ReportGridContainer', col, dataSrc, empr_helper.reportName);
        }

        setTimeout(function () {
            ////landscape
            if (reportId == 154 || reportId == 155 || reportId == 157 || reportId == 158 || reportId == 159) {
                empr_helper.DxGridBindingForReportsWithSetting_Aging('#ReportGridContainer', col, dataSrc, empr_helper.reportName, false, true);
                if (reportId == 159) {
                    empr_SalesInvoiceReport.ApplyDynamicSummary(dynamicNumericFields);
                }
            }
            ////potrait
            if (reportId == 99999) {
                empr_helper.DxGridBindingForReportsWithSetting_Aging('#ReportGridContainer', col, dataSrc, empr_helper.reportName);
            }
            
        }, 400);

        setTimeout(function () {

            var selectedRowKey = $('#GridContainer').dxDataGrid('instance').getSelectedRowKeys();
            if (selectedRowKey.length > 0) {
                $('#REPORT_NAME').text(selectedRowKey[0].reporT_NAME);
            }
            $('#OptionTab').removeClass('active');
            $('#OptionTabContent').removeClass('active');
            $('#ViewTab').click();
            $('.tab-pane').removeClass('fade');
            $('#ViewTab').addClass('active')
            $('#ViewTabContent').addClass('active');
        }, 500);
    },

    ApplyDynamicSummary(numericFields) {
        var grid = $('#ReportGridContainer').data('dxDataGrid');
        if (grid == undefined) {
            return;
        }
        var groupItems = [], totalItems = [];
        numericFields.forEach(function (field) {
            groupItems.push({
                column: field,
                summaryType: 'sum',
                displayFormat: '{0}',
                showInGroupFooter: true,
                valueFormat: '#,##0.##'
            });
            totalItems.push({
                column: field,
                summaryType: 'sum',
                displayFormat: '{0}',
                valueFormat: '#,##0.##'
            });
        });
        grid.option('summary', { recalculateWhileEditing: true, groupItems: groupItems, totalItems: totalItems });
    },

    ValidateInfo() {
        //debugger;
        var valid = true;
        var data = empr_SalesInvoiceReport.GetDataToSave();
        var userfromDateObj = new Date(data.FROMDATE);
        var usertoDateObj = new Date(data.TODATE);

        var periodFromDateObj = new Date(data.HIDDENFROMDATE);
        var periodToDateObj = new Date(data.HIDDENTODATE);

        if (data.FROMDATE == "" || data.FROMDATE == null || data.FROMDATE == undefined) {
            empr_helper.notify("Please select FromDate.", 2);
            valid = false;
        } else {
            empr_helper.fromDate = data.FROMDATE;
        }

        if (data.TODATE == "" || data.TODATE == null || data.TODATE == undefined) {
            empr_helper.notify("Please select ToDate.", 2);
            valid = false;
        } else {
            empr_helper.toDate = data.TODATE;
        }

        if (data.FROMDATE > data.TODATE) {
            empr_helper.notify("'To Date' must be greater than or equal to 'From Date'.", 2);
            valid = false;
        }

        if (data.FROMDATE != null && data.HIDDENFROMDATE != null) {

            if (userfromDateObj < periodFromDateObj || userfromDateObj > periodToDateObj) {
                empr_helper.notify("Selected dates are outside the active period.", 2);
                valid = false;
            }
        }

        if (data.TODATE != null && data.HIDDENTODATE != null) {

            if (usertoDateObj > periodToDateObj || usertoDateObj < periodFromDateObj) {
                empr_helper.notify("Selected dates are outside the active period.", 2);
                valid = false;
            }
        }

        if (data.REPORTID == 159 && data.FROMDATE && data.TODATE) {
            var fromParts = data.FROMDATE.split('-');
            var toParts = data.TODATE.split('-');
            var monthsCount = ((parseInt(toParts[0]) - parseInt(fromParts[0])) * 12) + (parseInt(toParts[1]) - parseInt(fromParts[1])) + 1;
            if (monthsCount > 12) {
                empr_helper.notify("Selected date range must not exceed 12 months.", 2);
                valid = false;
            }
        }

        if (data.REPORTID < 1) {
            empr_helper.notify("Please select report type.", 2);
            valid = false;
        }
        $("#Loader").hide();
        return valid;
    },

    InitItemGroupDDL(selectedValue) {
        ajaxHelper.ajaxGetJson("/SalesInvoiceReport/GetItemGroup", function (data) {
            if (data.msgType == 1) {
                empr_SalesInvoiceReport.ItemGroupData = data.data;
                $('#ITEM_GROUP').dxSelectBox({
                    dataSource: data.data,
                    displayExpr: 'value',
                    valueExpr: 'key',
                    value: selectedValue,
                    searchEnabled: true,
                    width: '100%',
                    placeholder: 'Search',
                    showClearButton: true,
                    dropDownOptions: {
                        height: 'auto',
                    },
                    pagingEnabled: true,
                    searchTimeout: 500,
                    onValueChanged: function (e) {
                        if (e.value != '' && e.value != null) {
                            if (!empr_SalesInvoiceReport.IsCustomSelection) {
                                $('#ITEM_MASTER').dxSelectBox('instance').option('value', '');
                            }
                        }
                        else {
                            $('#ITEM_MASTER').dxSelectBox('instance').option('value', '');
                        }
                    },
                });
            }
            else {
                empr_helper.notify(data.data, data.msgType);
            }
        }, false, true);
    },

    InitBranchDDL(selectedValue) {
        ajaxHelper.ajaxGetJson("/SalesInvoiceReport/GetBranch", function (data) {
            if (data.msgType == 1) {
                empr_SalesInvoiceReport.BranchData = data.data;
                $('#Branch').dxSelectBox({
                    dataSource: data.data,
                    displayExpr: 'value',
                    valueExpr: 'key',
                    value: selectedValue,
                    searchEnabled: true,
                    width: '100%',
                    placeholder: 'Search',
                    showClearButton: true,
                    dropDownOptions: {
                        height: 'auto',
                    },
                    pagingEnabled: true,
                    searchTimeout: 500,
                });
            }
            else {
                empr_helper.notify(data.data, data.msgType);
            }
        }, false, true);
    },

    InitItemMasterDDL(selectedValue) {
        ajaxHelper.ajaxGetJson("/SalesInvoiceReport/GetItemMaster", function (data) {
            if (data.msgType == 1) {
                empr_SalesInvoiceReport.ItemData = data.data;
                $('#ITEM_MASTER').dxSelectBox({
                    //dataSource: data.data,
                    displayExpr: 'value',
                    valueExpr: 'key',
                    value: selectedValue,
                    searchEnabled: true,
                    width: '100%',
                    placeholder: 'Search',
                    showClearButton: true,
                    dropDownOptions: {
                        height: 'auto',
                    },
                    dataSource: {
                        store: data.data,
                        paginate: true,
                        pageSize: 50
                    },
                    paging: {
                        enabled: true,
                        pageSize: 50,
                    },
                    pagingEnabled: true,
                    searchTimeout: 500,
                    onValueChanged: function (e) {
                        if (e.value != '' && e.value != null) {
                            var items = e.component._dataSource._items;
                            var item = items.filter(i => i.key == e.value);
                            if (item.length > 0) {
                                var controls = empr_SalesInvoiceReport.ItemGroupData;
                                var control = controls.filter(i => i.key == item[0].groupCode);
                                if (control.length > 0) {
                                    empr_SalesInvoiceReport.IsCustomSelection = true;
                                    $('#ITEM_GROUP').dxSelectBox('instance').option('value', control[0].key);
                                    setTimeout(function () {
                                        empr_SalesInvoiceReport.IsCustomSelection = false;
                                    }, 500);
                                }
                            }
                        }
                    },
                });
            }
            else {
                empr_helper.notify(data.data, data.msgType);
            }
        }, false, true);
    },

    openVoucherPage(link, tran_Id) {
        var newWindow = window.open(link, '_blank');
        newWindow.addEventListener('load', function () {
            setTimeout(function () {
                newWindow.postMessage({ traN_ID: tran_Id }, '*');
            }, 1000);
        });
    },
}
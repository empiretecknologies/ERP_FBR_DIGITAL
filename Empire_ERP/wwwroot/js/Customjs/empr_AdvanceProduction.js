var empr_AdvanceProduction = {
    totalCount: 0,
    rowsCount: 0,
    itemCode: 0,
    CurrentBalanceValue: 0,
    CurrentStock: [],
    processCode: 0,
    InitEvents: function () {
        $(document).ready(function () {
            //localStorage.setItem('AdvanceProduction', '');
            empr_AdvanceProduction.GetCurrentStock();
            empr_AdvanceProduction.InitUnitDDL();
            empr_AdvanceProduction.ResetForm();
            empr_AdvanceProduction.InitWarehouseDDL();
            empr_AdvanceProduction.InitLotDDL();
            empr_AdvanceProduction.InitWastageItemsDDL();

            $('body').on('click', '#BtnQuickSearch', function () {
                empr_AdvanceProduction.InitQuickSearchGrid();
            });

            $('body').on('click', '.elm_edit', function () {
                var id = $(this).attr("reportid");
                empr_helper.selectedBill = id;
                $('#Code').val(id);
                $('.vHide').show();
                $('.modal').modal('hide');
                empr_AdvanceProduction.GetAdvanceProductionByCode(id);
            });

            $('body').on('click', '.elm_print', function () {
                empr_helper.selectedBill = $(this).attr("reportid");
                empr_AdvanceProduction.GeneratePrintReport();
            });

            $("#QTY, #RATE").on("keyup change", function () {

                var qty = parseFloat($("#QTY").val()) || 0;
                var rate = parseFloat($("#RATE").val()) || 0;

                empr_AdvanceProduction.SetWastage(qty);

                if (qty > 0 && rate > 0)
                    $("#AMT").val((qty * rate).toFixed(2));
                else
                    $("#AMT").val("");
            });

            //$("#WASTAGE_QTY").on("input", function () {
            //    debugger;
            //    var wastage = empr_AdvanceProduction.CurrentBalanceValue;
            //    var wastageQty = parseFloat($("#WASTAGE_QTY").val()) || 0;
            //    var result = wastage - wastageQty;
            //    $("#WASTAGE").val(result.toFixed(2));
            //});

            $("body").on("input", "#QTY, #WASTAGE_QTY", function () {
                debugger;
                empr_AdvanceProduction.SetItemBalance();
            });


            $('body').on('click', '.elm_copy', function () {
                var id = $(this).attr("reportid");
                var date = $(this).attr("reportdate");
                var batchName = $(this).attr("reportitemcode");
                //empr_AdvanceProduction.processCode = parseInt($(this).attr("reportprocesscode"));
                swal({
                    title: 'Are you sure you want to Copy this record?',
                    text: "",
                    type: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#0CC27E',
                    cancelButtonColor: '#FF586B',
                    confirmButtonText: 'Yes',
                    cancelButtonText: 'No',
                    confirmButtonClass: 'btn btn-success mr-5',
                    cancelButtonClass: 'btn btn-danger',
                    buttonsStyling: false
                }).then(function () {
                    $('#updated_Date').val(date);
                    empr_helper.selectedBill = id;
                    //empr_AdvanceProduction.InitCopiedFinishItemsDDL(empr_AdvanceProduction.itemCode);
                    //empr_AdvanceProduction.InitCopiedProcessesDDL(empr_AdvanceProduction.processCode);
                    debugger;
                    $('#updatedBatchName').val(batchName);
                    $('#CopyViewModalBOM').modal('show');
                });
            });


            $('body').on('click', '#saveCopiedRecord', function () {
                var abc = {
                    tran_ID: empr_helper.selectedBill,
                    v_DATE: $('#updated_Date').val(),
                    batcH_NAME: $('#updatedBatchName').val(),
                    //finish_ITEM: empr_AdvanceProduction.itemCode,
                    //process: empr_AdvanceProduction.processCode
                }
                console.log('copy data before', abc);
                ajaxHelper.ajaxPostJsonData(abc, "/AdvanceProduction/CopyRecord", function (data) {
                    console.log('saveCopiedRecord', data);
                    empr_helper.notify(data.msg, data.msgType);
                    if (data.msgType == 1) {
                        $('.modal').modal('hide');
                        empr_AdvanceProduction.GetAdvanceProductionByCode(data.data.code);
                    }
                }, false, true);
            });

            $('body').on('click', '#BtnSave', function () {
                if (Permissions != "Admin") {
                    if (!$("#Code").val() && !Permissions.r_ADD) {
                        empr_helper.notify("You are not allowed to add new record !", 2);
                    }
                    else if (($("#Code").val() > 0) && !Permissions.r_EDIT) {
                        empr_helper.notify("You are not allowed to edit records !", 2);
                    } else {
                        if (empr_AdvanceProduction.ValidateInfo()) {
                            empr_AdvanceProduction.SaveInfo();
                        }
                    }
                } else {
                    if (empr_AdvanceProduction.ValidateInfo()) {
                        empr_AdvanceProduction.SaveInfo();
                    }
                }
            });

            $('body').on('click', '#BtnNew', function () {
                empr_AdvanceProduction.ResetForm();
            });

            $('body').on('click', '#BtnDelete', function () {
                empr_AdvanceProduction.Delete();
            });

            $('body').on('click', '#BtnPrint,#BtnGenerateReport', function () {
                empr_AdvanceProduction.GeneratePrintReport();
            });

            if (Permissions != "Admin") {
                !Permissions.r_ADD && $('#BtnNew').hide();
                !Permissions.r_VIEW && $('#BtnQuickSearch').hide();
                !Permissions.r_PRINT && $('.btn-print').hide();
                (!Permissions.r_ADD && !Permissions.r_EDIT) && $('#BtnSave').hide();
            }
        });
    },

    SetItemBalance: function () {
        var totalQty = $('#QTY').val();
        var wasteQty = $('#WASTAGE_QTY').val();
        var gridQtySum = 0;

        var grid = $('#DetailContainer').dxDataGrid('instance');
        grid.getVisibleRows().forEach(function (row) {
            gridQtySum += parseFloat(grid.cellValue(row.rowIndex, 'qty')) || 0;
        });

        $('#WASTAGE').val(totalQty - wasteQty - gridQtySum);

    },


    GetCurrentStock: function () {
        ajaxHelper.ajaxGetJson('/AdvanceProduction/GetCurrentStock', function (data) {
            if (data.length > 0) {
                empr_AdvanceProduction.CurrentStock = data;
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },


    GeneratePrintReport: function () {
        debugger
        empr_AdvanceProduction.InitReportTypeDDL();
        let TRAN_ID = empr_helper.selectedBill;
        let MD_ID = 0;
        let reportName = "";
        //let MD_ID = $('#ReportType').dxSelectBox('option', 'value');
        let selectedItem = $('#ReportType').dxSelectBox('option', 'selectedItem');
        if (selectedItem) {
            MD_ID = selectedItem.mD_ID;
            reportName = selectedItem.reporT_NAME;
        }
        if (TRAN_ID == 0 || TRAN_ID == null || TRAN_ID == undefined || TRAN_ID == "") {
            empr_helper.notify("Please open the bill in edit mode.", 2);
            return;
        }
        var dataModel = {
            TRAN_ID: TRAN_ID,
            MD_ID: MD_ID,
            REPORT_NAME: reportName,
        }
        ajaxHelper.ajaxPostJsonData(dataModel, "/AdvanceProduction/GetPrintReport", function (data) {
            if (data.msgType == 1) {
                $('#ModalBody').empty();
                setTimeout(function () {
                    $('#ReportType').dxSelectBox('instance').option('value', MD_ID);
                    $('#ModalBody').html("<center><object id='objReport' data='" + window.location.origin + data.data + "' width='1100' height='600'></object></center>");
                    $('#ShowReportModal').show();
                    $('#ShowReportModal').modal('show');
                }, 100);
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },
    InitReportTypeDDL: function (selectedValue) {
        ajaxHelper.ajaxGetJson("/AdvanceProduction/GetReportTypes", function (data) {
            if (data.msgType == 1) {
                if (data.data.length > 0) {
                    selectedValue = data.data[0].mD_ID;
                }
                $('#ReportType').dxSelectBox({
                    dataSource: data.data,
                    displayExpr: 'mD_NAME',
                    valueExpr: 'mD_ID',
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
                    },
                });
            }
            else {
                empr_helper.notify(data.data, data.msgType);
            }
        }, false, true);
    },
    ResetForm: function () {
        $('.Record input').not('#ASTATUS, .dx-texteditor-input, #V_DATE').val('');
        $('#REMARKS').val('');
        $('#BtnDelete').hide();
        //$('#ASTATUS').dxSelectBox('instance').option('value', 'Y');
        $('.card-body').removeClass('customHighlightForModifiedCells');
        $('#COMP').prop('checked', false);
        if (Permissions != "Admin") {
            if (Permissions.r_ADD) {
                $('#BtnSave').show();
            } else {
                $('#BtnSave').hide();
            }
        } else {
            $('#BtnSave').show();
        }

        empr_helper.selectedBill = 0;
        empr_AdvanceProduction.CreateGrid([{ __KEY__: empr_AdvanceProduction.GenerateKey(36) }]);
        empr_AdvanceProduction.InitFinishItemsDDL();
        empr_AdvanceProduction.InitWastageItemsDDL();
        empr_AdvanceProduction.InitProcessesDDL();
        empr_AdvanceProduction.InitReportTypeDDL();
        empr_AdvanceProduction.InitWarehouseDDL();
        empr_AdvanceProduction.InitLotDDL();
    },


    


    CreateGrid: function (dataSrc) {
        if (dataSrc.length > 0) {
            empr_AdvanceProduction.rowsCount = dataSrc.length - 1;
        }
        var col = [
            {
                dataField: "Action",
                width: 120,
                alignment: 'center',
                fixed: true,
                fixedPosition: "left",
                allowExporting: false,
                allowEditing: false,
                cellTemplate: function (container, options) {
                    if (Permissions != "Admin") {
                        const copyAction = !Permissions.r_COPY
                            ? ''
                            : `<a href="javascript:;" class="grid-action-icon Clone" onclick="empr_AdvanceProduction.CloneRow(${options.rowIndex})" title="Duplicate"><i class="fa fa-clone"></i></a>`;
                        const addAction = (!Permissions.r_ADD && !Permissions.r_EDIT)
                            ? ''
                            : `<a href="javascript:;" class="grid-action-icon Add" style="margin-left: 8px" onclick="empr_AdvanceProduction.AddRow()" title="Add"><i class="fa fa-add"></i></a>`;
                        const deleteAction = !Permissions.r_DLT
                            ? ''
                            : `<a href="javascript:;" class="grid-action-icon Delete" style="margin-left: 8px" onclick="empr_AdvanceProduction.DeleteRow(${options.rowIndex})" title="Delete"><i class="fa fa-trash"></i></a>`;
                        const actions = `<div class="btn-group btn-group-sm">${copyAction}${addAction}${deleteAction}</div>`;
                        $(actions).appendTo(container);
                    } else {
                        $(`<div class="btn-group btn-group-sm">
                           <a href="javascript:;" class="grid-action-icon" onclick="empr_AdvanceProduction.CloneRow(`+ options.rowIndex + `)" title="Duplicate"><i class="fa fa-clone"></i></a>
                           <a href="javascript:;" class="grid-action-icon" style="margin-left: 8px" onclick="empr_AdvanceProduction.AddRow()" title="Add"><i class="fa fa-add"></i></a>
                           <a href="javascript:;" class="grid-action-icon" style="margin-left: 8px" onclick="empr_AdvanceProduction.DeleteRow(`+ options.rowIndex + `)" title="Delete"><i class="fa fa-trash"></i></a>
                           </div>`).appendTo(container);
                    }
                }
            },
            {
                dataField: 'dT_CODE',
                caption: 'Code',
                visible: false,
            },
            //{
            //    dataField: 'warehouse',
            //    caption: 'Warehouse',
            //    width: 200,
            //    alignment: 'center',
            //    lookup: {
            //        dataSource: {
            //            store: Warehouse,
            //            paginate: true,
            //            pageSize: 50
            //        },
            //        allowClearing: true,
            //        displayExpr: 'value',
            //        valueExpr: 'key',
            //        searchEnabled: true,
            //        showClearButton: true,
            //    },
            //    setCellValue: function (newData, value, currentRowData) {
            //        newData.warehouse = value;
            //    }
            //},
            //{
            //    dataField: 'lot',
            //    caption: 'Lot',
            //    width: 200,
            //    alignment: 'center',
            //    lookup: {
            //        dataSource: function (options) {
            //            let warehouse = options.data?.warehouse;
            //            return Lots.filter(x =>
            //                !warehouse || x.warehouse === warehouse
            //            );
            //        },
            //        allowClearing: true,
            //        displayExpr: 'value',
            //        valueExpr: 'key'
            //    },
            //    setCellValue: async function (newData, value, currentRowData) {
            //        debugger;
            //        newData.lot = value;

            //        let selectedLotObj = Lots.find(x => x.key === value);

            //        if (selectedLotObj) {
            //            newData.iteM_CODE = selectedLotObj.item;

            //            const item = Array.isArray(Items) ? Items.find(x => x.key === newData.iteM_CODE) : null;
            //            if (item) {
            //                newData.unit = item.unit;
            //            }

            //            //var stockRecord = empr_AdvanceProduction.CurrentStock.find(s => s.itemId == newData.iteM_CODE);
            //            //newData.currentStock = stockRecord ? stockRecord.balance : 0;

            //        } else {
            //            newData.iteM_CODE = null;
            //            newData.unit = null;
            //            newData.currentStock = 0;
            //        }
            //        debugger;


            //        let filteredStock = empr_AdvanceProduction.CurrentStock.filter(x =>
            //            String(x.itemId) === String(selectedLotObj.item) &&
            //            String(x.warehouse) === String(currentRowData.warehouse) &&
            //            String(x.lot) === String(newData.lot)
            //        );

            //        if (filteredStock && filteredStock.length > 0) {
            //            var balanceVal = parseFloat(filteredStock[0].balance) || 0;
            //            newData.currentStock = balanceVal;
            //        } else {
            //            newData.currentStock = null;
            //        }
            //    }
            //},
            {
                dataField: 'iteM_CODE',
                caption: 'Finish Item',
                width: 450,
                allowSorting: false,
                alignment: 'center',
                lookup: {
                    dataSource: FinishItems,
                    displayExpr: 'value',
                    valueExpr: 'key',
                    allowClearing: true,
                },
                setCellValue: function (newData, value, currentRowData) {
                    debugger;
                    newData.iteM_CODE = value;

                    // .find() use karein taake direct object mile
                    var selectedItem = FinishItems.find(u => String(u.key) === String(value));
                    if (selectedItem) {
                        newData.unit = selectedItem.unit;
                    }

                    var WAREHOUSE = $("#WAREHOUSE").dxSelectBox('option', 'value');
                    var LOT = $("#LOT").dxSelectBox('option', 'value');

                    if (WAREHOUSE > 0 && LOT > 0) {

                        // .find() se Direct Object milega (Array nahi)
                        let stockRecord = empr_AdvanceProduction.CurrentStock.find(x =>
                            String(x.itemId) === String(value) &&
                            String(x.warehouse) === String(WAREHOUSE) &&
                            String(x.lot) === String(LOT)
                        );

                        if (stockRecord) {
                            // Numbers parse kar lein agar decimal/string format mein calculation chahiye
                            newData.currentStock = Number((parseFloat(stockRecord.balance) || 0).toFixed(2));
                        } else {
                            newData.currentStock = 0;
                        }
                    }
                }
            },
            {
                dataField: 'currentStock',
                caption: 'Current Stock',
                allowEditing: false,
                alignment: 'right',
                cellTemplate: function (container, options) {
                    var value = options.value;
                    var color = value < 0 ? 'red' : 'black';

                    $('<span>')
                        .text(value)
                        .css('color', color)
                        .css('font-weight', value < 0 ? 'bold' : 'normal')
                        .appendTo(container);
                },
            },
            {
                dataField: 'unit',
                caption: 'Unit',
                width: 150,
                //allowEditing: false,
                alignment: 'center',
                lookup: {
                    dataSource: Units,
                    displayExpr: 'value',
                    valueExpr: 'key'
                }
            },
            {
                dataField: 'qty',
                caption: 'Qty',
                alignment: 'right',
                dataType: 'number',
                setCellValue: function (newData, value, currentRowData) {
                    newData.qty = value;
                    debugger;
                    var qty = Number(value) || 0;
                    var rate = Number(currentRowData.rate) || 0;
                    var rawQty = Number($('#QTY').val()) || 0; 

                    newData.amt = qty * rate;

                    if (rawQty > 0) {
                        var percentage = (qty / rawQty) * 100;
                        newData.wastagE_RATE = Number(percentage.toFixed(2)); 
                    } else {
                        newData.wastagE_RATE = 0;
                    }

                    setTimeout(function () {
                        empr_AdvanceProduction.SetWastage(rawQty);
                    }, 0);
                }
            },
            {
                dataField: 'wastagE_RATE',
                caption: 'Production %',
                alignment: 'right',
                dataType: 'number',
                allowEditing: false
            },
            {
                dataField: 'rate',
                caption: 'Rate',
                alignment: 'right',
                dataType: 'number',
                setCellValue: function (newData, value, currentRowData) {
                    newData.rate = value;

                    var qty = Number(currentRowData.qty) || 0;
                    var rate = Number(value) || 0;

                    newData.amt = qty * rate;
                }
            },
            {
                dataField: 'amt',
                caption: 'Amount',
                alignment: 'right',
                dataType: 'number',
                allowEditing: false
            },
            {
                dataField: 'dT_DESC',
                caption: 'Description',
                alignment: 'center',
            }
        ];
        empr_helper.editableDxGridbindingForTransactionsVouchers('#DetailContainer', col, dataSrc, "AdvanceProduction", "iteM_CODE");



        var detailGrid = $('#DetailContainer').dxDataGrid('instance');
        detailGrid.option('onCellValueChanged', function (e) {
            if (e.column.dataField === 'qty') {
                empr_AdvanceProduction.SetItemBalance()
            }
        });
        detailGrid.option('onContentReady', empr_AdvanceProduction.SetItemBalance);
        empr_AdvanceProduction.SetItemBalance();




        if (dataSrc.length == 0) {
            $('#DetailContainer').dxDataGrid('instance').addRow().done(function () {
                $('#DetailContainer').dxDataGrid('instance').saveEditData();
            });
        }


    },
    CloneRow: function (index) {
        if ($('#DetailContainer').dxDataGrid('instance').hasEditData()) {
            $('#DetailContainer').dxDataGrid('instance').saveEditData().done(function () {

                empr_AdvanceProduction.rowsCount += 1;
                const gridInstance = $('#DetailContainer').dxDataGrid('instance');
                var dataSource = gridInstance.option("dataSource");
                if (dataSource.length > 0) {
                    let clonedRowData = $.extend(true, {}, dataSource[index]);
                    if (clonedRowData.hasOwnProperty('dT_CODE')) {
                        delete clonedRowData.dT_CODE;
                    }
                    clonedRowData.__KEY__ = empr_AdvanceProduction.GenerateKey(36);
                    let newDataSource = [clonedRowData].concat(dataSource);
                    gridInstance.option("dataSource", newDataSource);
                    gridInstance.refresh();

                    setTimeout(function () {
                        empr_AdvanceProduction.SetWastage($('#QTY').val());
                    }, 500)
                }
            });
        }
        else {
            empr_AdvanceProduction.rowsCount += 1;
            const gridInstance = $('#DetailContainer').dxDataGrid('instance');
            var dataSource = gridInstance.option("dataSource");
            if (dataSource.length > 0) {
                let clonedRowData = $.extend(true, {}, dataSource[index]);
                if (clonedRowData.hasOwnProperty('dT_CODE')) {
                    delete clonedRowData.dT_CODE;
                }
                clonedRowData.__KEY__ = empr_AdvanceProduction.GenerateKey(36);
                let newDataSource = [clonedRowData].concat(dataSource);
                gridInstance.option("dataSource", newDataSource);
                gridInstance.refresh();

                setTimeout(function () {
                    empr_AdvanceProduction.SetWastage($('#QTY').val());
                }, 500)
            }
        }
    },
    AddRow: function () {
        if ($('#DetailContainer').dxDataGrid('instance').hasEditData()) {
            $('#DetailContainer').dxDataGrid('instance').saveEditData().done(function () {
                empr_AdvanceProduction.rowsCount += 1;
                const gridInstance = $('#DetailContainer').dxDataGrid('instance');
                const dataSource = gridInstance.option("dataSource");

                dataSource.unshift({ __KEY__: empr_AdvanceProduction.GenerateKey(36) });
                gridInstance.option("dataSource", dataSource);
                gridInstance.refresh();
            });
        }
        else {
            empr_AdvanceProduction.rowsCount += 1;
            const gridInstance = $('#DetailContainer').dxDataGrid('instance');
            const dataSource = gridInstance.option("dataSource");

            dataSource.unshift({ __KEY__: empr_AdvanceProduction.GenerateKey(36) });
            gridInstance.option("dataSource", dataSource);
            gridInstance.refresh();
        }
    },
    DeleteRow: function (index) {
        const gridInstance = $('#DetailContainer').dxDataGrid('instance');
        var dataSource = gridInstance.option("dataSource");
        if (dataSource.length > 0) {
            if (dataSource.length > 1) {
                var row = dataSource[index];
                if (row.dT_CODE == '' || row.dT_CODE == null || row.dT_CODE == undefined) {
                    gridInstance.deleteRow(index);
                    empr_AdvanceProduction.rowsCount -= 1;
                    gridInstance.saveEditData();

                    setTimeout(function () {
                        empr_AdvanceProduction.SetWastage($('#QTY').val());
                    }, 500)
                }
                else {
                    swal({
                        title: 'Are you sure you want to remove this record?',
                        text: "You won't be able to revert this!",
                        type: 'warning',
                        showCancelButton: true,
                        confirmButtonColor: '#0CC27E',
                        cancelButtonColor: '#FF586B',
                        confirmButtonText: 'Yes, delete it!',
                        cancelButtonText: 'No, cancel!',
                        confirmButtonClass: 'btn btn-success mr-5',
                        cancelButtonClass: 'btn btn-danger',
                        buttonsStyling: false
                    }).then(function () {
                        ajaxHelper.ajaxPostJsonData({ code: row.dT_CODE }, "/AdvanceProduction/DeleteAdvanceProductionDetailByCode", function (data) {
                            empr_helper.notify(data.msg, data.msgType);
                            if (data.msgType == 1) {
                                gridInstance.deleteRow(index);
                                empr_AdvanceProduction.rowsCount -= 1;
                                gridInstance.saveEditData();
                            }
                        }, false, true);

                        setTimeout(function () {
                            empr_AdvanceProduction.SetWastage($('#QTY').val());
                        }, 500)
                    });
                }
            }
            else {
                empr_helper.notify("You are not allowed to delete the last row.", 2);
            }
        }
    },
    GenerateKey: function (keyLength) {

        var key = "";
        var characters = "abcdef0123456789";
        for (var i = 0; i < keyLength; i++) {
            if (i === 8 || i === 13 || i === 18 || i === 23) {
                key += "-";
            } else {
                key += characters.charAt(Math.floor(Math.random() * characters.length));
            }
        }
        return key;
    },
    //InitFinishItemsDDL: function (selectedValue) {
    //    console.log(selectedValue);
    //    $.ajax({
    //        url: "AdvanceProduction/GetFinishItems",
    //        type: "GET",
    //        success: function (response) {
    //            empr_AdvanceProduction.BindDxDDL("FinishItem", response.data, selectedValue, "key", "value", "Select", function (d) {
    //                $('#FinishItem_Hidden').val(d.value)
    //                if (d.value == null) {
    //                    $('#FinishItem_Hidden').val('');
    //                }
    //            });
    //        }
    //    });
    //},

    InitFinishItemsDDL: function (selectedValue) {
        $('#FinishItem').dxSelectBox({
            dataSource: Items,
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
                debugger;
                var selectedItem = e.component.option("selectedItem");
                var WAREHOUSE = $("#WAREHOUSE").dxSelectBox('option', 'value');
                var LOT = $("#LOT").dxSelectBox('option', 'value');

                if (selectedItem && selectedItem.unit) {
                    var unit = parseInt(selectedItem.unit);
                    $('#UNIT').dxSelectBox("instance").option("value", unit);
                }
                else {
                    $('#UNIT').dxSelectBox("instance").option("value", null);
                }

                let filteredStock = empr_AdvanceProduction.CurrentStock.filter(x =>
                    String(x.itemId) === String(selectedItem?.key) &&
                    String(x.warehouse) === String(WAREHOUSE) &&
                    String(x.lot) === String(LOT)
                );

                if (filteredStock && filteredStock.length > 0) {
                    var balanceVal = parseFloat(filteredStock[0].balance) || 0;
                    $('#STOCK').val(balanceVal.toFixed(2));
                } else {
                    $('#STOCK').val("0.00");
                }
            }
        });
    },

    //InitCopiedFinishItemsDDL: function (selectedValue) {
    //    console.log("Value:", selectedValue, "| Type:", typeof selectedValue);
    //    $.ajax({
    //        url: "AdvanceProduction/GetFinishItems",
    //        type: "GET",
    //        success: function (response) {
    //            empr_AdvanceProduction.BindDxDDL("copiedFinishItem", response.data, selectedValue, "key", "value", "Select", function (d) {
    //                empr_AdvanceProduction.itemCode = d.value
    //                if (d.value == null) {
    //                    $('#copiedFinishItem_Hidden').val('');
    //                }
    //            });
    //        }
    //    });
    //},
    //InitProcessesDDL: function (selectedValue) {
    //    $.ajax({
    //        url: "AdvanceProduction/GetProcesses",
    //        type: "GET",
    //        success: function (response) {
    //            empr_AdvanceProduction.BindDxDDL("Process", response.data, selectedValue, "key", "value", "Select", function (d) {

    //                $('#Process_Hidden').val(d.value)
    //                if (d.value == null) {
    //                    $('#Process_Hidden').val('');
    //                }
    //            });
    //        }
    //    });
    //},

    InitProcessesDDL: function (selectedValue) {
        $('#Process').dxSelectBox({
            dataSource: Processes,
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
            searchTimeout: 500
        });
    },

    //InitCopiedProcessesDDL: function (selectedValue) {
    //    $.ajax({
    //        url: "AdvanceProduction/GetProcesses",
    //        type: "GET",
    //        success: function (response) {
    //            empr_AdvanceProduction.BindDxDDL("copiedProcess", response.data, selectedValue, "key", "value", "Select", function (d) {
    //                empr_AdvanceProduction.processCode = d.value
    //                if (d.value == null) {
    //                    $('#copiedProcess_Hidden').val('');
    //                }
    //            });
    //        }
    //    });
    //},
    BindDxDDL: function (divId, data, selectedvalues, keyExp, dataField, placeholder, onvalueChangeFun) {
        ati_dxHelper.createDropdownSingle(divId, data, selectedvalues, keyExp, dataField, placeholder, onvalueChangeFun);
    },
    InitQuickSearchGrid: function () {
        empr_AdvanceProduction.GetAdvanceProductions();
    },
    GetAdvanceProductions: function () {
        ajaxHelper.ajaxGetJson('/AdvanceProduction/GetAdvanceProductions', function (data) {
            if (data.msgType == 1) {
                empr_AdvanceProduction.CreateQuickSearchGrid(data.data);
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },
    CreateQuickSearchGrid: function (dataSrc) {
        console.log(dataSrc);
        var col = [{
            dataField: "Action",
            width: 100,
            alignment: 'center',
            fixed: true,
            fixedPosition: "left",
            allowExporting: false,
            cellTemplate: function (container, options) {
                debugger;

                $(`<div class="btn-group btn-group-sm">
                       <a href="javascript:;"  class="grid-action-icon elm_edit" reportid=${options.data.traN_ID} title="Edit"><i class="fa fa-edit"></i></a>
                       <a href="javascript:;"  class="grid-action-icon elm_print" style="margin-left: 8px" reportid=${options.data.traN_ID} title="PRINT"><i class="fa fa-print"></i></a>
                       <a href="javascript:;"  class="grid-action-icon elm_copy" style="margin-left: 8px" reportid=${options.data.traN_ID} reportdate=${options.data.v_DATE} reportitemcode='${options.data.batcH_NAME}' title="COPY"><i class="fa fa-copy"></i></a>
                       </div>`).appendTo(container);
            }
        },
        { dataField: 'traN_ID', caption: 'Code', alignment: 'center' },
        { dataField: 'v_DATE', caption: 'Voucher Date', dataType: 'date', format: 'dd-MM-yyy' },
        { dataField: 'astatus', caption: 'Status', },
        { dataField: 'voucheR_NO', caption: 'Voucher No', },
        { dataField: 'ref', caption: 'Reference No', },
        { dataField: 'iteM_NAME', caption: 'Item Name', },
        //{ dataField: 'batcH_NAME', caption: 'Batch Name', },
        //{ dataField: 'cost', caption: 'Cost%', },
        //{ dataField: 'loss', caption: 'Normal Loss%', },
        //{ dataField: 'process', caption: 'Process', },
        { dataField: 'remarks', caption: 'Remarks', },
        { dataField: 'adD_USER_ID', caption: 'Created By', visible: false, },
        { dataField: 'adD_DATE', caption: 'Created Date', dataType: 'date', visible: false, format: 'dd-MM-yyy' },
        { dataField: 'adD_COMPUTER_NAME', caption: 'Created Computer', visible: false, },
        { dataField: 'adD_POSTALCODE', caption: 'Created Postal Code', visible: false, },
        { dataField: 'adD_IP_ADDRESS', caption: 'Created IP', visible: false, },
        { dataField: 'ediT_USER_ID', caption: 'Updated By', visible: false, },
        { dataField: 'ediT_DATE', caption: 'Updated Date', dataType: 'date', visible: false, format: 'dd-MM-yyy' },
        { dataField: 'ediT_COMPUTER_NAME', caption: 'Updated Computer', visible: false, },
        { dataField: 'ediT_IP_ADDRESS', caption: 'Updated IP', visible: false, },
        { dataField: 'ediT_POSTALCODE', caption: 'Updated Postal Code', visible: false, },
        ];
        empr_helper.dxGridbindingVouchers('#gridContainer', col, dataSrc, "AdvanceProductionQS");
    },
    GetDataToSave: function () {

        var CODE = $("#Code").val();
        var V_DATE = $("#V_DATE").val();
        var ASTATUS = $('#ASTATUS').dxSelectBox('option', 'value');
        var VOUCHER_NO = $("#VOUCHER_NO").val();
        var ITEM_CODE = $('#FinishItem').dxSelectBox('option', 'value');
        var UNIT = $('#UNIT').dxSelectBox('option', 'value');
        var WAREHOUSE = $("#WAREHOUSE").dxSelectBox('option', 'value');
        var WASTAGE_CODE = $("#WASTAGE_CODE").dxSelectBox('option', 'value');
        var LOT = $("#LOT").dxSelectBox('option', 'value');
        var QTY = $("#QTY").val();
        var WASTAGE_QTY = $("#WASTAGE_QTY").val();
        var RATE = $("#RATE").val();
        var AMT = $("#AMT").val();
        var REF = $("#REF").val();
        var WASTAGE = $("#WASTAGE").val();
        var REMARKS = $("#REMARKS").val();
        var COMP = $('#COMP').prop('checked') ? 1 : 0;

        //var COSTPERCENT = $("#CostPercent").val();
        //var NORMALLOSSPERCENT = $("#NormalLossPercent").val();
        //var PROCESS = $('#Process').dxSelectBox('option', 'value'); 
        //var ITEM_CODE = $('#FinishItem_Hidden').val();
        //var PROCESS = $('#Process_Hidden').val();
        //var BQTY = $('#BatchQuantity').val();

        var masterRecord = {
            TRAN_ID: CODE,
            V_DATE: V_DATE,
            ASTATUS: ASTATUS,
            VOUCHER_NO: VOUCHER_NO,
            ITEM_CODE: ITEM_CODE,
            WASTAGE_CODE: WASTAGE_CODE,
            WAREHOUSE: WAREHOUSE,
            LOT: LOT,
            UNIT: UNIT,
            QTY: QTY,
            WASTAGE_QTY: WASTAGE_QTY,
            RATE: RATE,
            AMT: AMT,
            REF: REF,
            WASTAGE: WASTAGE,
            REMARKS: REMARKS,
            COMP: COMP,
        }

        var detailRecords = [];
        if ($('#DetailContainer').dxDataGrid('instance').hasEditData()) {
            $('#DetailContainer').dxDataGrid('instance').saveEditData().done(function () {
                detailRecords = $('#DetailContainer').dxDataGrid('instance').option("dataSource");
            });
        }
        else {
            detailRecords = $('#DetailContainer').dxDataGrid('instance').option("dataSource");
        }
        if (empr_AdvanceProduction.rowsCount == detailRecords.length) {
            var modelRecord = {
                Master: masterRecord,
                Detail: detailRecords
            };
            return modelRecord;
        }
        else {
            var modelRecord = {
                Master: masterRecord,
                Detail: $('#DetailContainer').dxDataGrid('instance').option("dataSource")
            };
            return modelRecord;
        }
    },
    ValidateInfo: function () {

        var valid = true;
        var data = empr_AdvanceProduction.GetDataToSave();

        //if (data.Master.ITEM_CODE == '') {
        //    empr_helper.notify("Item is required.", 2);
        //    valid = false;
        //    return valid;
        //}

        if (data.Master.ITEM_CODE == undefined) {
            empr_helper.notify("Finish Item is required.", 2);
            valid = false;
            return valid;
        }

        //if (data.Master.BATCH_NAME == '') {
        //    empr_helper.notify("Batch Name is required.", 2);
        //    valid = false;
        //    return valid;
        //}

        //if (data.Master.PROCESS == undefined) {
        //    empr_helper.notify("Process is required.", 2);
        //    valid = false;
        //    return valid;
        //}

        //if (data.Master.AMT == '') {
        //    empr_helper.notify("Batch quantity is required.", 2);
        //    valid = false;
        //    return valid;
        //}


        data.Detail = $('#DetailContainer').dxDataGrid('instance').option("dataSource");

        if (data.Detail.length == 0) {
            empr_helper.notify("Please add items.", 2);
            valid = false;
            return valid;
        }

        $.each(data.Detail, function (index, item) {
            if (item.iteM_CODE == "" || item.iteM_CODE == null || item.iteM_CODE == undefined) {
                empr_helper.notify("Please select item at line No " + (index + 1), 2);
                valid = false;
                return valid;
            }
            //if (item.qty == "" || item.qty == null || item.qty == undefined) {
            //    empr_helper.notify("Please enter item quantity at line No " + (index + 1), 2);
            //    valid = false;
            //    return valid;
            //}

            //if (item.qty <= 0) {
            //    empr_helper.notify("Please enter correct item quantity at line No " + (index + 1), 2);
            //    valid = false;
            //    return valid;
            //}

            //if (item.qty != "" && item.qty != null && item.qty != undefined && item.qty < 0) {
            //    empr_helper.notify("Please enter correct item quantity2 at line No " + (index + 1), 2);
            //    valid = false;
            //    return valid;
            //}
        });

        return valid;
    },
    SaveInfo: function () {
        var dataModel = empr_AdvanceProduction.GetDataToSave();
        if (dataModel.Master.TRAN_ID == 0
            || dataModel.Master.TRAN_ID == null
            || dataModel.Master.TRAN_ID == undefined
            || dataModel.Master.TRAN_ID == "") {
            dataModel.Detail.reverse();
        }
        console.log('Save Attempt : ', dataModel);
        ajaxHelper.ajaxPostJsonData(dataModel, "/AdvanceProduction/Save", function (data) {
            empr_helper.notify(data.msg, data.msgType);
            console.log('after save', data);
            if (data.msgType == 1) {
                if (dataModel.Master.TRAN_ID == 0
                    || dataModel.Master.TRAN_ID == null
                    || dataModel.Master.TRAN_ID == undefined) {

                    empr_helper.selectedBill = data.data.code;
                    $('#Code').val(data.data.code);
                    $('#VOUCHER_NO').val(data.data.voucherNo);
                }
                empr_AdvanceProduction.GetCurrentStockAndEditData(data.data.code);
                $('#BtnDelete').show();
            }
        }, false, true);
    },

    GetCurrentStockAndEditData: function (tranId) {
        ajaxHelper.ajaxGetJson('/AdvanceProduction/GetCurrentStock', function (data) {
            debugger;
            if (data.length > 0) {
                empr_AdvanceProduction.CurrentStock = data;
                empr_AdvanceProduction.GetAdvanceProductionByCode(tranId);
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },

    GetAdvanceProductionByCode: function (code) {
        ajaxHelper.ajaxGetJson('/AdvanceProduction/GetAdvanceProductionByCode?code=' + code, function (data) {
            if (data.master.msgType == 1) {
                console.log('edit', data);
                var masterData = data.master.data;
                if (masterData.length == 1) {
                    var response = masterData[0];
                    $('#Code').val(response.traN_ID);
                    $('#ASTATUS').dxSelectBox('instance').option('value', response.astatus);
                    if ($('#FinishItem').data('dxSelectBox') != null) {
                        $('#FinishItem').dxSelectBox('instance').dispose();
                    }
                    if ($('#Process').data('dxSelectBox') != null) {
                        $('#Process').dxSelectBox('instance').dispose();
                    }
                    debugger;
                    $('#REF').val(response.ref);
                    $('#BATCH_NAME').val(response.batcH_NAME);
                    $('#REMARKS').val(response.remarks);
                    $('#V_DATE').val(response.v_DATE);
                    $('#QTY').val(response.qty);
                    $('#WASTAGE_QTY').val(response.wastagE_QTY);
                    $('#WASTAGE').val(response.wastage);
                    $('#RATE').val(response.rate);
                    $('#AMT').val(response.amt);
                    $('#VOUCHER_NO').val(response.voucheR_NO);
                    $('#CostPercent').val(response.cost);
                    $('#COMP').prop('checked', response.comp === 1);




                    let filteredStock = empr_AdvanceProduction.CurrentStock.filter(x =>
                        String(x.itemId) === String(response.iteM_CODE) &&
                        String(x.warehouse) === String(response.warehouse) &&
                        String(x.lot) === String(response.lot)
                    );

                    if (filteredStock && filteredStock.length > 0) {
                        var balanceVal = parseFloat(filteredStock[0].balance) || 0;
                        $('#STOCK').val(balanceVal.toFixed(2));
                    } else {
                        $('#STOCK').val("0.00");
                    }


                    //$('#NormalLossPercent').val(response.loss);
                    //$('#BatchQuantity').val(response.bqty);
                    //$('#FinishItem_Hidden').val(response.iteM_CODE);
                    //$('#Process_Hidden').val(response.process);
                    debugger;
                    empr_AdvanceProduction.InitFinishItemsDDL(parseInt(response.iteM_CODE));
                    //empr_AdvanceProduction.InitWastageItemsDDL(parseInt(response.wastagE_CODE));
                    empr_AdvanceProduction.InitUnitDDL(parseInt(response.unit));
                    $('#WASTAGE_CODE').dxSelectBox('instance').option('value', parseInt(response.wastagE_CODE));
                    $('#WAREHOUSE').dxSelectBox('instance').option('value', parseInt(response.warehouse));
                    $('#LOT').dxSelectBox('instance').option('value', parseInt(response.lot));
                    //empr_AdvanceProduction.InitLotDDL(response.lot);
                    //$('#BtnDelete').show();
                    if (Permissions != "Admin") {
                        if (Permissions.r_DLT) {
                            $('#BtnDelete').show();
                        }
                        if (Permissions.r_EDIT) {
                            $('#BtnSave').show();
                        }
                        else {
                            $('#BtnSave').hide();
                        }
                    } else {
                        $('#BtnSave').show();
                        $('#BtnDelete').show();
                    }
                }
                debugger;




                if (data.detail.msgType == 1) {
                    var updatedDetailData = data.detail.data.map(item => {
                        let stockRecord = empr_AdvanceProduction.CurrentStock.find(s =>
                            String(s.itemId) === String(item.iteM_CODE) &&
                            String(s.warehouse) === String(response.warehouse) &&
                            String(s.lot) === String(response.lot)
                        );

                        return {
                            ...item,
                            currentStock: stockRecord ? Number((parseFloat(stockRecord.balance) || 0).toFixed(2)) : 0
                        };
                    });

                    empr_AdvanceProduction.CreateGrid(updatedDetailData, false);
                }
                else {
                    empr_helper.notify(data.detail.msg, data.detail.msgType);
                }

            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },
    GetAdvanceProductionDetailsByCode: function (code) {
        ajaxHelper.ajaxGetJson('/AdvanceProduction/GetAdvanceProductionDetailByCode?code=' + code, function (data) {
            if (data.msgType == 1) {
                empr_AdvanceProduction.CreateGrid(data.data);
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },
    Delete: function () {

        swal({
            title: 'Are you sure you want to remove this record?',
            text: "You won't be able to revert this!",
            type: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#0CC27E',
            cancelButtonColor: '#FF586B',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'No, cancel!',
            confirmButtonClass: 'btn btn-success mr-5',
            cancelButtonClass: 'btn btn-danger',
            buttonsStyling: false
        }).then(function () {
            ajaxHelper.ajaxPostJsonData({ code: $('#Code').val() }, "/AdvanceProduction/Delete", function (data) {
                empr_helper.notify(data.msg, data.msgType);
                if (data.msgType == 1) {
                    empr_AdvanceProduction.ResetForm();
                    $('#BtnDelete').hide();
                }
            }, false, true);
        });
    },

    InitUnitDDL: function (selectedValue) {

        $('#UNIT').dxSelectBox({
            dataSource: Units,
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
                debugger;
                let selectedWarehouse = e.value;
                let filteredLots = Lots.filter(x =>
                    x.warehouse === selectedWarehouse
                );
                //empr_WarehouseTransfer.LOT_FROM = filteredLots;
                $("#LOT").dxSelectBox("instance").option("dataSource", filteredLots);
            }

        });
    },

    InitWastageItemsDDL: function (selectedValue) {
        if (WastageItems.length > 0) {
            if (selectedValue == undefined || selectedValue == null) {
                selectedValue = WastageItems[0].key;
            }
        }
        
        $('#WASTAGE_CODE').dxSelectBox({
            dataSource: WastageItems,
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
            searchTimeout: 0
        });
    },

    InitLotDDL: function (selectedValue) {
        //empr_WarehouseTransfer.LOT_FROM = Lots;
        $('#LOT').dxSelectBox({
            dataSource: null,
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
            searchTimeout: 0,
            onValueChanged: function (e) {
                debugger;
                let selectedLot = e.value;
                let filteredLots = Lots.filter(x =>
                    x.key === selectedLot
                );

                empr_AdvanceProduction.InitFinishItemsDDL(filteredLots[0].item);

                //$("#FinishItem").dxSelectBox("instance").option("dataSource", filteredLots[0].item);
            }
        });
    },
}
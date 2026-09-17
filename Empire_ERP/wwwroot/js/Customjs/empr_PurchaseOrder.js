$(document).keydown(function (e) {
    if ((e.ctrlKey || e.metaKey) && e.key === 'd') {
        e.preventDefault();
        if ($("#Code").val() != '') {
            empr_PurchaseOrder.Delete();
        } else {
            empr_helper.notify("Please select any record for delete..", 2);
        }
        return false;
    }
    if ((e.altKey || e.metaKey) && e.key === 'a') {
        e.preventDefault();
        $("#SCODE").dxSelectBox("instance")?.option("value", "");

        empr_PurchaseOrder.ResetForm();
        return false;
    }
    if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
        e.preventDefault();
        $('.card .modal').modal('show');
        return false;
    }
    if (e.key === 'Enter') {
        e.preventDefault();
        return false;
    }
});

var empr_PurchaseOrder = {
    totalCount: 0,
    formName: typeForm,
    rowsCount: 0,
    pickIds: [],
    vDate: '',
    CurrentStock: [],
    originalValues: {},

    InitEvents: function () {
        $(document).ready(function () {
            console.log('PartyType', PartyType);

            empr_PurchaseOrder.GetCurrentStock();
            empr_PurchaseOrder.InitQuickSearchGrid();
            empr_PurchaseOrder.ResetForm();
            empr_PurchaseOrder.InitReportTypeDDL();


            window.addEventListener('message', function (event) {

                if (event.origin !== window.location.origin) {
                    return;
                }
                var data = event.data;
                if (data && data.traN_ID) {
                    empr_helper.selectedBill = data.traN_ID;
                    $('#Code').val(data.traN_ID);
                    empr_PurchaseOrder.GetPurchaseBillByCode(data.traN_ID);
                }
            });

            $('body').on('click', '.elm_print', function () {
                empr_helper.selectedBill = $(this).attr("reportid");
                empr_PurchaseOrder.GeneratePrintReport();
            });

            $('.chkCell').prop('checked', true);

            $('body').on('click', '.elm_edit', function () {
                var id = $(this).attr("reportid");
                $('#Code').val(id);
                $('.modal').modal('hide');
                empr_helper.selectedBill = id;
                empr_PurchaseOrder.GetPurchaseBillByCode(id);
            });

            $('body').on('click', '.elm_copy', function () {
                var id = $(this).attr("reportid");
                var date = $(this).attr("reportdate");
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
                    $('#updatedDate').val(date);
                    empr_helper.selectedBill = id;
                    $('#CopyViewModal').modal('show');
                });
            });

            $('body').on('click', '#docBrowseBtn', function () {
                $('#DOC').val('');
                $('#hdnDOC').val('');
                $('#DOCName').val('');
                $('#DOC').click();
            });

            $('body').on('click', '#saveCopiedRecord', function () {
                ajaxHelper.ajaxPostJsonData({ traN_ID: empr_helper.selectedBill, v_DATE: $('#updatedDate').val() }, "/PurchaseOrder/CopyRecord", function (data) {
                    empr_helper.notify(data.msg, data.msgType);
                    if (data.msgType == 1) {
                        $('.modal').modal('hide');
                        empr_PurchaseOrder.GetPurchaseBillByCode(data.data.code);
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
                        $("#Loader").show();
                        $("#Loader").css('display', 'flex');
                        setTimeout(function () {
                            if (empr_PurchaseOrder.ValidateMainInfo()) {
                                empr_PurchaseOrder.Save();
                            }
                            setTimeout(function () {
                                $("#Loader").hide();
                            }, 500);
                        }, 200);
                    }
                } else {
                    $("#Loader").show();
                    $("#Loader").css('display', 'flex');
                    setTimeout(function () {
                        if (empr_PurchaseOrder.ValidateMainInfo()) {
                            empr_PurchaseOrder.Save();
                        }
                        setTimeout(function () {
                            $("#Loader").hide();
                        }, 500);
                    }, 200);
                }
            });

            $('body').on('click', '#BtnDelete', function () {
                empr_PurchaseOrder.Delete();
            });

            $('body').on('click', '#BtnNew', function () {
                $("#SCODE").dxSelectBox("instance")?.option("value", "");
                empr_PurchaseOrder.ResetForm();
            });

            $('body').on('click', '#BtnQuickSearch', function () {
                empr_PurchaseOrder.InitQuickSearchGrid();
            });

            $('body').on('click', '#BtnPrint,#BtnGenerateReport', function () {
                empr_PurchaseOrder.GeneratePrintReport();
            });

            

            if (Permissions != "Admin") {
                !Permissions.r_ADD && $('#BtnNew').hide();
                !Permissions.r_VIEW && $('#BtnQuickSearch').hide();
                !Permissions.r_PRINT && $('.btn-print').hide();
                (!Permissions.r_ADD && !Permissions.r_EDIT) && $('#BtnSave').hide();
            }
        });
    },

    GetCurrentStock: function () {
        ajaxHelper.ajaxGetJson('/PurchaseOrder/GetCurrentStock', function (data) {
            if (data.length > 0) {
                empr_PurchaseOrder.CurrentStock = data;
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },

    CreateGrid: function (dataSrc, dueCalc) {

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
                            : `<a href="javascript:;" class="grid-action-icon Clone" onclick="empr_PurchaseOrder.CloneRow(${options.rowIndex})" title="Duplicate"><i class="fa fa-clone"></i></a>`;
                        const addAction = (!Permissions.r_ADD && !Permissions.r_EDIT)
                            ? ''
                            : `<a href="javascript:;" class="grid-action-icon Add" style="margin-left: 8px" onclick="empr_PurchaseOrder.AddRow()" title="Add"><i class="fa fa-add"></i></a>`;
                        const deleteAction = !Permissions.r_DLT
                            ? ''
                            : `<a href="javascript:;" class="grid-action-icon Delete" style="margin-left: 8px" onclick="empr_PurchaseOrder.DeleteRow(${options.rowIndex},${options.data.dT_CODE})" title="Delete"><i class="fa fa-trash"></i></a>`;
                        const searchAction = (!Permissions.r_ADD && !Permissions.r_EDIT)
                            ? ''
                            : `<a href="javascript:;" class="grid-action-icon Search" style="margin-left: 8px" onclick="empr_PurchaseOrder.InitBarcodePickGrid()" title="Search"><i class="fa fa-search"></i></a>`;
                        const actions = `<div class="btn-group btn-group-sm">${copyAction}${addAction}${deleteAction}</div>`;
                        $(actions).appendTo(container);
                    }
                    else {
                        $(`<div class="btn-group btn-group-sm">
                           <a href="javascript:;" class="grid-action-icon Clone" onclick="empr_PurchaseOrder.CloneRow(`+ options.rowIndex + `)" title="Duplicate"><i class="fa fa-clone"></i></a>
                           <a href="javascript:;" class="grid-action-icon Add" style="margin-left: 8px" onclick="empr_PurchaseOrder.AddRow()" title="Add"><i class="fa fa-add"></i></a>
                           <a href="javascript:;" class="grid-action-icon Delete" style="margin-left: 8px" onclick="empr_PurchaseOrder.DeleteRow(${options.rowIndex},${options.data.dT_CODE})" title="Delete"><i class="fa fa-trash"></i></a>
                           </div>`).appendTo(container);
                    }
                }
            },
            {
                dataField: 'dT_CODE',
                caption: 'Code',
                visible: false,
            },
            {
                dataField: 'partY_CODE',
                visible: false
            },
            {
                dataField: 'acT_CODE',
                visible: false
            },
            {
                dataField: 'warehouse',
                caption: 'Warehouse',
                width: 200,
                alignment: 'center',
                lookup: {
                    dataSource: {
                        store: Warehouse,
                        paginate: true,
                        pageSize: 50
                    },
                    allowClearing: true,
                    displayExpr: 'value',
                    valueExpr: 'key',
                    searchEnabled: true,
                    showClearButton: true,
                },
                setCellValue: function (newData, value, currentRowData) {
                    newData.warehouse = value;
                }
            },
            {
                dataField: 'lot',
                caption: 'Lot',
                width: 200,
                alignment: 'center',
                lookup: {
                    dataSource: function (options) {
                        let warehouse = options.data?.warehouse;
                        return Lots.filter(x =>
                            !warehouse || x.warehouse === warehouse
                        );
                    },
                    allowClearing: true,
                    displayExpr: 'value',
                    valueExpr: 'key'
                },
                setCellValue: async function (newData, value, currentRowData) {
                    debugger;
                    newData.lot = value;

                    let selectedLotObj = Lots.find(x => x.key === value);

                    if (selectedLotObj) {
                        newData.iteM_CODE = selectedLotObj.item;

                        const item = Array.isArray(Items) ? Items.find(x => x.key === newData.iteM_CODE) : null;
                        if (item) {
                            newData.unit = item.unit;
                        }


                        var warehouse = parseFloat(currentRowData.warehouse) || 0;
                        var lot = newData.lot;
                        if (warehouse > 0 && lot > 0) {

                            let stockRecord = empr_PurchaseOrder.CurrentStock.filter(x =>
                                x.itemId === String(selectedLotObj.item) &&
                                x.warehouse === String(warehouse) &&
                                x.lot === String(lot)
                            );

                            if (stockRecord && stockRecord.length > 0) {
                                newData.currentStock = stockRecord ? stockRecord[0].balance : 0;
                            } else {
                                newData.currentStock = 0;
                            }
                        }

                    } else {
                        newData.iteM_CODE = null;
                        newData.unit = null;
                        newData.currentStock = 0;
                    }
                }
            },
            {
                dataField: 'iteM_CODE',
                caption: 'Item',
                width: 320,
                minWidth: 150,
                //fixed: true,
                lookup: {
                    dataSource: {
                        store: Items,
                        paginate: true,
                        pageSize: 50,
                    },
                    displayExpr: 'value',
                    valueExpr: 'key',
                    allowClearing: true,
                    searchEnabled: true,
                    showClearButton: true,
                },
                setCellValue: function (newData, value, currentRowData) {
                    newData.iteM_CODE = value;
                    
                    const item = Array.isArray(Items) ? Items.find(x => x.key === value) : null;
                    
                    if (item) {
                        newData.unit = item.unit;
                    }
                    else {
                        newData.currentStock = 0;
                    }

                    var warehouse = parseFloat(currentRowData.warehouse) || 0;
                    var lot = parseFloat(currentRowData.lot) || 0;
                    if (warehouse > 0 && lot > 0) {

                        let stockRecord = empr_PurchaseOrder.CurrentStock.filter(x =>
                            String(x.itemId) === String(value) &&
                            String(x.warehouse) === String(warehouse) &&
                            String(x.lot) === String(lot)
                        );

                        debugger;

                        if (stockRecord && stockRecord.length > 0) {
                            var obj = stockRecord[0];
                            newData.currentStock = parseFloat(obj.balance) || 0;
                        } else {
                            newData.currentStock = 0;
                        }
                    }
                },

            },
            {
                dataField: 'currentStock',
                caption: 'Current Stock',
                allowEditing: false,
                alignment: 'right',
                dataType: 'number',
                format: { type: 'fixedPoint', precision: 0 },
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
                dataField: 'qty',
                caption: 'Qty',
                width: 75,
                alignment: 'right',
                setCellValue: function (newData, value, currentRowData) {
                    if (value && typeof value === "string") {
                        var hasInvalidChars = /[^0-9+\-*/().\s]/g.test(value);

                        if (hasInvalidChars) {
                            empr_helper.notify('Invalid character entered. Only numbers and +, -, *, / are allowed.', 2);
                            return;
                        }

                        try {
                            var result = new Function('return ' + value)();

                            if (!isNaN(result) && isFinite(result)) {
                                newData.qty = result;
                            } else {
                                empr_helper.notify('Invalid mathematical expression.', 2);
                            }
                        } catch (e) {
                            empr_helper.notify('Formula error! Please enter a valid expression.', 2);
                        }
                    } else {
                        newData.qty = value;
                    }

                    var rate = parseFloat(currentRowData.rate) || 0;
                    var qty = parseFloat(newData.qty) || 0;

                    if (!isNaN(qty)) {
                        if (!isNaN(rate)) {
                            newData.amt = (newData.qty * rate).toFixed(2);

                        }
                    }
                    else {
                        empr_helper.notify("Please enter the correct quantity.", 2);
                    }

                    newData.bags = (qty / 50).toFixed(2);
                    
                }
            },
            //{
            //    dataField: 'qty',
            //    caption: 'Qty',
            //    width: 75,
            //    dataType: 'number',
            //    format: { type: 'fixedPoint', precision: 2 },
            //    setCellValue: function (newData, value, currentRowData) {

            //        newData.qty = value;
            //        var rate = parseFloat(currentRowData.rate) || 0;
            //        var qty = parseFloat(newData.qty) || 0;

            //        if (!isNaN(qty)) {
            //            if (!isNaN(rate)) {
            //                newData.amt = (newData.qty * rate).toFixed(2);

            //            }
            //        }
            //        else {
            //            empr_helper.notify("Please enter the correct quantity.", 2);
            //        }
            //    }
            //},
            {
                dataField: 'bags',
                caption: 'Bags',
                width: 100,
                dataType: 'number',
                format: { type: 'fixedPoint', precision: 0 }
            },
            {
                dataField: 'unit',
                caption: 'Unit',
                width: 100,
                alignment: 'center',
                defaultCellValue: function () {
                    if (Units && Units.length > 0) {
                        return Units[0].key; 
                    }
                    return null;
                },
                lookup: {
                    dataSource: Units,
                    allowClearing: true,
                    displayExpr: 'value',
                    valueExpr: 'key'
                },

            },
            {
                dataField: 'rate',
                caption: 'Rate',
                width: 80,
                alignment: 'right',
                setCellValue: function (newData, value, currentRowData) {
                    if (value && typeof value === "string") {
                        var hasInvalidChars = /[^0-9+\-*/().\s]/g.test(value);

                        if (hasInvalidChars) {
                            empr_helper.notify('Invalid character entered. Only numbers and +, -, *, / are allowed.', 2);
                            return;
                        }

                        try {
                            var result = new Function('return ' + value)();

                            if (!isNaN(result) && isFinite(result)) {
                                newData.rate = result;
                            } else {
                                empr_helper.notify('Invalid mathematical expression.', 2);
                            }
                        } catch (e) {
                            empr_helper.notify('Formula error! Please enter a valid expression.', 2);
                        }
                    } else {
                        newData.rate = value;
                    }

                    var rate = parseFloat(newData.rate) || 0;
                    var qty = parseFloat(currentRowData.qty) || 0;
                    if (!isNaN(qty) && !isNaN(rate)) {
                        newData.amt = (qty * rate).toFixed(2);
                    } else {
                        newData.amt = 0;
                    }

                }
            },
            //{
            //    dataField: 'rate',
            //    caption: 'Rate',
            //    width: 80,
            //    dataType: 'number',
            //    format: { type: 'fixedPoint', precision: 2 },
            //    setCellValue: function (newData, value, currentRowData) {
            //        newData.rate = value;
            //        var rate = parseFloat(newData.rate) || 0;
            //        var qty = parseFloat(currentRowData.qty) || 0;
            //        if (!isNaN(qty) && !isNaN(rate)) {
            //            newData.amt = (qty * rate).toFixed(2);
            //        } else {
            //            newData.amt = 0;
            //        }

            //    }
            //},
            {
                dataField: 'amt',
                caption: 'Amount',
                width: 100,
                allowEditing: false,
                dataType: 'number',
                format: { type: 'fixedPoint', precision: 2 }
            },
            {
                dataField: 'dT_DESC',
                caption: 'Description',
                width: 400,
                alignment: 'center',
                wordWrapEnabled: true,
            },
            {
                dataField: 'deL_DATE',
                caption: 'Delivery Date',
                alignment: 'center',
                dataType: 'date',
                format: "dd-MM-yyyy",
                //defaultValue: new Date()
            },
            {
                dataField: 'paY_TERM',
                caption: 'Pay Terms',
                width: 70,
                dataType: 'number',
                format: { type: 'fixedPoint', precision: 0 },
                setCellValue: function (newData, value, currentRowData) {
                    newData.paY_TERM = value;
                    let deliveryDate = currentRowData.deL_DATE || new Date();
                    if (deliveryDate && value) {
                        let dueDate = new Date(deliveryDate);
                        dueDate.setDate(dueDate.getDate() + parseInt(value));
                        newData.duE_DATE = dueDate;
                    }
                }
            },
            {
                dataField: 'duE_DATE',
                caption: 'Due Date',
                alignment: 'center',
                dataType: 'date',
                format: "dd-MM-yyyy",
                setCellValue: function (newData, value, currentRowData) {
                    newData.duE_DATE = value;
                    let deliveryDate = currentRowData.deL_DATE || new Date();
                    if (deliveryDate && value) {
                        let d1 = new Date(deliveryDate);
                        let d2 = new Date(value);
                        d1.setHours(0, 0, 0, 0);
                        d2.setHours(0, 0, 0, 0);
                        let timeDiff = d2.getTime() - d1.getTime();
                        let daysDiff = Math.round(timeDiff / (1000 * 3600 * 24));
                        newData.paY_TERM = daysDiff >= 0 ? daysDiff : 0;
                    }
                }
            },
            {
                dataField: 'inC_EXC',
                caption: 'Inc / Exc',
                width: 100,
                alignment: 'center',
                lookup: {
                    dataSource: empr_helper.Inc_Exc_Data,
                    allowClearing: true,
                    displayExpr: 'value',
                    valueExpr: 'key'
                },

            },
            {
                dataField: 'bparty',
                caption: 'Broker',
                width: 200,
                alignment: 'center',
                lookup: {
                    dataSource: {
                        store: PartyType,
                        paginate: true,
                        pageSize: 50
                    },
                    allowClearing: true,
                    displayExpr: 'value',
                    valueExpr: 'key',
                    searchEnabled: true,
                    showClearButton: true,
                },
                setCellValue: function (newData, value, currentRowData) {
                    newData.bparty = value;

                    if (value) {
                        const items = Array.isArray(PartyType) ? PartyType : (PartyType._array || []);
                        const selectedParty = items.find(item => item.key === value);

                        if (selectedParty) {
                            newData.bpartY_CODE = selectedParty.partyCode;
                            newData.bacT_CODE = selectedParty.accountCode;
                        }
                    } else {
                        newData.bpartY_CODE = null;
                        newData.bacT_CODE = null;
                    }
                }
            },
            {
                dataField: 'comM_TYPE',
                caption: 'Comm.Type',
                width: 120,
                alignment: 'center',
                lookup: {
                    dataSource: {
                        store: empr_helper.comm_Type, 
                        paginate: true,
                        pageSize: 50
                    },
                    allowClearing: true,
                    displayExpr: 'value',
                    valueExpr: 'key',
                    searchEnabled: true,
                    showClearButton: true,
                },

            },
            {
                dataField: 'comM_VALUE',
                caption: 'Comm.Value',
                width: 120,
                dataType: 'number',
                format: { type: 'fixedPoint', precision: 2 }
            },
            
            {
                dataField: 'id',
                visible: false,
            },
            {
                dataField: 'picK_ID',
                caption: 'Pick ID',
                visible: false
            },
            {
                dataField: 'picK_ID_D',
                caption: 'Pick Detail ID',
                visible: false
            },
        ];
        empr_helper.editableDxGridbindingForTransactionsVouchers('#DetailContainer', col, dataSrc, "PurchaseBill", "iteM_CODE");
        if (dataSrc.length == 0) {
            $('#DetailContainer').dxDataGrid('instance').addRow().done(function () {
                $('#DetailContainer').dxDataGrid('instance').saveEditData();
            });
        }

    },

    CloneRow: function (index) {
        const gridIns = $('#DetailContainer').dxDataGrid('instance');
        const dataSrc = gridIns.option("dataSource");

        if (dataSrc.length >= Limit && Limit != 0) {
            empr_helper.notify("You can only add  " + Limit + " records.", 2);
            return;
        }
        if ($('#DetailContainer').dxDataGrid('instance').hasEditData()) {
            $('#DetailContainer').dxDataGrid('instance').saveEditData().done(function () {

                empr_PurchaseOrder.rowsCount += 1;
                const gridInstance = $('#DetailContainer').dxDataGrid('instance');
                var dataSource = gridInstance.option("dataSource");
                if (dataSource.length > 0) {
                    let clonedRowData = $.extend(true, {}, dataSource[index]);
                    if (clonedRowData.hasOwnProperty('dT_CODE')) {
                        delete clonedRowData.dT_CODE;
                    }
                    clonedRowData.__KEY__ = empr_PurchaseOrder.GenerateKey(36);
                    clonedRowData.dT_CODE = 0;
                    let newDataSource = [clonedRowData].concat(dataSource);
                    gridInstance.option("dataSource", newDataSource);
                    gridInstance.refresh();
                }
            });
        }
        else {
            empr_PurchaseOrder.rowsCount += 1;
            const gridInstance = $('#DetailContainer').dxDataGrid('instance');
            var dataSource = gridInstance.option("dataSource");
            if (dataSource.length > 0) {
                let clonedRowData = $.extend(true, {}, dataSource[index]);
                if (clonedRowData.hasOwnProperty('dT_CODE')) {
                    delete clonedRowData.dT_CODE;
                }
                clonedRowData.__KEY__ = empr_PurchaseOrder.GenerateKey(36);
                clonedRowData.dT_CODE = 0;
                let newDataSource = [clonedRowData].concat(dataSource);
                gridInstance.option("dataSource", newDataSource);
                gridInstance.refresh();
            }
        }
    },

    CommCloneRow: function (index) {
        if (empr_PurchaseOrder.pickIds.length > 0) {
            empr_helper.notify("Cannot clone row on return data.", 2);
        } else {
            const gridIns = $('#CommContainer').dxDataGrid('instance');
            const dataSrc = gridIns.option("dataSource");

            if (dataSrc.length >= Limit && Limit != 0) {
                empr_helper.notify("You can only add  " + Limit + " records.", 2);
                return;
            }
            if ($('#CommContainer').dxDataGrid('instance').hasEditData()) {
                $('#CommContainer').dxDataGrid('instance').saveEditData().done(function () {

                    empr_PurchaseOrder.rowsCount += 1;
                    const gridInstance = $('#CommContainer').dxDataGrid('instance');
                    var dataSource = gridInstance.option("dataSource");
                    if (dataSource.length > 0) {
                        let clonedRowData = $.extend(true, {}, dataSource[index]);
                        if (clonedRowData.hasOwnProperty('dT_CODE')) {
                            delete clonedRowData.dT_CODE;
                        }
                        clonedRowData.__KEY__ = empr_PurchaseOrder.GenerateKey(36);
                        clonedRowData.dT_CODE = 0;
                        let newDataSource = [clonedRowData].concat(dataSource);
                        gridInstance.option("dataSource", newDataSource);
                        gridInstance.refresh();
                    }
                });
            }
            else {
                empr_PurchaseOrder.rowsCount += 1;
                const gridInstance = $('#CommContainer').dxDataGrid('instance');
                var dataSource = gridInstance.option("dataSource");
                if (dataSource.length > 0) {
                    let clonedRowData = $.extend(true, {}, dataSource[index]);
                    if (clonedRowData.hasOwnProperty('dT_CODE')) {
                        delete clonedRowData.dT_CODE;
                    }
                    clonedRowData.__KEY__ = empr_PurchaseOrder.GenerateKey(36);
                    clonedRowData.dT_CODE = 0;
                    let newDataSource = [clonedRowData].concat(dataSource);
                    gridInstance.option("dataSource", newDataSource);
                    gridInstance.refresh();
                }
            }
        }
    },

    AddRow: function () {
        const gridIns = $('#DetailContainer').dxDataGrid('instance');
        const dataSrc = gridIns.option("dataSource");

        if (dataSrc.length >= Limit && Limit != 0) {
            empr_helper.notify("You can only add  " + Limit + " records.", 2);
            return;
        }
        if ($('#DetailContainer').dxDataGrid('instance').hasEditData()) {
            $('#DetailContainer').dxDataGrid('instance').saveEditData().done(function () {
                empr_PurchaseOrder.rowsCount += 1;
                const gridInstance = $('#DetailContainer').dxDataGrid('instance');
                const dataSource = gridInstance.option("dataSource");
                let payTerm = parseInt($('#TERMS').val(), 10) || 0;
                var deliveryDate = $('#RINV_DATE').val();

                if (!deliveryDate) {
                    let today = new Date();
                    today.setHours(0, 0, 0, 0);
                    deliveryDate = today;
                }

                const newRow = {
                    __KEY__: empr_PurchaseOrder.GenerateKey(36),
                    dT_CODE: 0,
                    //deL_DATE: todayDate,
                };

                dataSource.unshift(newRow);
                gridInstance.option("dataSource", dataSource);
                gridInstance.refresh();
                empr_helper.MoveFocusToGridWithouTab('#DetailContainer', 0, 'iteM_CODE')
            });
        }
        else {
            empr_PurchaseOrder.rowsCount += 1;
            const gridInstance = $('#DetailContainer').dxDataGrid('instance');
            const dataSource = gridInstance.option("dataSource");

            let payTerm = parseInt($('#TERMS').val(), 10) || 0;

            var deliveryDate = $('#RINV_DATE').val();

            if (!deliveryDate) {
                let today = new Date();
                today.setHours(0, 0, 0, 0);
                deliveryDate = today;
            }

            const newRow = {
                __KEY__: empr_PurchaseOrder.GenerateKey(36),
                dT_CODE: 0,
            };

            dataSource.unshift(newRow);
            gridInstance.option("dataSource", dataSource);
            gridInstance.refresh();
            empr_helper.MoveFocusToGridWithouTab('#DetailContainer', 0, 'iteM_CODE')
        }
    },

    DeleteRow: function (index, dtCode) {
        const gridInstance = $('#DetailContainer').dxDataGrid('instance');
        var dataSource = gridInstance.option("dataSource");
        if (dataSource.length > 0) {
            if (dataSource.length > 1) {
                var row = dataSource[index];
                if (dtCode == '' || dtCode == null || dtCode == undefined) {
                    gridInstance.deleteRow(index);
                    empr_PurchaseOrder.rowsCount -= 1;
                    gridInstance.saveEditData();
                }
                else {
                    var availableRows = dataSource.filter(x => x.dT_CODE > 0);
                    if (availableRows.length > 0) {
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
                            ajaxHelper.ajaxPostJsonData({ code: dtCode }, "/PurchaseOrder/DeletePurchaseBillDetailByCode", function (data) {
                                empr_helper.notify(data.msg, data.msgType);
                                if (data.msgType == 1) {
                                    gridInstance.deleteRow(index);
                                    empr_PurchaseOrder.rowsCount -= 1;
                                    gridInstance.saveEditData();
                                }
                            }, false, true);
                        });
                    } else {
                        empr_helper.notify("You are not allowed to delete the last row.", 2);
                    }
                }
            }
            else {
                empr_helper.notify("You are not allowed to delete the last row.", 2);
            }
        }
    },

    InitQuickSearchGrid: function () {
        empr_PurchaseOrder.GetPurchaseBill();
    },

    GetPurchaseBill: function () {
        ajaxHelper.ajaxGetJson('/PurchaseOrder/GetPurchaseOrder', function (data) {
            if (data.msgType == 1) {
                empr_PurchaseOrder.CreateQuickSearchGrid(data.data);
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },

    CreateQuickSearchGrid: function (dataSrc) {
        var col = [];
        if (dataSrc.length > 0 ? dataSrc[0].amt != undefined : false) {
            col = [{
                dataField: "Action",
                width: 100,
                alignment: 'center',
                fixed: true,
                fixedPosition: "left",
                allowExporting: false,
                cellTemplate: function (container, options) {
                    if (Permissions != "Admin" && !Permissions.r_PRINT) {
                        $(`<div class="btn-group btn-group-sm">
                               <a href="javascript:;"  class="grid-action-icon elm_edit" reportid=${options.data.code} title="Edit"><i class="fa fa-edit"></i></a>
                               </div>`).appendTo(container);
                    } else {
                        $(`<div class="btn-group btn-group-sm">
                               <a href="javascript:;"  class="grid-action-icon elm_edit" reportid=${options.data.code} title="Edit"><i class="fa fa-edit"></i></a>
                               <a href="javascript:;"  class="grid-action-icon elm_print" style="margin-left: 8px" reportid=${options.data.code} title="PRINT"><i class="fa fa-print"></i></a>
                               <a href="javascript:;"  class="grid-action-icon elm_copy" style="margin-left: 8px" reportdate=${options.data.v_DATE} reportid=${options.data.code} title="COPY"><i class="fa fa-copy"></i></a>
                               </div>`).appendTo(container);
                    }
                }
            },
            { dataField: 'id', caption: 'Code', width: 80, alignment: "center" },
            { dataField: 'v_DATE', caption: 'Voucher Date', dataType: 'date', format: 'dd-MM-yyy' },
            { dataField: 'voucheR_NO', caption: 'Voucher No', },
            { dataField: 'partY_NAME', caption: 'Party Name', },
            { dataField: 'warehouse', caption: 'Warehouse', },
            { dataField: 'lot', caption: 'Lot', },
            { dataField: 'iteM_NAME', caption: 'Item Name', },
            { dataField: 'ref', caption: 'Refrence #', },
            { dataField: 'remarks', caption: 'Description', },
            { dataField: 'qty', caption: 'Qty', },
            { dataField: 'rate', caption: 'Rate', },
            { dataField: 'amt', caption: 'Amount', },
            { dataField: 'astatus', caption: 'Status', },
            ];
        }
        else {
            col = [{
                dataField: "Action",
                width: 100,
                alignment: 'center',
                fixed: true,
                fixedPosition: "left",
                allowExporting: false,
                cellTemplate: function (container, options) {


                    $(`<div class="btn-group btn-group-sm">
                               <a href="javascript:;"  class="grid-action-icon elm_edit" reportid=${options.data.code} title="Edit"><i class="fa fa-edit"></i></a>
                               <a href="javascript:;"  class="grid-action-icon elm_print" style="margin-left: 8px" reportid=${options.data.code} title="PRINT"><i class="fa fa-print"></i></a>
                               <a href="javascript:;"  class="grid-action-icon elm_copy" style="margin-left: 8px" reportdate=${options.data.v_DATE} reportid=${options.data.code} title="COPY"><i class="fa fa-copy"></i></a>
                               </div>`).appendTo(container);
                }
            },
            { dataField: 'id', caption: 'Code', width: 80, alignment: "center" },
            { dataField: 'v_DATE', caption: 'Voucher Date', dataType: 'date', format: 'dd-MM-yyy' },
            { dataField: 'voucheR_NO', caption: 'Voucher No', },
            { dataField: 'partY_NAME', caption: 'Party Name', },
            //{ dataField: 'region', caption: 'Region', },
            { dataField: 'ref', caption: 'Refrence #', },
            { dataField: 'remarks', caption: 'Remarks', },
            { dataField: 'astatus', caption: 'Status', },
                //{ dataField: 'rinV_NO', caption: 'R.Inv No', },
                //{ dataField: 'rinV_DATE', caption: 'R.Inv Date', dataType: 'date', format: 'dd-MM-yyy' },
            ]
        }
        //empr_helper.dxGridbindingLazyLoading('#gridContainer', col, "/PurchaseOrder/GetPurchaseBill", "id", "PartyOpening");
        empr_helper.dxGridbindingVouchers('#gridContainer', col, dataSrc, "PurchaseOrder" , "single");
        setTimeout(function () {
            $('#gridContainer').dxDataGrid('instance').resize();
        }, 500);
    },

    GetPurchaseBillByCode: function (code) {
        empr_PurchaseOrder.ResetForm();
        ajaxHelper.ajaxGetJson('/PurchaseOrder/GetPurchaseOrderByCode?code=' + code, function (data) {
            if (data.master.msgType == 1) {
                var masterData = data.master.data;
                console.log('edit Data', data);
                if (masterData.length == 1) {
                    //$('#pickItems').show();
                    //empr_PurchaseOrder.pickIds = [];
                    var response = masterData[0];
                    empr_PurchaseOrder.vDate = response.v_DATE;
                    var filteredData = $.grep(PartyType, function (item) {
                        return item.partyCode === response.partY_CODE && item.accountCode === response.acT_CODE;
                    });
                    //$('#btnLedger').show();
                    $('#Code').val(response.id);
                    $('#ASTATUS').dxSelectBox('instance').option('value', response.astatus);
                    $('#BTYPE').dxSelectBox('instance').option('value', response.btype);
                    $('#ORDER_TYPE').dxSelectBox('instance').option('value', response.ordeR_TYPE);
                    $('#PARTY_CODE').dxSelectBox('instance').option('value', filteredData[0].key);
                    $('#partyhidden').val(filteredData[0].partyCode);

                    $('#REF').val(response.ref);
                    $('#REMARKS').val(response.remarks);
                    
                    $('#V_DATE').val(response.v_DATE);
                    $('#VOUCHER_NO').val(response.voucheR_NO);

                    $('#hdnDOC').val(response.doc);
                    var DocPath = response.doc;
                    var DocName = DocPath.split('/').pop();
                    $("#DOCName").val(DocName);

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
                    }
                    $('#BtnDelete').show();
                }
                if (data.detail.msgType == 1) {
                    var updatedDetailData = data.detail.data.map(item => {
                        let stockRecord = empr_PurchaseOrder.CurrentStock.find(s =>
                            String(s.itemId) === String(item.iteM_CODE) &&
                            String(s.warehouse) === String(item.warehouse) &&
                            String(s.lot || "").trim() === String(item.lot || "").trim()
                        );

                        return {
                            ...item,
                            currentStock: stockRecord ? Number((parseFloat(stockRecord.balance) || 0).toFixed(2)) : 0
                        };
                    });

                    empr_PurchaseOrder.CreateGrid(updatedDetailData, false);
                }
                else {
                    empr_helper.notify(data.detail.msg, data.detail.msgType);
                }

            }
            else {
                empr_helper.notify(data.master.msg, data.master.msgType);
            }
        }, false, true);
    },

    GetPurchaseBillDetailByCode: function (code) {
        ajaxHelper.ajaxGetJson('/PurchaseOrder/GetPurchaseBillDetailByCode?code=' + code, function (data) {
            if (data.msgType == 1) {
                empr_PurchaseOrder.CreateGrid(data.data, false);
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },

    GetDataToSave: function () {
        var ID = $("#Code").val();
        var ASTATUS = $('#ASTATUS').dxSelectBox('option', 'value');
        var V_DATE = $("#V_DATE").val();
        var VOUCHER_NO = $("#VOUCHER_NO").val();
        var PARTY_CODE = $("#partyhidden").val();
        var ACT_CODE = $("#acthidden").val();
        var BTYPE = $("#BTYPE").dxSelectBox('option', 'value');
        var ORDER_TYPE = $("#ORDER_TYPE").dxSelectBox('option', 'value');
        var REF = $("#REF").val();
        var DOC = $("#hdnDOC").val();
        var REMARKS = $("#REMARKS").val();

        var masterRecord = {
            TRAN_ID: ID,
            ASTATUS: ASTATUS,
            V_DATE: V_DATE,
            VOUCHER_NO: VOUCHER_NO,
            PARTY_CODE: PARTY_CODE,
            ACT_CODE: ACT_CODE,
            BTYPE: BTYPE,
            ORDER_TYPE: ORDER_TYPE,
            REF: REF,
            DOC: DOC,
            REMARKS: REMARKS
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
        
        if (empr_PurchaseOrder.rowsCount == detailRecords.length) {
            var modelRecord = {
                Master: masterRecord,
                Detail: detailRecords,
            };
            return modelRecord;
        }
        else {

            var detailRecords = $('#DetailContainer').dxDataGrid('instance').option("dataSource");

            $.each(detailRecords, function (index, item) {
                if (!(item.duE_DATE == "" || item.duE_DATE == null || item.duE_DATE == undefined)) {
                    item.duE_DATE = empr_helper.PrepareDate(item.duE_DATE);
                }

                if (!(item.deL_DATE == "" || item.deL_DATE == null || item.deL_DATE == undefined)) {
                    item.deL_DATE = empr_helper.PrepareDate(item.deL_DATE);
                }
            });

            var modelRecord = {
                Master: masterRecord,
                Detail: detailRecords,
            };
            return modelRecord;
        }
    },

    ValidateMainInfo: function () {

        var valid = true;
        var data = empr_PurchaseOrder.GetDataToSave();
        
        if (data.Master.V_DATE == '') {
            empr_helper.notify("Transaction date is required.", 2);
            valid = false;
            return valid;
        }

        if (data.Master.PARTY_CODE == '') {
            empr_helper.notify("Please select Party.", 2);
            valid = false;
            return valid;
        }

        if (data.Master.BTYPE == '' || data.Master.BTYPE == undefined) {
            empr_helper.notify("Please select Pay Type.", 2);
            valid = false;
            return valid;
        }

        if (data.Master.ORDER_TYPE == '' || data.Master.ORDER_TYPE == undefined) {
            empr_helper.notify("Please select Order Type.", 2);
            valid = false;
            return valid;
        }

        data.Detail = $('#DetailContainer').dxDataGrid('instance').option("dataSource");

        if (data.Detail.length == 0) {
            empr_helper.notify("Please add items.", 2);
            valid = false;
            return valid;
        }

        $.each(data.Detail, function (index, item) {
            debugger;
            if (item.iteM_CODE == "" || item.iteM_CODE == null || item.iteM_CODE == undefined) {
                empr_helper.notify("Please select Item at Line No " + (index + 1), 2);
                valid = false;
                return valid;
            }
            if (item.qty == "" || item.qty == null || item.qty == undefined) {
                empr_helper.notify("Please enter item quantity at Line No " + (index + 1), 2);
                valid = false;
                return valid;
            }

            if (item.qty <= 0) {
                empr_helper.notify("Please enter correct item quantity at Line No " + (index + 1), 2);
                valid = false;
                return valid;
            }

            if (item.rate == "" || item.rate == null || item.rate == undefined) {
                empr_helper.notify("Please enter rate at Line No " + (index + 1), 2);
                valid = false;
                return valid;
            }

            if (item.rate <= 0) {
                empr_helper.notify("Please enter correct rate at Line No " + (index + 1), 2);
                valid = false;
                return valid;
            }

            //if (item.warehouse == "" || item.warehouse == null || item.warehouse == undefined) {
            //    empr_helper.notify("Please select warehouse at Line No " + (index + 1), 2);
            //    valid = false;
            //    return valid;
            //}

            //if (item.lot == "" || item.lot == null || item.lot == undefined) {
            //    empr_helper.notify("Please select lot at Line No " + (index + 1), 2);
            //    valid = false;
            //    return valid;
            //}
        });

        if (data.Detail.length > Limit && Limit != 0) {
            empr_helper.notify("You can only add  " + Limit + " records.", 2);
            valid = false;
            return valid;
        }

        return valid;
    },

    Save: function () {
        var dataModel = empr_PurchaseOrder.GetDataToSave();
        if (dataModel.Master.TRAN_ID == 0
            || dataModel.Master.TRAN_ID == null
            || dataModel.Master.TRAN_ID == undefined
            || dataModel.Master.TRAN_ID == "") {
            dataModel.Detail.reverse();
        }
        console.log('save attempt', dataModel);
        ajaxHelper.ajaxPostJsonData(dataModel, "/PurchaseOrder/Save", function (data) {
            empr_helper.notify(data.msg, data.msgType);
            if (data.msgType == 1) {
                if (dataModel.Master.TRAN_ID == 0
                    || dataModel.Master.TRAN_ID == null
                    || dataModel.Master.TRAN_ID == undefined) {
                    $('#Code').val(data.data.code);
                    empr_helper.selectedBill = data.data.code;
                    $('#VOUCHER_NO').val(data.data.voucherNo);
                }
                if (dataClear == 1) {
                    empr_PurchaseOrder.GetPurchaseBillByCode(data.data.code);
                    $('#BtnDelete').show();
                }
                else {
                    $("#SCODE").dxSelectBox("instance")?.option("value", "");
                    empr_PurchaseOrder.ResetForm();
                }

            }
        }, false, true);
    },

    ResetForm: function () {
        $('.detailInfo a').tab('show');
        empr_PurchaseOrder.CreateGrid([{ __KEY__: empr_PurchaseOrder.GenerateKey(36), dT_CODE: 0, }], true);

        $('#Code').val('');
        $("#hdnDOC").val('');
        $("#DOCName").val('');
        $("#hdnDOC").val('');
        $('#BtnDelete').hide();
        $('#REMARKS').val('');
        $('#REF').val('');
        $('#VOUCHER_NO').val('');


        empr_helper.dcType = dcType;
        empr_PurchaseOrder.pickIds = [];
        empr_PurchaseOrder.InitPartyType();
        empr_PurchaseOrder.InitOrderTypeDDL();

        $('#V_DATE').val(todayDate);
        $('#V_DATE').focus();
        $('#pickItems').show();
        if (Permissions != "Admin") {
            if (Permissions.r_ADD) {
                $('#BtnSave').show();
            } else {
                $('#BtnSave').hide();
            }
        } else {
            $('#BtnSave').show();
        }
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
            ajaxHelper.ajaxPostJsonData({ code: $('#Code').val() }, "/PurchaseOrder/Delete", function (data) {
                empr_helper.notify(data.msg, data.msgType);
                if (data.msgType == 1) {
                    $("#SCODE").dxSelectBox("instance")?.option("value", "");
                    empr_PurchaseOrder.ResetForm();
                    $('#BtnDelete').hide();
                }
            }, false, true);
        });
    },

    UploadDoc: function () {
        $('#BtnSave').prop('disabled', true);
        var files = document.getElementById('DOC').files;
        var formData = new FormData();
        for (var i = 0; i !== files.length; i++) {
            formData.append("model", files[i]);
        }
        $.ajax(
            {
                url: "/Common/UploadVoucherDocs",
                data: formData,
                processData: false,
                contentType: false,
                type: "POST",
                success: function (data) {
                    if (data.msgType == '1') {
                        $("#hdnDOC").val(data.data);
                    }
                    else {
                        empr_helper.notify("Something went wrong while saving the file. please re-upload the file.", data.msgType);
                    }
                    $('#BtnSave').prop('disabled', false);

                }
            }
        );
    },

    OpenDoc: function () {
        var hdnUrl = $('#hdnDOC').val();
        if (hdnUrl == "" || hdnUrl == null) {
            empr_helper.notify("Please upload a file to view.", 2);
        }
        else {
            const fileURL = window.location.origin + hdnUrl;
            window.open(fileURL, '_blank');
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

    InitPartyType: function (selectedValue) {
        $('#PARTY_CODE').dxSelectBox({
            dataSource: new DevExpress.data.DataSource({
                store: PartyType,
                paginate: true,
                pageSize: 30
            }),
            displayExpr: 'value',
            valueExpr: 'key',
            value: selectedValue,
            searchEnabled: true,
            width: '100%',
            placeholder: 'Select',
            showClearButton: true,
            dropDownOptions: {
                height: 150
            },
            searchTimeout: 500,
            onValueChanged: function (d) {
                empr_PurchaseOrder.OnPartyChange(d);
            }
        }); 
    },

    OnPartyChange: function (d) {

        if (d.value == null || d.value == '') {
            $('#partyhidden').val('');
            $('#acthidden').val('');
            $('#PARTY_BALANCE').val('');
        }
        else {
            var filteredData = $.grep(PartyType, function (item) {
                return item.key === d.value;
            });

            $('#partyhidden').val(filteredData[0].partyCode);
            $('#acthidden').val(filteredData[0].accountCode);

            var code = $('#Code').val();
            if (code) {
                ajaxHelper.ajaxGetJson('/PurchaseOrder/GetPartyCurrentBalance?vDate=' + empr_PurchaseOrder.vDate, function (data) {
                    if (data.length > 0) {
                        var newPartyData = $.grep(data, function (item) {
                            return item.key === d.value;
                        });

                        let balance = parseFloat(newPartyData[0].balance || 0);

                        if (balance < 0) {
                            $('#PARTY_BALANCE').val(`(${Math.abs(balance)})`).css('color', 'red');
                        } else {
                            $('#PARTY_BALANCE').val(balance).css('color', 'black');
                        }
                    }
                }, false, true);
            }
            else {
                let balance = parseFloat(filteredData[0].balance || 0);

                if (balance < 0) {
                    $('#PARTY_BALANCE').val(`(${Math.abs(balance)})`).css('color', 'red');
                } else {
                    $('#PARTY_BALANCE').val(balance).css('color', 'black');
                }
            }

        }
    },

    InitReportTypeDDL: function (selectedValue) {
        ajaxHelper.ajaxGetJson("/PurchaseOrder/GetReportTypes", function (data) {
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
                    placeholder: 'Select',
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

    GeneratePrintReport: function () {
        empr_PurchaseOrder.InitReportTypeDDL();
        let TRAN_ID = empr_helper.selectedBill;
        let MD_ID = 0;
        let reportName = "";
        let selectedItem = $('#ReportType').dxSelectBox('option', 'selectedItem');
        let balance = $('#PARTY_BALANCE').val()
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
            BALANCE: balance,
        }
        ajaxHelper.ajaxPostJsonData(dataModel, "/PurchaseOrder/GetPrintReport", function (data) {
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

    UploadDoc: function () {
        $('#saveAttempt').prop('disabled', true);
        var files = document.getElementById('DOC').files;
        if (files.length > 0) {
            $('#DOCName').val(files[0].name);
        }

        var formData = new FormData();
        for (var i = 0; i !== files.length; i++) {
            formData.append("model", files[i]);
        }

        $.ajax({
            url: "/Common/UploadVoucherDocs",
            data: formData,
            processData: false,
            contentType: false,
            type: "POST",
            success: function (data) {
                if (data.msgType == '1') {
                    $("#hdnDOC").val(data.data);
                } else {
                    $('#DOCName').val("No File");
                    empr_helper.notify("Something went wrong while saving the file. Please re-upload.", 2);
                }
                $('#saveAttempt').prop('disabled', false);
            }
        });
    },

    OpenDoc: function () {
        var hdnUrl = $('#hdnDOC').val();
        if (!hdnUrl) {
            empr_helper.notify("Please upload a file to view.", 2);
        } else {
            const fileURL = window.location.origin + hdnUrl;
            window.open(fileURL, '_blank');
        }
    },

    openVoucherPage(link, tran_Id) {
        var newWindow = window.open(link, '_blank');
        newWindow.addEventListener('load', function () {
            setTimeout(function () {
                newWindow.postMessage({ traN_ID: tran_Id }, '*');
            }, 1000);
        });
    },

    InitOrderTypeDDL: function (selectedValue) {


        $('#ORDER_TYPE').dxSelectBox({
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

    InitRegionDDL: function (selectedValue) {

        $('#REGION').dxSelectBox({
            dataSource: Regions,
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
        });
    },

}
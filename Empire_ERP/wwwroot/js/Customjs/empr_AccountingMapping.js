var empr_AccountingMapping = {
    totalCount: 0,
    rowsCount: 0,
    DC_TYPE: '',
    act_code: '',
    InitEvents: function () {
        $(document).ready(function () {
            //console.log('account', Accounts);
            empr_AccountingMapping.ResetForm();
            //empr_AccountingMapping.InitSalesManDDL();
            //PartyType
            //empr_AccountingMapping.InitPartyDDL(null, 'PARTY_CODE');
            empr_AccountingMapping.InitQuickSearchGrid();
            window.addEventListener('message', function (event) {
                if (event.origin !== window.location.origin) {
                    return;
                }
                var data = event.data;
                if (data && data.traN_ID) {
                    $('#Code').val(data.traN_ID);
                    empr_AccountingMapping.GetAccountingMappingByCode(data.traN_ID);
                }
            });
            $('body').on('click', '#BtnQuickSearch', function () {
                empr_AccountingMapping.InitQuickSearchGrid();
            });

            $('body').on('click', '#BtnSave', function () {
                if (Permissions != "Admin") {
                    if (!$("#Code").val() && !Permissions.r_ADD) {
                        empr_helper.notify("You are not allowed to add new record !", 2);
                    }
                    else if (($("#Code").val() > 0) && !Permissions.r_EDIT) {
                        empr_helper.notify("You are not allowed to edit records !", 2);
                    } else {
                        empr_AccountingMapping.ValidateAndPrepareDataForSave();
                    }
                } else {
                    empr_AccountingMapping.ValidateAndPrepareDataForSave();
                }
            });



            $('body').on('click', '.elm_edit', function () {
                var id = $(this).attr("reportid");
                $('#Code').val(id);
                $('.modal').modal('hide');
                empr_helper.selectedBill = id;
                empr_AccountingMapping.GetAccountingMappingByCode(id);
            });

            $('body').on('click', '.elm_copy', function () {
                debugger;
                var tranId = $(this).attr("tranId");
                var itemCode = $(this).attr("itemCode");

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
                    //debugger
                    $('#AccountMappingCopy').modal('show');
                    empr_AccountingMapping.InitCopyItemsDDL(parseInt(itemCode));
                    //PartyModel
                    empr_helper.selectedBill = tranId;
                });
            });

            $('body').on('click', '#saveCopiedRecord', function () {
                debugger;
                var itemCode = $('#COPY_ITEM_CODE').dxSelectBox('option', 'value');

                ajaxHelper.ajaxPostJsonData({ traN_ID: empr_helper.selectedBill, iteM_CODE: itemCode }, "/AccountingMapping/CopyRecord", function (data) {
                    empr_helper.notify(data.msg, data.msgType);
                    if (data.msgType == 1) {
                        $('.modal').modal('hide');
                        empr_AccountingMapping.GetAccountingMappingByCode(data.data.code);
                    }
                }, false, true);
            });

            $('body').on('click', '#BtnDelete', function () {
                empr_AccountingMapping.Delete();
            });

            $('body').on('click', '#BtnNew', function () {
                empr_AccountingMapping.ResetForm();
            });

            //$('body').on('click', '.btn-print,#BtnGenerateReport', function () {
            //    empr_AccountingMapping.GeneratePrintReport();
            //});

            if (Permissions != "Admin") {
                !Permissions.r_ADD && $('#BtnNew').hide();
                !Permissions.r_VIEW && $('#BtnQuickSearch').hide();
                !Permissions.r_PRINT && $('.btn-print').hide();
                (!Permissions.r_ADD && !Permissions.r_EDIT) && $('#BtnSave').hide();
            }
        });
    },
    InitPartyDDL: function (selectedValue, targetId) {
        $('#' + targetId).dxSelectBox({
            dataSource: {
                store: PartyType,
                paginate: true,
                //pageSize: 50
            },
            paging: {
                enabled: true,
                pageSize: 50,
            },
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

                if (!e.value) return
                var selectedItem = PartyType.find(function (x) {
                    return x.key == e.value;
                });
                debugger;
                if (selectedItem) {
                    $("#ACT_CODE").val(selectedItem.accountCode);
                    empr_AccountingMapping.act_code = selectedItem.accountCode;

                }
            }


        });
    },
    //InitPartyType: function () {
    //      debugger;
    //      empr_PurchaseBill.bindDxDdl("PARTY_CODE", PartyType, null, "key", "value", "Select", function (d) {
    //          console.log(d)
    //          if (d.value == null || d.value == '') {
    //              $('#partyhidden').val('');
    //              $('#acthidden').val('');
    //              empr_PurchaseBill.InitSalesman(0);
    //          }
    //          else {
    //              debugger;
    //              var filteredData = $.grep(PartyType, function (item) {
    //                  return item.key === d.value;
    //              });
    //              console.log("Party:" + filteredData);
    //              $('#partyhidden').val(filteredData[0].partyCode)
    //              $('#acthidden').val(filteredData[0].accountCode)
    //              $('#DISC').val(filteredData[0].disc)
    //              if (filteredData[0].partyCode != "" && filteredData[0].partyCode != 0) {
    //                  empr_PurchaseBill.InitSalesman(filteredData[0].partyCode, parseInt(filteredData[0].scode));
    //              }
    //          }

    //      });

    //  },
    ResetForm: function () {
        empr_AccountingMapping.CreateGrid([{ __KEY__: empr_AccountingMapping.GenerateKey(36), dC_TYPE: empr_AccountingMapping.DC_TYPE }]);
        $('.Record input').not('.dx-texteditor-input').val('');
        $('#ASTATUS').dxSelectBox('instance').option('value', 'Y');
        $('#BtnDelete').hide();
        $('#REMARKS').val('');
        empr_AccountingMapping.InitItemsDDL();
        //$('#ASTATUS').dxSelectBox('instance').option('value', 'Y');
        //$('#SalesMan').dxSelectBox('instance').option('placeholder', 'Select Salesman');
        //$('#SalesMan').dxSelectBox('instance').option('value', null);

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

    CreateGrid: function (dataSrc) {
        if (dataSrc.length > 0) {
            empr_AccountingMapping.rowsCount = dataSrc.length - 1;

        }
        var col = [
            {
                dataField: "Action",
                width: 100,
                alignment: 'center',
                fixed: true,
                fixedPosition: "left",
                allowExporting: false,
                allowEditing: false,
                cellTemplate: function (container, options) {
                    if (Permissions != "Admin") {
                        const copyAction = !Permissions.r_COPY
                            ? ''
                            : `<a href="javascript:;" class="grid-action-icon Clone" onclick="empr_AccountingMapping.CloneRow(${options.rowIndex})" title="Duplicate"><i class="fa fa-clone"></i></a>`;
                        const addAction = (!Permissions.r_ADD && !Permissions.r_EDIT)
                            ? ''
                            : `<a href="javascript:;" class="grid-action-icon Add" style="margin-left: 8px" onclick="empr_AccountingMapping.AddRow()" title="Add"><i class="fa fa-add"></i></a>`;
                        const deleteAction = !Permissions.r_DLT
                            ? ''
                            : `<a href="javascript:;" class="grid-action-icon Delete" style="margin-left: 8px" onclick="empr_AccountingMapping.DeleteRow(${options.rowIndex},${options.data.dT_CODE})" title="Delete"><i class="fa fa-trash"></i></a>`;
                        const actions = `<div class="btn-group btn-group-sm">${copyAction}${addAction}${deleteAction}</div>`;
                        $(actions).appendTo(container);
                    } else {
                        $(`<div class="btn-group btn-group-sm">
                      
                      <a href="javascript:;" class="grid-action-icon Add" style="margin-left: 8px" onclick="empr_AccountingMapping.AddRow()" title="Add"><i class="fa fa-add"></i></a>
                      <a href="javascript:;" class="grid-action-icon Delete" style="margin-left: 8px" onclick="empr_AccountingMapping.DeleteRow(${options.rowIndex},${options.data.dT_CODE})" title="Delete"><i class="fa fa-trash"></i></a>
                      </div>`).appendTo(container);
                    }

                    //<a href="javascript:;" class="grid-action-icon Clone" onclick="empr_AccountingMapping.CloneRow(`+ options.rowIndex + `)" title="Duplicate"><i class="fa fa-clone"></i></a>
                }
            },
            {
                dataField: 'dT_CODE',
                caption: 'dtcode',
                visible: false,
            },
            {
                dataField: 'acT_SETUP',
                caption: 'Account Setup',
                allowSorting: false,
                lookup: {
                    dataSource: ActSetup,
                    displayExpr: 'value',
                    valueExpr: 'key',
                },

            },
            {
                dataField: 'coa',
                caption: 'COA',
                allowSorting: false,
                lookup: {
                    dataSource: GLData,
                    displayExpr: 'value',
                    valueExpr: 'key',
                },

            },
        ];
        empr_helper.editableDxGridbindingForTransactionsVouchers('#DetailContainer', col, dataSrc, "CashReceiptVoucher", "custoM_ACT_CODE");
        if (dataSrc.length == 0) {
            $('#DetailContainer').dxDataGrid('instance').addRow().done(function () {
                $('#DetailContainer').dxDataGrid('instance').saveEditData();
            });
        }
    },

    CloneRow: function (index) {
        //debugger;
        const gridInstance = $('#DetailContainer').dxDataGrid('instance');
        let dataSource = gridInstance.option("dataSource") || [];

        if (dataSource.length >= Limit && Limit != 0) {
            empr_helper.notify("You can only add " + Limit + " records.", 2);
            return;
        }


        if (gridInstance.hasEditData()) {
            gridInstance.saveEditData().done(() => {
                cloneRowAtIndex(index);
            });
        } else {
            cloneRowAtIndex(index);
        }

        function cloneRowAtIndex(idx) {
            let dataSource = gridInstance.option("dataSource") || [];
            if (!dataSource[idx]) return;

            let clonedRowData = $.extend(true, {}, dataSource[idx]);

            if (clonedRowData.hasOwnProperty('dT_CODE')) delete clonedRowData.dT_CODE;

            clonedRowData.__KEY__ = empr_AccountingMapping.GenerateKey(36);

            let newDataSource = [clonedRowData, ...dataSource];
            gridInstance.option("dataSource", newDataSource);

            gridInstance.refresh();

            // Update row count if needed
            empr_AccountingMapping.rowsCount += 1;
        }
    },

    AddRow: function () {
        //debugger;
        const gridIns = $('#DetailContainer').dxDataGrid('instance');
        const dataSrc = gridIns.option("dataSource");

        if (dataSrc.length >= Limit && Limit != 0) {
            empr_helper.notify("You can only add  " + Limit + " records.", 2);
            return;
        }
        if ($('#DetailContainer').dxDataGrid('instance').hasEditData()) {
            $('#DetailContainer').dxDataGrid('instance').saveEditData().done(function () {
                empr_AccountingMapping.rowsCount += 1;
                const gridInstance = $('#DetailContainer').dxDataGrid('instance');
                const dataSource = gridInstance.option("dataSource");

                dataSource.unshift({ __KEY__: empr_AccountingMapping.GenerateKey(36), grouP_CODE: empr_AccountingMapping.grouP_CODE });
                gridInstance.option("dataSource", dataSource);
                gridInstance.refresh();
            });
        }
        else {
            empr_AccountingMapping.rowsCount += 1;
            const gridInstance = $('#DetailContainer').dxDataGrid('instance');
            const dataSource = gridInstance.option("dataSource");

            dataSource.unshift({ __KEY__: empr_AccountingMapping.GenerateKey(36), dC_TYPE: empr_AccountingMapping.DC_TYPE });
            gridInstance.option("dataSource", dataSource);
            gridInstance.refresh();
        }
    },
    //AddRow: function () {
    //    const gridIns = $('#DetailContainer').dxDataGrid('instance');
    //    const dataSrc = gridIns.option("dataSource");

    //    if (dataSrc.length >= Limit && Limit != 0) {
    //        empr_helper.notify("You can only add " + Limit + " records.", 2);
    //        return;
    //    }

    //    const gridInstance = $('#DetailContainer').dxDataGrid('instance');
    //    const dataSource = gridInstance.option("dataSource");


    //    const groupCode = $('#MasterContainer').dxForm('instance').option('formData').GROUP_CODE || 0;


    //    const newRow = {
    //        __KEY__: empr_AccountingMapping.GenerateKey(36),
    //        DT_CODE: empr_AccountingMapping.dT_CODE,  // backend can generate new DT_CODE if 0
    //        GROUP_CODE: grouP_CODE            // link to existing master
    //        // DC_TYPE removed
    //    };

    //    dataSource.unshift(newRow);
    //    gridInstance.option("dataSource", dataSource);
    //    gridInstance.refresh();
    //},


    DeleteRow: function (index, dtCode) {
        //debugger;
        const gridInstance = $('#DetailContainer').dxDataGrid('instance');
        var dataSource = gridInstance.option("dataSource");
        if (dataSource.length > 0) {
            if (dataSource.length > 1) {
                var row = dataSource[index];
                if (dtCode == '' || dtCode == null || dtCode == undefined) {
                    gridInstance.deleteRow(index);
                    empr_AccountingMapping.rowsCount -= 1;
                    gridInstance.saveEditData();
                }
                else {
                    var availableRows = dataSource.filter(x => x.dT_CODE > 0);
                    if (availableRows.length > 1) {
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
                            //debugger;
                            ajaxHelper.ajaxPostJsonData({ gcode: $('#Code').val(), code: dtCode }, "/AccountingMapping/DeleteAccountingMappingDetailByCode", function (data) {
                                empr_helper.notify(data.msg, data.msgType);
                                if (data.msgType == 1) {
                                    gridInstance.deleteRow(index);
                                    empr_AccountingMapping.rowsCount -= 1;
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
                var row = dataSource[index];
                if (dtCode != '' && dtCode != null && dtCode != undefined) {
                    var availableRows = dataSource.filter(x => x.dT_CODE > 0);
                    if (availableRows.length > 1) {
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
                            ajaxHelper.ajaxPostJsonData({ gcode: $('#Code').val(), code: dtCode }, "/CommMap/DeleteAccountingMappingDetailByCode", function (data) {
                                empr_helper.notify(data.msg, data.msgType);
                                if (data.msgType == 1) {
                                    empr_AccountingMapping.CreateGrid([{ __KEY__: empr_AccountingMapping.GenerateKey(36), dC_TYPE: empr_AccountingMapping.DC_TYPE }]);
                                }
                            }, false, true);
                        });
                    } else {
                        empr_helper.notify("You are not allowed to delete the last row.", 2);
                    }
                } else {
                    empr_AccountingMapping.CreateGrid([{ __KEY__: empr_AccountingMapping.GenerateKey(36), dC_TYPE: empr_AccountingMapping.DC_TYPE }]);
                    empr_helper.notify("You are not allowed to delete the last row.", 2);
                }
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


    //InitSalesManDDL: function (_selectedValue) {

    //    $.ajax({
    //        url: 'CommMap/GetSalesMan',
    //        method: 'GET',
    //        success: function (data) {

    //            console.log("salesman ", data);

    //            if (data.msgType == 1) {
    //                $('#SalesMan').dxSelectBox({
    //                    dataSource: data.data,
    //                    displayExpr: 'value',
    //                    valueExpr: 'key',
    //                    value: _selectedValue,
    //                    searchEnabled: true,
    //                    width: '100%',
    //                    placeholder: 'Search',
    //                    showClearButton: true,
    //                    dropDownOptions: {
    //                        height: 'auto'
    //                    },
    //                    pagingEnabled: true,
    //                    searchTimeout: 500,

    //                    onValueChanged: function (e) {
    //                        //debugger;

    //                        if (e.value != '' && e.value != null) {
    //                            var items = e.component._dataSource._items;
    //                            var item = items.find(i => i.key == e.value);

    //                            if (item) {
    //                                $('#Rate').val(item.rate);
    //                                $('#SACT_CODE').val(item.code);
    //                            }
    //                        } else {
    //                            $('#Rate').val('');
    //                            $('#SACT_CODE').val('');
    //                        }
    //                    }
    //                });
    //            } else {
    //                empr_helper.notify(data.data, data.msgType);
    //            }
    //        },
    //        error: function (error) {
    //            console.error('Error fetching data:', error);
    //        }
    //    });
    //},

    InitItemsDDL: function (selectedValue) {

        $('#ITEM_CODE').dxSelectBox({
            dataSource: Items,
            displayExpr: 'value',
            valueExpr: 'key',
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
        });
    },

    InitCopyItemsDDL: function (selectedValue) {

        $('#COPY_ITEM_CODE').dxSelectBox({
            dataSource: Items,
            displayExpr: 'value',
            valueExpr: 'key',
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
        });
    },

    InitSalesManDDL: function (_selectedValue, targetId) {
        $.ajax({
            url: 'CommMap/GetSalesMan',
            method: 'GET',
            success: function (data) {
                if (data.msgType == 1) {
                    $('#' + targetId).dxSelectBox({
                        dataSource: data.data,
                        displayExpr: 'value',
                        valueExpr: 'key',
                        value: _selectedValue,
                        searchEnabled: true,
                        width: '100%',
                        placeholder: 'Search',
                        showClearButton: true,
                        dropDownOptions: {
                            height: 'auto'
                        },
                        pagingEnabled: true,
                        searchTimeout: 500,
                        onValueChanged: function (e) {
                            if (e.value) {
                                var items = e.component._dataSource._items;
                                var item = items.find(i => i.key == e.value);
                                if (item) {
                                    $('#Rate').val(item.rate);
                                    $('#SACT_CODE').val(item.code);
                                }
                            } else {
                                $('#Rate').val('');
                                $('#SACT_CODE').val('');
                            }
                        }
                    });
                } else {
                    empr_helper.notify(data.data, data.msgType);
                }
            },
            error: function (error) {
            }
        });
    },



    InitQuickSearchGrid: function () {
        empr_AccountingMapping.GetAccountingMapping();
    },

    GetAccountingMapping: function () {
        ajaxHelper.ajaxGetJson('/AccountingMapping/GetAccountingMapping', function (data) {
            if (data.msgType == 1) {
                empr_AccountingMapping.CreateQuickSearchGrid(data.data);
            }
            else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },

    CreateQuickSearchGrid: function (dataSrc) {

        //debugger;
        var columns = [{
            dataField: "Action",
            width: 100,
            alignment: 'center',
            fixed: true,
            fixedPosition: "left",
            allowExporting: false,
            cellTemplate: function (container, options) {
                //<a href="javascript:;"  class="grid-action-icon elm_print" style="margin-left: 8px" reportid=${options.data.traN_ID} title="PRINT"><i class="fa fa-print"></i></a>

                if (Permissions != "Admin" && !Permissions.r_PRINT) {
                    $(`<div class="btn-group btn-group-sm">
                               <a href="javascript:;"  class="grid-action-icon elm_edit" reportid=${options.data.traN_ID} title="Edit"><i class="fa fa-edit"></i></a>
                               </div>`).appendTo(container);
                } else {
                    $(`<div class="btn-group btn-group-sm">
                               <a href="javascript:;"  class="grid-action-icon elm_edit" reportid=${options.data.traN_ID} title="Edit"><i class="fa fa-edit"></i></a>
                               <a href="javascript:;"  class="grid-action-icon elm_copy" style="margin-left: 8px" itemCode=${options.data.iteM_CODE} tranId=${options.data.traN_ID} title="COPY"><i class="fa fa-copy"></i></a>
                               </div>`).appendTo(container);
                }
            }
        },

        { dataField: 'traN_ID', caption: 'Code', alignment: 'center' },
        { dataField: 'astatus', caption: 'Status', alignment: 'center' },

        { dataField: 'iteM_NAME', caption: 'Item Name' },
        { dataField: 'remarks', caption: 'Remarks' },



        ];
        empr_helper.dxGridbindingVouchers('#gridContainer', columns, dataSrc, "CashReceiptVoucherQS", "single");
        setTimeout(function () {
            $('#gridContainer').dxDataGrid('instance').resize();
        }, 500);
        //empr_helper.dxGridbindingLazyLoading('#gridContainer', columns, "/CashReceiptVoucher/GetAccountingMapping", "dT_CODE", "CashReceiptVoucher", "multiple");
    },
    //ValidateAndPrepareDataForSave: function () {
    //    debugger;
    //    const gridInstance = $('#DetailContainer').dxDataGrid('instance');

    //    if (gridInstance.hasEditData()) {
    //        // Save any pending edits first
    //        gridInstance.saveEditData().done(function () {
    //            let detailRecords = gridInstance.option("dataSource");

    //            // Flatten grouped data
    //            if (Array.isArray(detailRecords) && detailRecords.some(item => item.key !== undefined)) {
    //                detailRecords = detailRecords.flatMap(group => group.items || []);
    //            }


    //            if (!Array.isArray(detailRecords) || detailRecords.length === 0) {
    //                empr_helper.notify("No detail records to save.", 2);
    //                return;
    //            }


    //            if (!$("#Code").val()) {
    //                detailRecords.reverse();
    //            }

    //            debugger;
    //            for (let obj of detailRecords) {
    //                obj.TRAN_ID = $("#Code").val();
    //                obj.ASTATUS = $('#ASTATUS').dxSelectBox('option', 'value');
    //                obj.ITEM_CODE = $('#ITEM_CODE').dxSelectBox('option', 'value');
    //                obj.REMARKS = $("#REMARKS").val();
    //                obj.COA = obj.coa || 0;
    //                obj.DT_CODE = obj.dT_CODE || 0;
    //                obj.ACT_SETUP = obj.acT_SETUP || 0;

    //                if (!obj.ITEM_CODE) {
    //                    empr_helper.notify("Item is required for all records.", 2);
    //                    return;
    //                }

    //            }

    //            // After validation, save
    //            empr_AccountingMapping.SaveInfo(detailRecords);
    //        });
    //    } else {
    //        // When no pending edits
    //        let detailRecords = gridInstance.option("dataSource");

    //        if (Array.isArray(detailRecords) && detailRecords.some(item => item.key !== undefined)) {
    //            detailRecords = detailRecords.flatMap(group => group.items || []);
    //        }

    //        if (!Array.isArray(detailRecords) || detailRecords.length === 0) {
    //            empr_helper.notify("No detail records to save.", 2);
    //            return;
    //        }

    //        if (!$("#Code").val()) {
    //            detailRecords.reverse();
    //        }

    //        for (let obj of detailRecords) {
    //            obj.TRAN_ID = $("#Code").val();
    //            obj.ASTATUS = $('#ASTATUS').dxSelectBox('option', 'value');
    //            obj.ITEM_CODE = $('#ITEM_CODE').dxSelectBox('option', 'value');
    //            obj.REMARKS = $("#REMARKS").val();
    //            obj.DT_CODE = obj.dT_CODE || 0;
    //            obj.COA = obj.coa || 0;
    //            obj.ACT_SETUP = obj.acT_SETUP || 0;

    //            if (!obj.ITEM_CODE) {
    //                empr_helper.notify("Item is required.", 2);
    //                return;
    //            }

    //        }

    //        empr_AccountingMapping.SaveInfo(detailRecords);
    //    }
    //},
    ValidateAndPrepareDataForSave: function () {
        debugger;
        const gridInstance = $('#DetailContainer').dxDataGrid('instance');

        if (gridInstance.hasEditData()) {
            // Save any pending edits first
            gridInstance.saveEditData().done(function () {
                let detailRecords = gridInstance.option("dataSource");

                // Flatten grouped data
                if (Array.isArray(detailRecords) && detailRecords.some(item => item.key !== undefined)) {
                    detailRecords = detailRecords.flatMap(group => group.items || []);
                }

                if (!Array.isArray(detailRecords) || detailRecords.length === 0) {
                    empr_helper.notify("No detail records to save.", 2);
                    return;
                }

                if (!$("#Code").val()) {
                    detailRecords.reverse();
                }

                // --- Naya Loop: Duplicate acT_SETUP Validation (Block 1) ---
                let seenActSetups1 = new Set();
                for (let checkObj of detailRecords) {
                    let currentActSetup = checkObj.acT_SETUP || 0;
                    if (seenActSetups1.has(currentActSetup)) {
                        empr_helper.notify("Accout Setup should be different in each record.", 2);
                        return; // Yahin se baahir nikal jayega
                    }
                    seenActSetups1.add(currentActSetup);
                }

                debugger;
                for (let obj of detailRecords) {
                    obj.TRAN_ID = $("#Code").val();
                    obj.ASTATUS = $('#ASTATUS').dxSelectBox('option', 'value');
                    obj.ITEM_CODE = $('#ITEM_CODE').dxSelectBox('option', 'value');
                    obj.REMARKS = $("#REMARKS").val();
                    obj.COA = obj.coa || 0;
                    obj.DT_CODE = obj.dT_CODE || 0;
                    obj.ACT_SETUP = obj.acT_SETUP || 0;

                    if (!obj.ITEM_CODE) {
                        empr_helper.notify("Item is required for all records.", 2);
                        return;
                    }

                    if (!obj.ACT_SETUP) {
                        empr_helper.notify("Account Setup is required.", 2);
                        return;
                    }
                    if (!obj.COA) {
                        empr_helper.notify("COA is required.", 2);
                        return;
                    }
                }

                // After validation, save
                empr_AccountingMapping.SaveInfo(detailRecords);
            });
        } else {
            // When no pending edits
            let detailRecords = gridInstance.option("dataSource");

            if (Array.isArray(detailRecords) && detailRecords.some(item => item.key !== undefined)) {
                detailRecords = detailRecords.flatMap(group => group.items || []);
            }

            if (!Array.isArray(detailRecords) || detailRecords.length === 0) {
                empr_helper.notify("No detail records to save.", 2);
                return;
            }

            if (!$("#Code").val()) {
                detailRecords.reverse();
            }

            // --- Naya Loop: Duplicate acT_SETUP Validation (Block 2) ---
            let seenActSetups2 = new Set();
            for (let checkObj of detailRecords) {
                let currentActSetup = checkObj.acT_SETUP || 0;
                if (seenActSetups2.has(currentActSetup)) {
                    empr_helper.notify("Accout Setup should be different in each record.", 2);
                    return; // Yahin se baahir nikal jayega
                }
                seenActSetups2.add(currentActSetup);
            }

            for (let obj of detailRecords) {
                obj.TRAN_ID = $("#Code").val();
                obj.ASTATUS = $('#ASTATUS').dxSelectBox('option', 'value');
                obj.ITEM_CODE = $('#ITEM_CODE').dxSelectBox('option', 'value');
                obj.REMARKS = $("#REMARKS").val();
                obj.DT_CODE = obj.dT_CODE || 0;
                obj.COA = obj.coa || 0;
                obj.ACT_SETUP = obj.acT_SETUP || 0;

                if (!obj.ITEM_CODE) {
                    empr_helper.notify("Item is required.", 2);
                    return;
                }

                if (!obj.ACT_SETUP) {
                    empr_helper.notify("Account Setup is required.", 2);
                    return;
                }
                if (!obj.COA) {
                    empr_helper.notify("COA is required.", 2);
                    return;
                }


            }

            empr_AccountingMapping.SaveInfo(detailRecords);
        }
    },

    SaveInfo: function (detailRecords) {
        debugger;
        console.log('Safe Attempt', detailRecords)
        ajaxHelper.ajaxPostJsonData({ modelRecord: detailRecords }, "/AccountingMapping/Save", function (data) {
            empr_helper.notify(data.msg, data.msgType);
            if (data.msgError != null) {
                empr_helper.notify(data.msgError, 2);
            }
            if (data.msgType == 1) {
                debugger;
                if ($("#Code").val() == 0
                    || $("#Code").val() == null
                    || $("#Code").val() == undefined
                    || $("#Code").val() == "") {
                    $('#Code').val(data.data.code);
                    empr_helper.selectedBill = data.data.code;
                    empr_AccountingMapping.ResetForm();
                    //    $('#VOUCHER_NO').val(data.data.voucherNo);
                }
                if (dataClear == 1) {
                    empr_AccountingMapping.GetAccountingMappingByCode($('#Code').val());
                    if (Permissions != "Admin") {
                        if (Permissions.r_DLT) {
                            $('#BtnDelete').show();
                        }
                    } else {
                        $('#BtnDelete').show();
                    }
                }
                else {
                    empr_AccountingMapping.ResetForm();
                    //$('#PARTY_CODE').dxSelectBox('instance').option('value', null);


                }


            }
        }, false, true);
    },

    GetAccountingMappingByCode: function (code) {
        $("#Loader").show();
        $("#Loader").css('display', 'flex');
        ajaxHelper.ajaxGetJson('/AccountingMapping/GetAccountingMappingByCode?code=' + code, function (data) {
            //debugger;
            if (data.master.msgType == 1) {
                var masterData = data.master.data;
                if (masterData.length == 1) {
                    var response = masterData[0];
                    $('#Code').val(response.traN_ID);
                    //$('#ACT_CODE').val(response.acT_CODE);
                    $('#REMARKS').val(response.remarks);
                    empr_AccountingMapping.InitItemsDDL(response.iteM_CODE);
                    //$('#V_DATE').val(response.v_DATE);

                    //$('#PARTY_CODE').dxSelectBox('instance').option('value', response.party);

                    $('#ASTATUS').dxSelectBox('instance').option('value', response.astatus);



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
                if (data.detail.msgType == 1) {
                    empr_AccountingMapping.CreateGrid(data.detail.data);
                    $('.card-body').addClass('customHighlightForModifiedCells');
                    $("#Loader").hide();
                }
                else {
                    empr_helper.notify("2" + data.msg, data.msgType);
                    $("#Loader").hide();
                }
            }
            else {
                empr_helper.notify("1" + data.msg, data.msgType);
                $("#Loader").hide();
            }
        }, false, true);
    },

    GetAccountingMappingDetailsByCode: function (code) {
        debugger;
        ajaxHelper.ajaxGetJson('/AccountingMapping/GetAccountingMappingDetailsByCode?code=' + code, function (data) {
            //console.log(data)
            if (data.msgType == 1) {
                empr_AccountingMapping.CreateGrid(data.data);
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
            ajaxHelper.ajaxPostJsonData({ code: $('#Code').val() }, "/AccountingMapping/Delete", function (data) {
                empr_helper.notify(data.msg, data.msgType);
                if (data.msgType == 1) {
                    empr_AccountingMapping.ResetForm();
                    $('#BtnDelete').hide();
                }
            }, false, true);
        });
    },


}
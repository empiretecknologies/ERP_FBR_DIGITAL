var empr_LotRegistration = {
    lotNo: 0,
    initEvents: function () {
        $(document).ready(function () {
            empr_LotRegistration.InitQuickSearch();
            empr_LotRegistration.InitWarehouse();
            empr_LotRegistration.InitItems();
            empr_LotRegistration.InitCostCenterDDL();
            console.log("warhouse drop value ", Warehouse);

            $('#BtnSave').click(function () {
                if (Permissions != "Admin") {
                    if (!$("#Code").val() && !Permissions.r_ADD) {
                        empr_helper.notify("You are not allowed to add new record !", 2);
                    }
                    else if (($("#Code").val() > 0) && !Permissions.r_EDIT) {
                        empr_helper.notify("You are not allowed to edit records !", 2);
                    } else {
                        if (empr_LotRegistration.validateForm()) {
                            empr_LotRegistration.saveAttempt();
                        }
                    }
                } else {
                    if (empr_LotRegistration.validateForm()) {
                        empr_LotRegistration.saveAttempt();
                    }
                }
            });

            $('body').on('click', '#QuickSearch', function () {
                empr_LotRegistration.InitQuickSearch();
            });

            $('body').on('click', '.elm_edit', function () {
                var reportid = $(this).attr("reportid");
                empr_LotRegistration.GetLotRegistrationByID(reportid);
            });

            $('body').on('click', '.elm_copy', function () {
                var id = $(this).attr("reportid");
                var date = $(this).attr("reportdate");
                console.log(date);
                var lotNo = $(this).attr("reportlotno");
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
                    $('#updatedDated').val(date);
                    $('#updatedLotNo').val(lotNo);
                    empr_helper.selectedBill = id;
                    $('#CopyViewModal_LR').modal('show');
                });
            });

            $('body').on('click', '#saveCopiedRecord', function () {
                ajaxHelper.ajaxPostJsonData({ traN_ID: empr_helper.selectedBill, v_DATE: $('#updatedDated').val(), item_NAME: $('#updatedLotNo').val() }, "/LotRegistration/CopyRecord", function (data) {
                    console.log(data.data);
                    empr_helper.notify(data.msg, data.msgType);
                    if (data.msgType == 1) {
                        $('.modal').modal('hide');
                        empr_LotRegistration.GetLotRegistrationByID(data.data);
                    }
                }, false, true);
            });

            $('body').on('click', '#BtnNew', function () {
                $('#BtnDelete').hide();
                $('#BtnNew').hide();
                empr_LotRegistration.resetForm();
            });

            $('#BtnDelete').click(function () {
                empr_LotRegistration.DeleteRecord();
            });

            if (Permissions != "Admin") {
                !Permissions.r_VIEW && $('#gridContainer').hide();
                (!Permissions.r_ADD && !Permissions.r_EDIT) && $('#BtnSave').hide();
            }
        });
    },
    DeleteRecord: function () {

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
            ajaxHelper.ajaxPostJsonData({ id: $('#Code').val() }, "/LotRegistration/Delete", function (data) {
                empr_helper.notify(data.msg, data.msgType);
                if (data.msgType == 1) {
                    empr_LotRegistration.resetForm();
                    empr_LotRegistration.InitQuickSearch();
                    $('#optmodal').modal('hide');
                    $('#BtnDelete').hide();
                    $('#BtnNew').hide();
                }
            }, false, true);
        });

    },
    resetForm: function () {
        $("#GROUP_NAME").val('');
        $("#Image").val('');
        $("#GROUP_PIC").val(''); 
        empr_LotRegistration.InitWarehouse();
        empr_LotRegistration.InitItems();
        empr_LotRegistration.InitCostCenterDDL();
        $('#ASTATUS').dxSelectBox('instance').option('value', "Y");
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
    validateForm: function () {

        var valid = true;
        var GROUP_NAME = $("#GROUP_NAME").val().trim();

        if (GROUP_NAME == '') {
            valid = false;
            empr_helper.notify("Please enter name.", 2);
        }

        return valid;
    },
    GetDataToSave: function () {
        var ID = $("#Code").val();
        var GROUP_NAME = $("#GROUP_NAME").val();
        var GROUP_PIC = $("#GROUP_PIC").val();
        var ASTATUS = $("#ASTATUS").dxSelectBox('instance').option('value');
        var WAREHOUSE = $("#WAREHOUSE").dxSelectBox('instance').option('value');
        var CC_ID = $("#COST_CENTER_ID").dxSelectBox('instance').option('value');
        var ITEM = $("#ITEM").dxSelectBox('instance').option('value');
        var modelRecord = {
            GROUP_CODE: ID,
            GROUP_NAME: GROUP_NAME,
            ASTATUS: ASTATUS,
            ITEM: ITEM,
            GPIC: GROUP_PIC,
            CC_ID: CC_ID,
            WAREHOUSE: WAREHOUSE
        }
        return modelRecord;
    },
    saveAttempt: function () {

        var obj = empr_LotRegistration.GetDataToSave();
        debugger
        ajaxHelper.ajaxPostJsonData(obj, "/LotRegistration/save", function (data) {
            empr_helper.notify(data.msg, data.msgType);
            if (data.msgType == 1) {
                empr_LotRegistration.resetForm();
                empr_LotRegistration.InitQuickSearch();
                $('#optmodal').modal('hide');
                $('#BtnDelete').hide();
                $('#BtnNew').hide();
            }
        }, false, true);
    },
    SaveImage: function () {
        debugger;
        $('#BtnSave').prop('disabled', true);
        //var files = document.getElementById('Image').files;
        //var formData = new FormData();
        //for (var i = 0; i !== files.length; i++) {
        //    formData.append("model", files[i]);
        //}
        var base64String = $('#item-img-output').attr('src').replace('data:image/png;base64,', '');
        var binaryData = atob(base64String);
        var blob = new Blob([new Uint8Array(Array.prototype.map.call(binaryData, function (char) {
            return char.charCodeAt(0);
        }))], { type: 'image/png' });

        var formData = new FormData();
        formData.append('model', blob);
        $.ajax({
            url: "/LotRegistration/SaveImage",
            data: formData,
            processData: false,
            contentType: false,
            type: "POST",
            success: function (data) {
                debugger;
                if (data.msgType == '1') {
                    $("#GROUP_PIC").val(data.data);
                }
                else {
                    console.log(data);
                    empr_helper.notify("Something went wrong while saving the file. please re-upload the file.", data.msgType);
                }
                $('#BtnSave').prop('disabled', false);
            }
        }
        );
    },
    InitQuickSearch: function () {
        empr_LotRegistration.GetAllLotRegistrations();
    },
    GetAllLotRegistrations: function () {
        ajaxHelper.ajaxGetJson('/LotRegistration/QuickSearch', function (data) {
            empr_LotRegistration.CreateGrid(data.data);
            var allLots = data.data;
            if (allLots.length > 0) {
                if (!isNaN(parseInt(allLots[0].loT_NO))) {
                    empr_LotRegistration.lotNo = parseInt(allLots[0].loT_NO) + 1;
                } else {
                    empr_LotRegistration.lotNo = allLots[0].loT_NO;
                }
            } else {
                empr_LotRegistration.lotNo = 0;
            }
            $("#LOT_NO").val(empr_LotRegistration.lotNo);
        }, false, true);
    },
    GetLotRegistrationByID: function (id) {
        debugger;
        ajaxHelper.ajaxGetJson('/LotRegistration/GetLotRegistrationByID?id=' + id, function (data) {
            empr_LotRegistration.resetForm();
            debugger;

            if (data.msgType == 1) {

                var record = data.data;
                $("#Code").val(record.grouP_CODE);
                $('#WAREHOUSE').dxSelectBox('instance').option('value', record.warehouse);
                $('#ITEM').dxSelectBox('instance').option('value', record.iteM_CODE);
                $('#COST_CENTER_ID').dxSelectBox('instance').option('value', record.cC_ID);
                $('#ASTATUS').dxSelectBox('instance').option('value', record.astatus);
                $("#GROUP_NAME").val(record.grouP_NAME);
                $("#GROUP_PIC").val(record.gpic);
                $('.modal').modal('hide');
                if (Permissions != "Admin") {
                    if (Permissions.r_DLT) {
                        $('#BtnDelete').show();
                    }
                    if (Permissions.r_ADD) {
                        $('#BtnNew').show();
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
                    $('#BtnNew').show();
                }
            } else {
                empr_helper.notify(data.msg, data.msgType);
            }
        }, false, true);
    },
    CreateGrid: function (dataSrc) {

        var col = [{
            dataField: "Action",
            width: 100,
            alignment: 'center',
            fixed: true,
            fixedPosition: "left",
            allowExporting: false,
            cellTemplate: function (container, options) {
                debugger
                var html = '<div class="btn-group btn-group-sm">';
                if (options.data.gpic != null && options.data.gpic != '' && options.data.gpic != undefined) {
                    html += `<a href="javascript:;" class="grid-action-icon" title="View Pic" onclick="ShowImage('${options.data.gpic}')"><i class="fa fa-eye"></i></a>`;
                }
                html += `<a href="javascript:;" class="grid-action-icon elm_edit" style="padding-left: 6px;" reportid=${options.data.grouP_CODE} title="Edit"><i class="fa fa-edit"></i></a>`;
                html += '</div>';
                $(html).appendTo(container);
            }
        },
        {
            dataField: 'gpic',
            caption: 'Image',
            width: 120,
            visible: false,
            cellTemplate(container, options) {
                if (options.value != null && options.value != '' && options.value != undefined) {
                    $('<div>')
                        .append($('<img>', { src: options.value, height: '100px', width: '100px' }))
                        .appendTo(container);
                }
            },
            },
            { dataField: 'grouP_CODE', caption: 'Code', width: 90, alignment: 'center' },
        { dataField: 'grouP_NAME', caption: 'Lot Name' },
        { dataField: 'item', caption: 'Item' },
        { dataField: 'warehouse', caption: 'Warehouse' },
        { dataField: 'cC_NAME', caption: 'Cost Center' },
        { dataField: 'astatus', caption: 'Active' },
        ];
        empr_helper.dxGridbindingVouchers('#gridContainer', col, dataSrc, "SetupSubType", 'single');
    },
    InitWarehouse: function (selectedValue) {

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

    InitItems: function (selectedValue) {

        $('#ITEM').dxSelectBox({
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
        });
    },

    InitCostCenterDDL: function (selectedValue) {

        $('#COST_CENTER_ID').dxSelectBox({
            dataSource: CostCenter,
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

}
var empr_PurchaseBill_InvoiceBulkUpload = {
    excelRows: [],
    partyRows: [],
    itemRows: [],
    currentStep: 1,
    partiesReady: false,
    itemsReady: false,

    columns: [
        "V_Date", "Party Name", "Party Address", "Party Region", "Party Reg.Type", "Party NTN/CNIC", "Party ST.Reg",
        "Item Name", "Item Unit", "HS Code",
        "Invoice Qty", "Rate", "Amount", "W.Rate", "W.Amt", "Tax", "Tax Amt", "Net Amount",
        "Sale Type", "Item SNO", "Schedule No", "Serial SNO"
    ],

    OpenFilePicker: function () {
        var input = document.getElementById("InvoiceBulkFileInput");
        if (input) {
            input.value = "";
            input.click();
        }
    },

    InitEvents: function () {
        $("#BtnInvoiceBulkUpload").off("click").on("click", function () {
            empr_PurchaseBill_InvoiceBulkUpload.OpenModal();
        });

        $("#BtnDownloadInvoiceBulkTemplate").off("click").on("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            empr_PurchaseBill_InvoiceBulkUpload.DownloadTemplate();
        });

        $("#BtnInvoiceBulkSelectFile").off("click").on("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            empr_PurchaseBill_InvoiceBulkUpload.OpenFilePicker();
        });

        $("#ibuDropZone").off("click").on("click", function (e) {
            if ($(e.target).closest("#BtnInvoiceBulkSelectFile").length) {
                return;
            }
            e.preventDefault();
            e.stopPropagation();
            empr_PurchaseBill_InvoiceBulkUpload.OpenFilePicker();
        });

        $("#ibuDropZone").off("dragover").on("dragover", function (e) {
            e.preventDefault();
            e.stopPropagation();
        });

        $("#ibuDropZone").off("drop").on("drop", function (e) {
            e.preventDefault();
            e.stopPropagation();
            var files = e.originalEvent.dataTransfer.files;
            if (files && files.length > 0) {
                empr_PurchaseBill_InvoiceBulkUpload.HandleFile(files[0]);
            }
        });

        $("#InvoiceBulkFileInput").off("click").on("click", function (e) {
            e.stopPropagation();
        });

        $("#InvoiceBulkFileInput").off("change").on("change", function () {
            var file = this.files && this.files[0];
            if (file) {
                empr_PurchaseBill_InvoiceBulkUpload.HandleFile(file);
            }
        });

        $("#BtnInvoiceBulkAddParties").off("click").on("click", function () {
            empr_PurchaseBill_InvoiceBulkUpload.AddParties();
        });

        $("#BtnInvoiceBulkAddItems").off("click").on("click", function () {
            empr_PurchaseBill_InvoiceBulkUpload.AddItems();
        });

        $("#BtnInvoiceBulkUploadInvoices").off("click").on("click", function () {
            empr_PurchaseBill_InvoiceBulkUpload.UploadInvoices();
        });
    },

    OpenModal: function () {
        empr_PurchaseBill_InvoiceBulkUpload.ResetState();
        empr_PurchaseBill_InvoiceBulkUpload.ShowStep(1);
        $("#InvoiceBulkUploadModal").modal("show");
    },

    ResetState: function () {
        empr_PurchaseBill_InvoiceBulkUpload.excelRows = [];
        empr_PurchaseBill_InvoiceBulkUpload.partyRows = [];
        empr_PurchaseBill_InvoiceBulkUpload.itemRows = [];
        empr_PurchaseBill_InvoiceBulkUpload.partiesReady = false;
        empr_PurchaseBill_InvoiceBulkUpload.itemsReady = false;
        $("#InvoiceBulkFileInput").val("");
        $("#ibuSelectedFileName").text("");
        $("#ibuPartyGrid").empty();
        $("#ibuItemGrid").empty();
        $("#ibuInvoiceGrid").empty();
        $("#ibuInvoiceSummary").text("");
        $("#ibuPartyErrorBox").addClass("d-none").empty();
        $("#ibuItemErrorBox").addClass("d-none").empty();
    },

    CancelProcess: function (message, keepStep, keepPartyErrors, keepItemErrors) {
        empr_helper.notify(message || "Bulk upload process cancelled.", 2);
        var partyErrorHtml = keepPartyErrors ? $("#ibuPartyErrorBox").html() : "";
        var itemErrorHtml = keepItemErrors ? $("#ibuItemErrorBox").html() : "";
        var showPartyError = keepPartyErrors && !!partyErrorHtml;
        var showItemError = keepItemErrors && !!itemErrorHtml;

        empr_PurchaseBill_InvoiceBulkUpload.excelRows = [];
        empr_PurchaseBill_InvoiceBulkUpload.partyRows = [];
        empr_PurchaseBill_InvoiceBulkUpload.itemRows = [];
        empr_PurchaseBill_InvoiceBulkUpload.partiesReady = false;
        empr_PurchaseBill_InvoiceBulkUpload.itemsReady = false;
        $("#InvoiceBulkFileInput").val("");
        $("#ibuSelectedFileName").text("");
        $("#ibuPartyGrid").empty();
        $("#ibuItemGrid").empty();
        $("#ibuInvoiceGrid").empty();
        $("#ibuInvoiceSummary").text("");

        if (showPartyError) {
            $("#ibuPartyErrorBox").removeClass("d-none").html(partyErrorHtml);
        } else {
            $("#ibuPartyErrorBox").addClass("d-none").empty();
        }
        if (showItemError) {
            $("#ibuItemErrorBox").removeClass("d-none").html(itemErrorHtml);
        } else {
            $("#ibuItemErrorBox").addClass("d-none").empty();
        }

        empr_PurchaseBill_InvoiceBulkUpload.ShowStep(keepStep || 1);
    },

    GetPartyEmptyFields: function (party) {
        var empty = [];
        if (!party.partyName) empty.push("Party Name");
        if (!party.partyAddress) empty.push("Party Address");
        if (!party.partyRegion) empty.push("Party Region");
        if (!party.partyRegType) empty.push("Party Reg.Type");
        if (!party.partyNtn) empty.push("Party NTN/CNIC");
        if (!party.partyStReg) empty.push("Party ST.Reg");
        return empty;
    },

    GetItemEmptyFields: function (item) {
        var empty = [];
        if (!item.itemName) empty.push("Item Name");
        if (!item.itemUnit) empty.push("Item Unit");
        if (!item.hsCode) empty.push("HS Code");
        return empty;
    },

    ShowPartyErrors: function (errors) {
        if (!errors || !errors.length) {
            $("#ibuPartyErrorBox").addClass("d-none").empty();
            return;
        }
        var html = "<strong>" + errors.length + " party error(s):</strong><ul>";
        errors.forEach(function (e) {
            html += "<li><strong>" + (e.name || "Unknown Party") + "</strong> — empty: " + e.fields.join(", ") + "</li>";
        });
        html += "</ul>";
        $("#ibuPartyErrorBox").removeClass("d-none").html(html);
    },

    ShowItemErrors: function (errors) {
        if (!errors || !errors.length) {
            $("#ibuItemErrorBox").addClass("d-none").empty();
            return;
        }
        var html = "<strong>" + errors.length + " item error(s):</strong><ul>";
        errors.forEach(function (e) {
            html += "<li><strong>" + (e.name || "Unknown Item") + "</strong> — empty: " + e.fields.join(", ") + "</li>";
        });
        html += "</ul>";
        $("#ibuItemErrorBox").removeClass("d-none").html(html);
    },

    ValidatePartiesBeforeCheck: function () {
        var parties = empr_PurchaseBill_InvoiceBulkUpload.GetUniqueParties(true);
        var valid = [];
        var errors = [];
        parties.forEach(function (p) {
            var empty = empr_PurchaseBill_InvoiceBulkUpload.GetPartyEmptyFields(p);
            if (empty.length) {
                errors.push({ name: p.partyName || p.partyNtn || "N/A", fields: empty });
            } else {
                valid.push(p);
            }
        });
        return { valid: valid, errors: errors };
    },

    ValidateItemsBeforeCheck: function () {
        var items = empr_PurchaseBill_InvoiceBulkUpload.GetUniqueItems(true);
        var valid = [];
        var errors = [];
        items.forEach(function (it) {
            var empty = empr_PurchaseBill_InvoiceBulkUpload.GetItemEmptyFields(it);
            if (empty.length) {
                errors.push({ name: it.itemName || it.hsCode || "N/A", fields: empty });
            } else {
                valid.push(it);
            }
        });
        return { valid: valid, errors: errors };
    },

    ShowStep: function (step) {
        empr_PurchaseBill_InvoiceBulkUpload.currentStep = step;
        $("#ibuStep1, #ibuStep2, #ibuStep3, #ibuStep4").addClass("d-none");
        $("#ibuStep" + step).removeClass("d-none");

        $("#invoiceBulkSteps .ibu-step").removeClass("active done");
        $("#invoiceBulkSteps .ibu-step").each(function () {
            var s = parseInt($(this).attr("data-step"), 10);
            if (s < step) {
                $(this).addClass("done");
            } else if (s === step) {
                $(this).addClass("active");
            }
        });
    },

    DownloadTemplate: function () {
        if (typeof ExcelJS === "undefined") {
            empr_helper.notify("ExcelJS not loaded.", 2);
            return;
        }

        var workbook = new ExcelJS.Workbook();
        var sheet = workbook.addWorksheet("InvoiceBulk");
        var saleTypeSheet = workbook.addWorksheet("SaleTypes");

        saleTypeSheet.addRow(["Sale Type", "Item SNO", "Schedule No", "Serial SNO", "Code"]);
        var saleTypeCount = 0;
        if (typeof fbrType !== "undefined" && fbrType && fbrType.length) {
            fbrType.forEach(function (x) {
                var name = x.value || "";
                var sno = x.item_Sno || "";
                // Unique display so duplicate S_NAME rows are selectable (SN001 vs SN002)
                var display = sno ? (name + " | " + sno) : name;
                saleTypeSheet.addRow([
                    display,
                    sno,
                    x.sche_No || "",
                    x.seri_No || "",
                    x.key || ""
                ]);
                saleTypeCount++;
            });
        }
        if (saleTypeCount === 0) {
            saleTypeSheet.addRow(["", "", "", "", ""]);
            saleTypeCount = 1;
        }
        saleTypeSheet.getRow(1).font = { bold: true };
        saleTypeSheet.columns = [
            { width: 48 }, { width: 14 }, { width: 14 }, { width: 14 }, { width: 10 }
        ];

        var colWidths = [
            14, 22, 24, 14, 14, 16, 14,
            22, 12, 12,
            12, 12, 12, 10, 12, 10, 12, 12,
            48, 12, 12, 12
        ];
        sheet.columns = empr_PurchaseBill_InvoiceBulkUpload.columns.map(function (header, i) {
            return { header: header, width: colWidths[i] || 12 };
        });
        var headerRow = sheet.getRow(1);
        empr_PurchaseBill_InvoiceBulkUpload.columns.forEach(function (header, i) {
            headerRow.getCell(i + 1).value = header;
        });
        headerRow.font = { bold: true };
        headerRow.alignment = { vertical: "middle", horizontal: "center", wrapText: true };
        headerRow.commit();

        var templateRows = 100;
        var listEnd = saleTypeCount + 1;
        for (var r = 2; r <= templateRows + 1; r++) {
            sheet.getCell("A" + r).numFmt = "yyyy-mm-dd";
            sheet.getCell("M" + r).value = { formula: "IF(OR(K" + r + "=\"\",L" + r + "=\"\"),\"\",K" + r + "*L" + r + ")" };
            sheet.getCell("O" + r).value = { formula: "IF(M" + r + "=\"\",\"\",M" + r + "*N" + r + "/100)" };
            sheet.getCell("Q" + r).value = { formula: "IF(M" + r + "=\"\",\"\",M" + r + "*P" + r + "/100)" };
            sheet.getCell("R" + r).value = { formula: "IF(M" + r + "=\"\",\"\",M" + r + "+O" + r + "+Q" + r + ")" };
            sheet.getCell("T" + r).value = { formula: "IF(S" + r + "=\"\",\"\",IFERROR(VLOOKUP(S" + r + ",SaleTypes!$A$2:$D$" + listEnd + ",2,FALSE),\"\"))" };
            sheet.getCell("U" + r).value = { formula: "IF(S" + r + "=\"\",\"\",IFERROR(VLOOKUP(S" + r + ",SaleTypes!$A$2:$D$" + listEnd + ",3,FALSE),\"\"))" };
            sheet.getCell("V" + r).value = { formula: "IF(S" + r + "=\"\",\"\",IFERROR(VLOOKUP(S" + r + ",SaleTypes!$A$2:$D$" + listEnd + ",4,FALSE),\"\"))" };

            ["M", "O", "Q", "R", "T", "U", "V"].forEach(function (col) {
                sheet.getCell(col + r).fill = {
                    type: "pattern",
                    pattern: "solid",
                    fgColor: { argb: "FFF3F6F9" }
                };
            });

            sheet.getCell("S" + r).dataValidation = {
                type: "list",
                allowBlank: true,
                formulae: ["SaleTypes!$A$2:$A$" + listEnd],
                showErrorMessage: true,
                errorTitle: "Invalid Sale Type",
                error: "Please select a Sale Type from the list"
            };
        }

        workbook.xlsx.writeBuffer().then(function (buffer) {
            var blob = new Blob([buffer], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
            var link = document.createElement("a");
            link.href = URL.createObjectURL(blob);
            link.download = "Invoice_Bulk_Upload_Template_VDate.xlsx";
            link.click();
            URL.revokeObjectURL(link.href);
        });
    },

    HandleFile: function (file) {
        if (!file) return;
        var name = (file.name || "").toLowerCase();
        if (!(name.endsWith(".xlsx") || name.endsWith(".xls"))) {
            empr_helper.notify("Please select an Excel file (.xlsx).", 2);
            return;
        }

        $("#ibuSelectedFileName").text(file.name);
        var reader = new FileReader();
        reader.onload = function (e) {
            var data = new Uint8Array(e.target.result);
            empr_PurchaseBill_InvoiceBulkUpload.ParseExcel(data);
        };
        reader.readAsArrayBuffer(file);
    },

    ParseExcel: function (data) {
        if (typeof ExcelJS === "undefined") {
            empr_helper.notify("ExcelJS not loaded.", 2);
            return;
        }

        var workbook = new ExcelJS.Workbook();
        workbook.xlsx.load(data).then(function () {
            var sheet = workbook.worksheets[0];
            if (!sheet) {
                empr_helper.notify("Excel sheet not found.", 2);
                return;
            }

            var rows = [];
            var readCell = function (rowNumber, colNumber) {
                var cell = sheet.getCell(rowNumber, colNumber);
                if (!cell || cell.value == null || cell.value === "") return "";
                var v = cell.value;
                if (Object.prototype.toString.call(v) === "[object Date]") return v;
                if (typeof v === "object") {
                    if (v.result != null && v.result !== "") return v.result;
                    if (v.text != null) return v.text;
                    if (v.richText && v.richText.length) {
                        return v.richText.map(function (x) { return x.text || ""; }).join("");
                    }
                    if (v.formula && cell.result != null) return cell.result;
                    return "";
                }
                return v;
            };
            var toText = function (v) {
                if (v == null || v === "") return "";
                return String(v).trim();
            };
            var toNum = function (v) {
                if (v == null || v === "") return NaN;
                var n = parseFloat(v);
                return isNaN(n) ? NaN : n;
            };
            var pad2 = function (n) {
                return String(n).padStart(2, "0");
            };
            var formatYmd = function (d) {
                return d.getFullYear() + "-" + pad2(d.getMonth() + 1) + "-" + pad2(d.getDate());
            };
            var toDate = function (v) {
                if (v == null || v === "") return "";
                if (Object.prototype.toString.call(v) === "[object Date]" && !isNaN(v.getTime())) {
                    if (v.getUTCHours() === 0 && v.getHours() !== 0) {
                        return v.getUTCFullYear() + "-" + pad2(v.getUTCMonth() + 1) + "-" + pad2(v.getUTCDate());
                    }
                    return formatYmd(v);
                }
                if (typeof v === "number" && isFinite(v)) {
                    var d = new Date(1899, 11, 30);
                    d.setDate(d.getDate() + Math.floor(v));
                    if (!isNaN(d.getTime())) return formatYmd(d);
                }
                var s = String(v).trim();
                if (!s) return "";
                if (/^\d{4}-\d{2}-\d{2}/.test(s)) return s.substring(0, 10);
                var dmY = s.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})$/);
                if (dmY) {
                    var parsed = new Date(parseInt(dmY[3], 10), parseInt(dmY[2], 10) - 1, parseInt(dmY[1], 10));
                    if (!isNaN(parsed.getTime())) return formatYmd(parsed);
                }
                var parsedDate = new Date(s);
                if (!isNaN(parsedDate.getTime())) return formatYmd(parsedDate);
                return "";
            };

            sheet.eachRow(function (row, rowNumber) {
                if (rowNumber === 1) return;

                var vDate = toDate(readCell(rowNumber, 1));
                var partyName = toText(readCell(rowNumber, 2));
                var itemName = toText(readCell(rowNumber, 8));
                if (!partyName && !itemName) return;

                var qty = toNum(readCell(rowNumber, 11)) || 0;
                var rate = toNum(readCell(rowNumber, 12)) || 0;
                var wRate = toNum(readCell(rowNumber, 14)) || 0;
                var tax = toNum(readCell(rowNumber, 16)) || 0;

                var amount = Math.round((qty * rate) * 100) / 100;
                var wAmt = Math.round((amount * wRate / 100) * 100) / 100;
                var taxAmt = Math.round((amount * tax / 100) * 100) / 100;
                var netAmount = Math.round((amount + wAmt + taxAmt) * 100) / 100;

                var saleType = toText(readCell(rowNumber, 19));
                var itemSno = toText(readCell(rowNumber, 20));
                var scheduleNo = toText(readCell(rowNumber, 21));
                var serialSno = toText(readCell(rowNumber, 22));

                // Parse "Name | SNxxx" display if present
                var parsedSnoFromSaleType = "";
                var saleTypeNameOnly = saleType;
                if (saleType.indexOf("|") >= 0) {
                    var parts = saleType.split("|");
                    saleTypeNameOnly = toText(parts[0]);
                    parsedSnoFromSaleType = toText(parts.slice(1).join("|"));
                }
                if (!itemSno && parsedSnoFromSaleType) {
                    itemSno = parsedSnoFromSaleType;
                }

                if (typeof fbrType !== "undefined" && fbrType && fbrType.length) {
                    var selected = null;
                    // 1) Match by Item SNO (handles duplicate S_NAME)
                    if (itemSno) {
                        selected = fbrType.find(function (x) {
                            return String(x.item_Sno || "").toLowerCase() === itemSno.toLowerCase();
                        });
                    }
                    // 2) Match by CODE
                    if (!selected && saleType) {
                        selected = fbrType.find(function (x) {
                            return String(x.key) === saleType;
                        });
                    }
                    // 3) Exact unique display "name | sno"
                    if (!selected && saleType) {
                        selected = fbrType.find(function (x) {
                            var display = (x.value || "") + (x.item_Sno ? (" | " + x.item_Sno) : "");
                            return display.toLowerCase() === saleType.toLowerCase();
                        });
                    }
                    // 4) Fallback by S_NAME only (ambiguous)
                    if (!selected && saleTypeNameOnly) {
                        selected = fbrType.find(function (x) {
                            return String(x.value || "").toLowerCase() === saleTypeNameOnly.toLowerCase();
                        });
                    }
                    if (selected) {
                        saleType = selected.value || saleTypeNameOnly || saleType;
                        itemSno = selected.item_Sno || itemSno;
                        scheduleNo = selected.sche_No || scheduleNo;
                        serialSno = selected.seri_No || serialSno;
                    }
                }

                rows.push({
                    vDate: vDate,
                    partyName: partyName,
                    partyAddress: toText(readCell(rowNumber, 3)),
                    partyRegion: toText(readCell(rowNumber, 4)),
                    partyRegType: toText(readCell(rowNumber, 5)),
                    partyNtn: toText(readCell(rowNumber, 6)),
                    partyStReg: toText(readCell(rowNumber, 7)),
                    itemName: itemName,
                    itemUnit: toText(readCell(rowNumber, 9)),
                    hsCode: toText(readCell(rowNumber, 10)),
                    invoiceQty: qty,
                    rate: rate,
                    amount: amount,
                    wRate: wRate,
                    wAmt: wAmt,
                    tax: tax,
                    taxAmt: taxAmt,
                    netAmount: netAmount,
                    saleType: saleType,
                    itemSno: itemSno,
                    scheduleNo: scheduleNo,
                    serialSno: serialSno
                });
            });

            if (!rows.length) {
                empr_helper.notify("No data rows found in Excel.", 2);
                return;
            }

            empr_PurchaseBill_InvoiceBulkUpload.excelRows = rows;
            empr_PurchaseBill_InvoiceBulkUpload.CheckParties();
        }).catch(function () {
            empr_helper.notify("Unable to read Excel file.", 2);
        });
    },

    GetUniqueParties: function (includeIncomplete) {
        var map = {};
        empr_PurchaseBill_InvoiceBulkUpload.excelRows.forEach(function (r) {
            var key = ((r.partyNtn || "") + "|" + (r.partyName || "")).trim().toLowerCase();
            if (!includeIncomplete) {
                key = (r.partyNtn || "").trim().toLowerCase();
                if (!key) return;
            } else if (!key || key === "|") {
                key = "row_" + Math.random();
            }
            if (!map[key]) {
                map[key] = {
                    partyName: r.partyName,
                    partyAddress: r.partyAddress,
                    partyRegion: r.partyRegion,
                    partyRegType: r.partyRegType,
                    partyNtn: r.partyNtn,
                    partyStReg: r.partyStReg
                };
            }
        });
        return Object.keys(map).map(function (k) { return map[k]; });
    },

    GetInvoiceGroupCount: function (rows) {
        var keys = {};
        (rows || []).forEach(function (r) {
            var key = ((r.partyName || "") + "|" + (r.partyNtn || "")).trim().toLowerCase();
            keys[key] = true;
        });
        return Object.keys(keys).length;
    },

    GetUniqueItems: function (includeIncomplete) {
        var map = {};
        empr_PurchaseBill_InvoiceBulkUpload.excelRows.forEach(function (r) {
            var key = ((r.itemName || "") + "|" + (r.hsCode || "")).trim().toLowerCase();
            if (!includeIncomplete) {
                if (!key || key === "|") return;
            } else if (!key || key === "|") {
                key = "item_" + Math.random();
            }
            if (!map[key]) {
                map[key] = {
                    itemName: r.itemName,
                    itemUnit: r.itemUnit,
                    hsCode: r.hsCode
                };
            }
        });
        return Object.keys(map).map(function (k) { return map[k]; });
    },

    CheckParties: function () {
        var validation = empr_PurchaseBill_InvoiceBulkUpload.ValidatePartiesBeforeCheck();
        if (validation.errors.length) {
            empr_PurchaseBill_InvoiceBulkUpload.ShowPartyErrors(validation.errors);
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(
                validation.errors.length + " party(ies) have empty required fields. Process cancelled.",
                2,
                true,
                false
            );
            return;
        }

        $.ajax({
            url: "/PurchaseBill/InvoiceBulkCheckParties",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({ parties: validation.valid }),
            success: function (res) {
                if (res.msgType !== 1) {
                    empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(res.msg || "Party check failed. Process cancelled.");
                    return;
                }
                empr_PurchaseBill_InvoiceBulkUpload.ShowPartyErrors([]);
                empr_PurchaseBill_InvoiceBulkUpload.partyRows = res.data || [];

                if (!empr_PurchaseBill_InvoiceBulkUpload.partyRows.length) {
                    empr_PurchaseBill_InvoiceBulkUpload.partiesReady = true;
                    empr_helper.notify("All parties already exist.", 1);
                    empr_PurchaseBill_InvoiceBulkUpload.GoToItemsStep();
                } else {
                    empr_PurchaseBill_InvoiceBulkUpload.partiesReady = false;
                    empr_PurchaseBill_InvoiceBulkUpload.ShowStep(2);
                    empr_PurchaseBill_InvoiceBulkUpload.RenderPartyGrid();
                }
            },
            error: function () {
                empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("Party check request failed. Process cancelled.");
            }
        });
    },

    RenderPartyGrid: function () {
        var data = (empr_PurchaseBill_InvoiceBulkUpload.partyRows || []).map(function (x, i) {
            return Object.assign({ id: i + 1, selected: true }, x);
        });
        empr_PurchaseBill_InvoiceBulkUpload.partyRows = data;

        $("#ibuPartyGrid").dxDataGrid({
            dataSource: data,
            keyExpr: "id",
            showBorders: true,
            columnAutoWidth: true,
            paging: { pageSize: 10 },
            columns: [
                {
                    dataField: "selected",
                    caption: "",
                    width: 50,
                    dataType: "boolean",
                    visible: false
                },
                { dataField: "partyName", caption: "Party Name", allowEditing: false },
                { dataField: "partyNtn", caption: "NTN / CNIC", allowEditing: false },
                { dataField: "partyAddress", caption: "Address", allowEditing: false },
                { dataField: "partyRegion", caption: "Region", allowEditing: false },
                { dataField: "partyRegType", caption: "Reg.Type", allowEditing: false },
                { dataField: "partyStReg", caption: "ST.Reg", allowEditing: false },
                {
                    dataField: "status",
                    caption: "Status",
                    allowEditing: false,
                    cellTemplate: function (container, options) {
                        $("<span>")
                            .css({ color: "#dc3545", fontWeight: "600" })
                            .text(options.value || "")
                            .appendTo(container);
                    }
                }
            ],
            editing: { mode: "cell", allowUpdating: false }
        });
    },

    RemovePartyRow: function (id) {
        // Row remove disabled for now; kept for future checkbox support.
        return;
    },

    AddParties: function () {
        var selected = (empr_PurchaseBill_InvoiceBulkUpload.partyRows || []).filter(function (x) { return x.selected !== false; });
        if (!selected.length) {
            if (empr_PurchaseBill_InvoiceBulkUpload.partiesReady) {
                empr_PurchaseBill_InvoiceBulkUpload.GoToItemsStep();
                return;
            }
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(
                "No parties selected to add. Required parties are missing. Process cancelled."
            );
            return;
        }

        var errors = [];
        selected.forEach(function (p) {
            var empty = empr_PurchaseBill_InvoiceBulkUpload.GetPartyEmptyFields(p);
            if (empty.length) {
                errors.push({ name: p.partyName || p.partyNtn || "N/A", fields: empty });
            }
        });
        if (errors.length) {
            empr_PurchaseBill_InvoiceBulkUpload.ShowPartyErrors(errors);
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(
                errors.length + " party(ies) have empty required fields. Process cancelled.",
                2,
                true,
                false
            );
            return;
        }

        $.ajax({
            url: "/PurchaseBill/InvoiceBulkAddParties",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({ parties: selected }),
            success: function (res) {
                if (res.msgType !== 1) {
                    empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(res.msg || "Unable to add parties. Process cancelled.");
                    return;
                }
                empr_helper.notify(res.msg || "Parties added.", 1);
                empr_PurchaseBill_InvoiceBulkUpload.partiesReady = true;
                empr_PurchaseBill_InvoiceBulkUpload.partyRows = [];
                empr_PurchaseBill_InvoiceBulkUpload.GoToItemsStep();
            },
            error: function () {
                empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("Add Party request failed. Process cancelled.");
            }
        });
    },

    GoToItemsStep: function () {
        if (!empr_PurchaseBill_InvoiceBulkUpload.partiesReady) {
            if ((empr_PurchaseBill_InvoiceBulkUpload.partyRows || []).length > 0) {
                empr_helper.notify("Please click Add Party first. Pending parties must be added.", 2);
                return;
            }
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(
                "Parties are not ready. Process cancelled."
            );
            return;
        }

        if (!empr_PurchaseBill_InvoiceBulkUpload.excelRows.length) {
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("Excel data lost. Process cancelled.");
            return;
        }

        var validation = empr_PurchaseBill_InvoiceBulkUpload.ValidateItemsBeforeCheck();
        if (validation.errors.length) {
            empr_PurchaseBill_InvoiceBulkUpload.ShowItemErrors(validation.errors);
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(
                validation.errors.length + " item(s) have empty required fields. Process cancelled.",
                3,
                false,
                true
            );
            return;
        }

        $.ajax({
            url: "/PurchaseBill/InvoiceBulkCheckItems",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({ items: validation.valid }),
            success: function (res) {
                if (res.msgType !== 1) {
                    empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(res.msg || "Item check failed. Process cancelled.");
                    return;
                }
                empr_PurchaseBill_InvoiceBulkUpload.ShowItemErrors([]);
                empr_PurchaseBill_InvoiceBulkUpload.itemRows = res.data || [];

                if (!empr_PurchaseBill_InvoiceBulkUpload.itemRows.length) {
                    empr_PurchaseBill_InvoiceBulkUpload.itemsReady = true;
                    empr_helper.notify("All items already exist.", 1);
                    empr_PurchaseBill_InvoiceBulkUpload.GoToInvoiceStep();
                } else {
                    empr_PurchaseBill_InvoiceBulkUpload.itemsReady = false;
                    empr_PurchaseBill_InvoiceBulkUpload.ShowStep(3);
                    empr_PurchaseBill_InvoiceBulkUpload.RenderItemGrid();
                }
            },
            error: function () {
                empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("Item check request failed. Process cancelled.");
            }
        });
    },

    RenderItemGrid: function () {
        var data = (empr_PurchaseBill_InvoiceBulkUpload.itemRows || []).map(function (x, i) {
            return Object.assign({ id: i + 1, selected: true }, x);
        });
        empr_PurchaseBill_InvoiceBulkUpload.itemRows = data;

        $("#ibuItemGrid").dxDataGrid({
            dataSource: data,
            keyExpr: "id",
            showBorders: true,
            columnAutoWidth: true,
            paging: { pageSize: 10 },
            columns: [
                {
                    dataField: "selected",
                    caption: "",
                    width: 50,
                    dataType: "boolean",
                    visible: false
                },
                { dataField: "itemName", caption: "Item Name", allowEditing: false },
                { dataField: "itemUnit", caption: "Unit", allowEditing: false },
                { dataField: "hsCode", caption: "HS Code", allowEditing: false },
                {
                    dataField: "status",
                    caption: "Status",
                    allowEditing: false,
                    cellTemplate: function (container, options) {
                        $("<span>")
                            .css({ color: "#dc3545", fontWeight: "600" })
                            .text(options.value || "")
                            .appendTo(container);
                    }
                }
            ],
            editing: { mode: "cell", allowUpdating: false }
        });
    },

    RemoveItemRow: function (id) {
        // Row remove disabled for now; kept for future checkbox support.
        return;
    },

    AddItems: function () {
        var selected = (empr_PurchaseBill_InvoiceBulkUpload.itemRows || []).filter(function (x) { return x.selected !== false; });
        if (!selected.length) {
            if (empr_PurchaseBill_InvoiceBulkUpload.itemsReady) {
                empr_PurchaseBill_InvoiceBulkUpload.GoToInvoiceStep();
                return;
            }
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(
                "No items selected to add. Required items are missing. Process cancelled."
            );
            return;
        }

        var errors = [];
        selected.forEach(function (it) {
            var empty = empr_PurchaseBill_InvoiceBulkUpload.GetItemEmptyFields(it);
            if (empty.length) {
                errors.push({ name: it.itemName || it.hsCode || "N/A", fields: empty });
            }
        });
        if (errors.length) {
            empr_PurchaseBill_InvoiceBulkUpload.ShowItemErrors(errors);
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(
                errors.length + " item(s) have empty required fields. Process cancelled.",
                3,
                false,
                true
            );
            return;
        }

        $.ajax({
            url: "/PurchaseBill/InvoiceBulkAddItems",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({ items: selected }),
            success: function (res) {
                if (res.msgType !== 1) {
                    empr_PurchaseBill_InvoiceBulkUpload.CancelProcess(res.msg || "Unable to add items. Process cancelled.");
                    return;
                }
                empr_helper.notify(res.msg || "Items added.", 1);
                empr_PurchaseBill_InvoiceBulkUpload.itemsReady = true;
                empr_PurchaseBill_InvoiceBulkUpload.itemRows = [];
                empr_PurchaseBill_InvoiceBulkUpload.GoToInvoiceStep();
            },
            error: function () {
                empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("Add Items request failed. Process cancelled.");
            }
        });
    },

    GoToInvoiceStep: function () {
        if (!empr_PurchaseBill_InvoiceBulkUpload.partiesReady) {
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("Parties are not ready. Process cancelled.");
            return;
        }
        if (!empr_PurchaseBill_InvoiceBulkUpload.itemsReady) {
            if ((empr_PurchaseBill_InvoiceBulkUpload.itemRows || []).length > 0) {
                empr_helper.notify("Please click Add Items first. Pending items must be added.", 2);
                return;
            }
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("Items are not ready. Process cancelled.");
            return;
        }

        var rows = empr_PurchaseBill_InvoiceBulkUpload.excelRows || [];
        if (!rows.length) {
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("Excel data lost. Process cancelled.");
            return;
        }

        empr_PurchaseBill_InvoiceBulkUpload.ShowStep(4);
        $("#ibuInvoiceSummary").html(
            "<strong>Total Excel rows:</strong> " + rows.length +
            " &nbsp;|&nbsp; <strong>Invoices to create:</strong> " + empr_PurchaseBill_InvoiceBulkUpload.GetInvoiceGroupCount(rows) +
            " <span class=\"text-muted\">(same Party Name + NTN = 1 invoice)</span>"
        );
        empr_PurchaseBill_InvoiceBulkUpload.RenderInvoiceGrid(rows);
    },

    RenderInvoiceGrid: function (rows) {
        var data = (rows || []).map(function (x, i) {
            return {
                id: i + 1,
                vDate: x.vDate,
                partyName: x.partyName,
                partyNtn: x.partyNtn,
                itemName: x.itemName,
                itemUnit: x.itemUnit,
                hsCode: x.hsCode,
                invoiceQty: x.invoiceQty,
                rate: x.rate,
                amount: x.amount,
                wRate: x.wRate,
                wAmt: x.wAmt,
                tax: x.tax,
                taxAmt: x.taxAmt,
                netAmount: x.netAmount,
                saleType: x.saleType,
                itemSno: x.itemSno,
                scheduleNo: x.scheduleNo,
                serialSno: x.serialSno
            };
        });

        $("#ibuInvoiceGrid").dxDataGrid({
            dataSource: data,
            keyExpr: "id",
            showBorders: true,
            columnAutoWidth: true,
            wordWrapEnabled: true,
            paging: { pageSize: 10 },
            scrolling: { useNative: true },
            columns: [
                { dataField: "vDate", caption: "V_Date", allowEditing: false },
                { dataField: "partyName", caption: "Party Name", allowEditing: false },
                { dataField: "partyNtn", caption: "NTN / CNIC", allowEditing: false },
                { dataField: "itemName", caption: "Item Name", allowEditing: false },
                { dataField: "itemUnit", caption: "Unit", allowEditing: false },
                { dataField: "hsCode", caption: "HS Code", allowEditing: false },
                { dataField: "invoiceQty", caption: "Qty", allowEditing: false },
                { dataField: "rate", caption: "Rate", allowEditing: false },
                { dataField: "amount", caption: "Amount", allowEditing: false },
                { dataField: "wRate", caption: "W.Rate", allowEditing: false },
                { dataField: "wAmt", caption: "W.Amt", allowEditing: false },
                { dataField: "tax", caption: "Tax", allowEditing: false },
                { dataField: "taxAmt", caption: "Tax Amt", allowEditing: false },
                { dataField: "netAmount", caption: "Net Amount", allowEditing: false },
                { dataField: "saleType", caption: "Sale Type", allowEditing: false },
                { dataField: "itemSno", caption: "Item SNO", allowEditing: false },
                { dataField: "scheduleNo", caption: "Schedule No", allowEditing: false },
                { dataField: "serialSno", caption: "Serial SNO", allowEditing: false }
            ]
        });
    },

    UploadInvoices: function () {
        if (!empr_PurchaseBill_InvoiceBulkUpload.partiesReady || !empr_PurchaseBill_InvoiceBulkUpload.itemsReady) {
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("Parties/Items are not ready. Process cancelled.");
            return;
        }

        var rows = empr_PurchaseBill_InvoiceBulkUpload.excelRows || [];
        if (!rows.length) {
            empr_PurchaseBill_InvoiceBulkUpload.CancelProcess("No excel rows to upload. Process cancelled.");
            return;
        }

        $.ajax({
            url: "/PurchaseBill/InvoiceBulkUploadInvoices",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({ rows: rows }),
            success: function (res) {
                if (res.msgType !== 1) {
                    empr_helper.notify(res.msg || "Invoice upload aborted.", 2);
                    return;
                }
                empr_helper.notify(res.msg || "Invoices uploaded.", 1);
                $("#InvoiceBulkUploadModal").modal("hide");
                empr_PurchaseBill_InvoiceBulkUpload.RefreshPartyAndItemDropdowns();
                if (typeof empr_PurchaseBill !== "undefined" && empr_PurchaseBill.GetPurchaseBill) {
                    empr_PurchaseBill.GetPurchaseBill();
                }
            },
            error: function () {
                empr_helper.notify("Invoice upload request failed.", 2);
            }
        });
    },

    RefreshPartyAndItemDropdowns: function () {
        var vDate = (typeof todayDate !== "undefined" && todayDate)
            ? todayDate
            : ((typeof empr_PurchaseBill !== "undefined" && empr_PurchaseBill.vDate) ? empr_PurchaseBill.vDate : "");

        // Refresh Party dropdown datasource
        $.ajax({
            url: "/PurchaseBill/GetPartyCurrentBalance?vDate=" + encodeURIComponent(vDate),
            type: "GET",
            success: function (data) {
                if (!Array.isArray(data)) return;
                if (typeof PartyType !== "undefined" && Array.isArray(PartyType)) {
                    PartyType.length = 0;
                    Array.prototype.push.apply(PartyType, data);
                } else {
                    window.PartyType = data;
                }

                var selectedParty = null;
                try {
                    selectedParty = $("#PARTY_CODE").dxSelectBox("instance").option("value");
                } catch (e) { }

                if (typeof empr_PurchaseBill !== "undefined" && empr_PurchaseBill.InitPartyType) {
                    empr_PurchaseBill.InitPartyType(selectedParty);
                }
            }
        });

        // Refresh Item dropdown datasource (detail grid lookup)
        $.ajax({
            url: "/PurchaseBill/GetItems",
            type: "GET",
            success: function (res) {
                var data = (res && res.msgType === 1) ? res.data : res;
                if (!Array.isArray(data)) return;

                if (typeof Items !== "undefined" && Array.isArray(Items)) {
                    Items.length = 0;
                    Array.prototype.push.apply(Items, data);
                } else {
                    window.Items = data;
                }

                try {
                    var grid = $("#DetailContainer").dxDataGrid("instance");
                    if (grid) {
                        grid.columnOption("iteM_CODE", "lookup.dataSource", {
                            store: Items,
                            paginate: true,
                            pageSize: 50
                        });
                    }
                } catch (e) { }
            }
        });
    }
};

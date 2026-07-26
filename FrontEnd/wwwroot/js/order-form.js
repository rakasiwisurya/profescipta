// ---------------------------------------------------------------------
// Halaman Order Input (Create & Edit) — pengelolaan baris item.
//
// ATURAN PENTING (FSD bagian 5.3 & 7.2):
// File ini TIDAK PERNAH menghitung TOTAL baris maupun Grand Total.
// Setiap kali daftar item berubah, seluruh baris dikirim ke
// Orders/CalculateItems -> Sales Order Service, dan angka yang
// ditampilkan diambil apa adanya dari respons service.
// Yang dilakukan di sini hanya: mengatur tampilan baris (mode input /
// mode tampil), mengirim data ke service, dan memformat angka untuk
// dibaca manusia (pemisah ribuan) — format, bukan kalkulasi.
// ---------------------------------------------------------------------
(function () {
    "use strict";

    var itemsBody = document.getElementById("itemsBody");

    if (!itemsBody) {
        return;
    }

    var config = JSON.parse(document.getElementById("orderFormConfig").textContent);
    var hiddenInputsContainer = document.getElementById("itemsHiddenInputs");
    var grandTotalDisplay = document.getElementById("grandTotalDisplay");
    var addItemButton = document.getElementById("addItemButton");
    var orderForm = document.getElementById("orderForm");

    // ---- State halaman ----
    // items  : baris yang sudah tersimpan sementara di form
    // draft  : baris yang sedang dalam mode input (null kalau tidak ada)
    //          { rowIndex: nomor baris yang diedit atau null untuk baris baru,
    //            itemName, quantity, price, errors: [] }
    var items = JSON.parse(document.getElementById("initialItemsData").textContent) || [];
    var draft = null;

    var moneyFormatter = new Intl.NumberFormat("id-ID", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    // -----------------------------------------------------------------
    // Render
    // -----------------------------------------------------------------
    function render() {
        itemsBody.innerHTML = "";

        if (items.length === 0 && draft === null) {
            var emptyRow = document.createElement("tr");
            emptyRow.innerHTML =
                '<td colspan="6" class="so-empty">Belum ada item. Klik "+ Add Item" untuk menambah baris.</td>';
            itemsBody.appendChild(emptyRow);
        }

        items.forEach(function (item, index) {
            // Baris yang sedang diedit ditampilkan sebagai baris input,
            // bukan baris tampil.
            if (draft !== null && draft.rowIndex === index) {
                itemsBody.appendChild(buildEditingRow(index + 1));
                return;
            }

            itemsBody.appendChild(buildDisplayRow(item, index));
        });

        // Baris baru (belum masuk items) selalu muncul di paling bawah.
        if (draft !== null && draft.rowIndex === null) {
            itemsBody.appendChild(buildEditingRow(items.length + 1));
        }

        renderHiddenInputs();
        focusDraftInput();
    }

    function buildDisplayRow(item, index) {
        var row = document.createElement("tr");

        row.appendChild(createCell(index + 1, "so-col-num"));
        row.appendChild(createCell(item.itemName, ""));
        row.appendChild(createCell(item.quantity, ""));
        row.appendChild(createCell(moneyFormatter.format(item.price), "text-money"));

        // Nilai total di sini berasal dari respons service.
        row.appendChild(createCell(moneyFormatter.format(item.total), "text-money"));

        var actionCell = document.createElement("td");
        actionCell.className = "so-col-action";

        var editButton = createIconButton("✎", "Ubah baris ini");
        editButton.addEventListener("click", function () {
            startEditRow(index);
        });

        var deleteButton = createIconButton("🗑", "Hapus baris ini");
        deleteButton.addEventListener("click", function () {
            removeRow(index);
        });

        actionCell.appendChild(editButton);
        actionCell.appendChild(deleteButton);
        row.appendChild(actionCell);

        return row;
    }

    function buildEditingRow(displayNumber) {
        var row = document.createElement("tr");
        row.className = "so-row-editing";

        row.appendChild(createCell(displayNumber, "so-col-num"));

        // ---- Input nama item ----
        var nameCell = document.createElement("td");
        var nameInput = createInput("text", draft.itemName, "Nama Barang...");
        nameInput.maxLength = 100;
        nameInput.id = "draftItemName";
        nameCell.appendChild(nameInput);

        // Pesan error dari service ditampilkan inline di bawah input.
        if (draft.errors.length > 0) {
            var errorList = document.createElement("ul");
            errorList.className = "so-row-errors";

            draft.errors.forEach(function (message) {
                var errorItem = document.createElement("li");
                errorItem.textContent = message;
                errorList.appendChild(errorItem);
            });

            nameCell.appendChild(errorList);
        }

        row.appendChild(nameCell);

        // ---- Input qty ----
        var quantityCell = document.createElement("td");
        var quantityInput = createInput("number", draft.quantity, "0");
        quantityInput.min = "1";
        quantityInput.step = "1";
        quantityInput.id = "draftQuantity";
        quantityCell.appendChild(quantityInput);
        row.appendChild(quantityCell);

        // ---- Input harga ----
        var priceCell = document.createElement("td");
        var priceInput = createInput("number", draft.price, "0");
        priceInput.min = "0";
        priceInput.step = "0.01";
        priceInput.id = "draftPrice";
        priceCell.appendChild(priceInput);
        row.appendChild(priceCell);

        // Kolom Total dibiarkan kosong sampai service mengirim hasilnya.
        var totalCell = createCell("", "text-money text-muted");
        totalCell.textContent = "–";
        row.appendChild(totalCell);

        // ---- Tombol simpan / batal baris ----
        var actionCell = document.createElement("td");
        actionCell.className = "so-col-action";

        var confirmButton = document.createElement("button");
        confirmButton.type = "button";
        confirmButton.className = "btn btn-success btn-sm me-1";
        confirmButton.title = "Simpan baris";
        confirmButton.innerHTML = "&#10003;";
        confirmButton.addEventListener("click", confirmDraft);

        var cancelButton = document.createElement("button");
        cancelButton.type = "button";
        cancelButton.className = "btn btn-outline-secondary btn-sm";
        cancelButton.title = "Batalkan baris";
        cancelButton.innerHTML = "&#10007;";
        cancelButton.addEventListener("click", cancelDraft);

        actionCell.appendChild(confirmButton);
        actionCell.appendChild(cancelButton);
        row.appendChild(actionCell);

        return row;
    }

    /// Hidden input inilah yang benar-benar terkirim ke server saat Save Order.
    function renderHiddenInputs() {
        hiddenInputsContainer.innerHTML = "";

        items.forEach(function (item, index) {
            appendHiddenInput("Items[" + index + "].ItemName", item.itemName);
            appendHiddenInput("Items[" + index + "].Quantity", item.quantity);
            appendHiddenInput("Items[" + index + "].Price", item.price);
            appendHiddenInput("Items[" + index + "].Total", item.total);
        });
    }

    // -----------------------------------------------------------------
    // Aksi pengguna
    // -----------------------------------------------------------------
    function addItem() {
        if (draft !== null) {
            // Hanya satu baris boleh dalam mode input sekaligus, supaya
            // tidak ada baris setengah jadi yang tertinggal.
            alert("Selesaikan dulu baris item yang sedang diisi (tombol ✓ atau ✗).");
            return;
        }

        draft = { rowIndex: null, itemName: "", quantity: "", price: "", errors: [] };
        render();
    }

    function startEditRow(index) {
        if (draft !== null) {
            alert("Selesaikan dulu baris item yang sedang diisi (tombol ✓ atau ✗).");
            return;
        }

        var item = items[index];

        draft = {
            rowIndex: index,
            itemName: item.itemName,
            quantity: item.quantity,
            price: item.price,
            errors: []
        };

        render();
    }

    function cancelDraft() {
        // Membatalkan baris input tidak mempengaruhi baris lain
        // yang sudah tersimpan sementara.
        draft = null;
        render();
    }

    /// Tombol ✓ : minta service memvalidasi + menghitung, baru baris disimpan.
    function confirmDraft() {
        readDraftFromInputs();

        var candidateItems = buildItemsWithDraft();
        var draftPosition = draft.rowIndex === null ? items.length : draft.rowIndex;

        requestCalculation(candidateItems).then(function (calculation) {
            if (calculation === null) {
                return;
            }

            var draftResult = calculation.items[draftPosition];

            if (!draftResult.isValid) {
                // Baris tetap dalam mode input, error ditampilkan inline.
                draft.errors = draftResult.errors;
                render();
                return;
            }

            // Baris valid: pindahkan ke daftar tersimpan sementara,
            // memakai angka hasil kalkulasi service.
            var savedRow = {
                itemName: draftResult.itemName,
                quantity: draftResult.quantity,
                price: draftResult.price,
                total: draftResult.total
            };

            if (draft.rowIndex === null) {
                items.push(savedRow);
            } else {
                items[draft.rowIndex] = savedRow;
            }

            draft = null;

            applyCalculation(calculation);
        });
    }

    function removeRow(index) {
        if (draft !== null) {
            alert("Selesaikan dulu baris item yang sedang diisi (tombol ✓ atau ✗).");
            return;
        }

        // Tidak ada popup konfirmasi untuk hapus item di form (FSD bagian 4.2).
        items.splice(index, 1);

        if (items.length === 0) {
            // Tidak ada yang perlu dihitung service; kosongkan tampilan total.
            grandTotalDisplay.textContent = moneyFormatter.format(0);
            render();
            return;
        }

        requestCalculation(items.map(toRequestItem)).then(function (calculation) {
            if (calculation === null) {
                render();
                return;
            }

            applyCalculation(calculation);
        });
    }

    // -----------------------------------------------------------------
    // Komunikasi ke service (via controller front-end)
    // -----------------------------------------------------------------
    function requestCalculation(requestItems) {
        return fetch(config.calculateUrl, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ items: requestItems })
        })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error("Service membalas status " + response.status);
                }

                return response.json();
            })
            .catch(function (error) {
                alert("Gagal menghubungi Sales Order Service: " + error.message);
                return null;
            });
    }

    /// Menyalin angka hasil kalkulasi service ke tampilan tabel.
    function applyCalculation(calculation) {
        calculation.items.forEach(function (calculated) {
            if (calculated.rowIndex < items.length) {
                items[calculated.rowIndex].total = calculated.total;
            }
        });

        grandTotalDisplay.textContent = moneyFormatter.format(calculation.grandTotal);

        render();
    }

    // -----------------------------------------------------------------
    // Utilitas
    // -----------------------------------------------------------------
    function readDraftFromInputs() {
        draft.itemName = document.getElementById("draftItemName").value;
        draft.quantity = document.getElementById("draftQuantity").value;
        draft.price = document.getElementById("draftPrice").value;
        draft.errors = [];
    }

    /// Menyusun daftar item versi request: baris draft ikut disertakan
    /// pada posisinya, supaya service bisa memvalidasi & menghitung semuanya.
    function buildItemsWithDraft() {
        var requestItems = items.map(toRequestItem);

        var draftItem = {
            itemName: draft.itemName,
            quantity: toNumberOrNull(draft.quantity),
            price: toNumberOrNull(draft.price)
        };

        if (draft.rowIndex === null) {
            requestItems.push(draftItem);
        } else {
            requestItems[draft.rowIndex] = draftItem;
        }

        return requestItems;
    }

    function toRequestItem(item) {
        return {
            itemName: item.itemName,
            quantity: toNumberOrNull(item.quantity),
            price: toNumberOrNull(item.price)
        };
    }

    /// Mengubah isi input menjadi angka JSON. Input kosong dikirim sebagai
    /// null supaya service yang menentukan pesan errornya.
    function toNumberOrNull(value) {
        if (value === null || value === undefined || String(value).trim() === "") {
            return null;
        }

        var parsed = Number(value);

        return isNaN(parsed) ? null : parsed;
    }

    function createCell(text, className) {
        var cell = document.createElement("td");

        if (className) {
            cell.className = className;
        }

        cell.textContent = text;

        return cell;
    }

    function createInput(type, value, placeholder) {
        var input = document.createElement("input");
        input.type = type;
        input.className = "form-control form-control-sm";
        input.value = value === null || value === undefined ? "" : value;
        input.placeholder = placeholder;

        // Enter di dalam baris item berarti "simpan baris", bukan submit form.
        input.addEventListener("keydown", function (event) {
            if (event.key === "Enter") {
                event.preventDefault();
                confirmDraft();
            }
        });

        return input;
    }

    function createIconButton(symbol, title) {
        var button = document.createElement("button");
        button.type = "button";
        button.className = "btn-icon";
        button.title = title;
        button.textContent = symbol;

        return button;
    }

    function appendHiddenInput(name, value) {
        var input = document.createElement("input");
        input.type = "hidden";
        input.name = name;
        input.value = value === null || value === undefined ? "" : value;

        hiddenInputsContainer.appendChild(input);
    }

    function focusDraftInput() {
        var nameInput = document.getElementById("draftItemName");

        if (nameInput) {
            nameInput.focus();
        }
    }

    // -----------------------------------------------------------------
    // Pemasangan event & render awal
    // -----------------------------------------------------------------
    addItemButton.addEventListener("click", addItem);

    orderForm.addEventListener("submit", function (event) {
        if (draft !== null) {
            event.preventDefault();
            alert("Selesaikan dulu baris item yang sedang diisi sebelum menyimpan order.");
        }
    });

    render();
})();

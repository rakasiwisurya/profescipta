// ---------------------------------------------------------------------
// Halaman Order List.
// Tugas file ini hanya satu: mengisi popup konfirmasi hapus dengan nomor
// SO dari baris yang diklik, lalu menampilkan popup-nya. Penghapusannya
// sendiri dikerjakan server (form POST ke Orders/Delete -> service).
// ---------------------------------------------------------------------
(function () {
    "use strict";

    var modalElement = document.getElementById("deleteOrderModal");

    if (!modalElement) {
        return;
    }

    var deleteModal = new bootstrap.Modal(modalElement);
    var orderIdInput = document.getElementById("deleteOrderId");
    var orderNoLabel = document.getElementById("deleteOrderNo");

    document.querySelectorAll(".js-delete-order").forEach(function (button) {
        button.addEventListener("click", function () {
            orderIdInput.value = button.getAttribute("data-order-id");

            // Nomor order ditampilkan dinamis sesuai baris yang dihapus.
            orderNoLabel.textContent = button.getAttribute("data-order-no");

            deleteModal.show();
        });
    });
})();

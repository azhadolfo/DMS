// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
toastr.options = {
    positionClass: "toast-top-right", // Adjust this to your preferred position
    preventDuplicates: true,          // Prevent duplicate notifications
};

// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function DisplayGeneralNotification(message, title) {
    setTimeout(function () {
        toastr.options = {
            closeButton: true,
            progressBar: true,
            showMethod: 'slideDown',
            timeOut: 4000
        };
        toastr.info(message, title);
    }, 1300);
}

function DisplayPersonalNotification(message, title) {
    setTimeout(function () {
        toastr.options = {
            closeButton: true,
            progressBar: true,
            showMethod: 'slideDown',
            timeOut: 4000
        };
        toastr.success(message, title);
    }, 1300);
}

//sorting
$(document).ready(function () {
    $('#myTable').DataTable();
});

//own table in gatepass
$(document).ready(function () {
    $('#myOwnTable').DataTable({
        "order": [[0, "desc"]] // Sort the first column (ID) in descending order
        // You can customize sorting options as needed
    });
});

//dropwdown
$(document).ready(function () {
    $("#categoryDropdown").select2({
        placeholder: "Select a category...",
        allowClear: true
    });
    $("#departmentDropdown").select2({
        placeholder: "Select a department...",
        allowClear: true
    });
});
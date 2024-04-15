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

//sorting
$(document).ready(function () {
    $('#myTable').DataTable({
        "stateSave": true,
        "deferRender": true
    });
});

//sorting remove sorting
$(document).ready(function () {
    $('#myTableNoSort').DataTable({
        "ordering": false,
        "stateSave": true
    });
});

//dropwdown
$(document).ready(function () {
    $("#categoryDropdown").select2({
        placeholder: "Select a category...",
        allowClear: true,
        width: 'resolve'
    });
    $("#departmentDropdown").select2({
        placeholder: "Select a department...",
        allowClear: true,
        width: 'resolve'
    });
    $("#companyDropdown").select2({
        placeholder: "Select a company...",
        allowClear: true,
        width: 'resolve'
    });
    $("#yearDropdown").select2({
        width: 'resolve'
    });
    $("#subCategoryDropdown").select2({
        placeholder: "Select a sub-category...",
    });
    $("#deliveryDropdown").select2({
        placeholder: "Select a sub-category...",
    });
    $("#governmentDropdown").select2({
        placeholder: "Select a sub-category...",
    });
});
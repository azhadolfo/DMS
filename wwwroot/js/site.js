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
    $('#myTable').DataTable();
});


//sorting remove sorting
$(document).ready(function () {
    $('#myTableNoSort').DataTable({
        "ordering": false
    });
});

//myOwnTable in gatepass
$(document).ready(function () {
    var currentPage = 0; // Store the current page number

    var newBetterTable = $('#newBetterTable').DataTable({
        "order": [[0, "desc"]],
        "rowId": "pk",
        "stateSave": true,
        "drawCallback": function (settings) {
            var indexes = newBetterTable.rows({ page: 'current' }).indexes();
            if (indexes.length === 0) {
                currentPage = 0;
            } else {
                currentPage = indexes[0];
            }
        }
    });

    // Load your initial data here (e.g., using newBetterTable.clear().rows.add() and newBetterTable.draw())

    newBetterTable.on('page.dt', function () {
        currentPage = newBetterTable.page.info().page;
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
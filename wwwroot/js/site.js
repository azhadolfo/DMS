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

//myOwnTable in gatepass
$(document).ready(function () {
    var currentPage = 0; // Store the current page number

    var table = $('#myOwnTable').DataTable({
        "order": [[0, "desc"]],
        "rowId": "pk",
        "stateSave": true,
        "drawCallback": function (settings) {
            // Get the row identifiers of the rows that are currently visible
            var visibleRows = table.rows({ page: 'current' }).data().toArray();
            table.state.save();
            // If visibleRows is empty, it means the table is empty (no rows)
            if (visibleRows.length === 0) {
                currentPage = 0; // Reset current page when there are no rows
            } else {
                // Find the index of the first visible row's "pk" value in the dataset
                var indexOfFirstVisibleRow = table.rows({ page: 'current' }).indexes()[0];
                var firstVisibleRowData = table.row(indexOfFirstVisibleRow).data();
                currentPage = visibleRows.indexOf(firstVisibleRowData);
                table.state.load();
            }
        }
    });

    // Load your initial data here (e.g., using table.clear().rows.add() and table.draw())

    // You can also set an event handler to handle page changes
    table.on('page.dt', function () {
        currentPage = table.page.info().page;
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
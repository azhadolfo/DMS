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
        "deferRender": true,
        "paging": true, // Enable pagination
        "lengthChange": true, // Disable length change
        "searching": true, // Enable search box
        "info": true, // Enable table information display
        "autoWidth": false, // Disable auto width calculation
        "order": [], // Disable initial sorting
        "columnDefs": [
            { "targets": 'no-sort', "orderable": false } // Disable sorting for specific columns
        ],
        "language": {
            "emptyTable": "No data available in table",
            "info": "Showing _START_ to _END_ of _TOTAL_ entries",
            "infoEmpty": "Showing 0 to 0 of 0 entries",
            "infoFiltered": "(filtered from _MAX_ total entries)",
            "zeroRecords": "No matching records found",
            "lengthMenu": "Show _MENU_ entries",
            "search": "Search:",
            "paginate": {
                "first": "First",
                "last": "Last",
                "next": "Next",
                "previous": "Previous"
            }
        }
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
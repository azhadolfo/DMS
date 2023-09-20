"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// Disable the send button until the connection is established.
document.getElementById("notif").disabled = true;



connection.on("ReceiveMessage", function (user, message) {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    li.textContent = `${user} says ${message}`;

    // Display a notification when a new message arrives
    toastr.success(`${user} says: ${message}`);
});



connection.on("Approved", function (message) {
    // Display a notification when a new message arrives
    Swal.fire(
        'Success',
        message,
        'success'
    )
});

connection.on("Disapproved", function (message) {
    // Display a notification when a new message arrives
    Swal.fire(
        'Success',
        message,
        'success'
    )
});


connection.start().then(function () {
    document.getElementById("notif").disabled = false;
}).catch(function (err) {
    console.error(err.toString());
});


document.getElementById("notif").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    var message = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", user, message).catch(function (err) {
        console.error(err.toString());
    });
    event.preventDefault();
});
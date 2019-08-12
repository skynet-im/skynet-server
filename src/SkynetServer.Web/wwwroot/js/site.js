// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your Javascript code.

"use strict";

(function () {
    let mainHeader = document.getElementsByClassName("main-header")[0];
    let content = document.getElementsByClassName("content")[0];
    $.post("/api" + window.location.pathname).done(function (data) {
        switch (data.statusCode) {
            case "Invalid":
                document.title = "Invalid token - Skynet";
                mainHeader.textContent = "Invalid confirmation code";
                content.innerHTML = '<p>The confirmation code you entered is invalid. Please note that after creating your account, the confirmation code is only valid for 24 hours.</p>';
                break;
            case "Success":
                document.title = "Successfully confirmed - Skynet";
                mainHeader.textContent = "Successfully confirmed";
                content.innerHTML = '<p>Your mail address <a href="mailto:' + data.mailAddress + '">' + data.mailAddress + '</a> has been successfully confirmed. Now you can log in for the first time. Enjoy using Skynet!</p>';
                break;
            case "Confirmed":
                document.title = "Address confirmed - Skynet";
                mainHeader.textContent = "Address already confirmed";
                content.innerHTML = '<p>Your mail address <a href="mailto:' + data.mailAddress + '">' + data.mailAddress + '</a> has already been confirmed before. Simply open the Skynet app and login. Enjoy using Skynet!</p>';
                break;
        }
    });
})();

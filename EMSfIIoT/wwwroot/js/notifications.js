"use strict";

var token = getCookie("ADB2CToken");
var sendButton = $("#sendButton")

if (token != null) {
    var connection = new signalR.HubConnectionBuilder()
        .withUrl("https://emsfiiot-api.azurewebsites.net/NotificationsHub", {
            accessTokenFactory: () => {
                return token;
            }
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.None)
        .build();

    connection.on("Notification", function (type, title, user, message) {
        var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

        toastr["info"](msg, user, { "iconClass": 'notification-info' });

        var localTime = moment.utc().local().format('DD/MM/YYYY HH:mm:ss');

        $(".notifications-badge > .dropdown-menu > .list-group > a:first").before(
            "<a href=\"#\" class=\"list-group-item list-group-item-action flex-column align-items-start\"> \
                <div class=\"d-flex w-100 justify-content-between\"> \
                    <h6 class=\"mb-1\">" + title +"</h6> \
                    <small>Just now</small> \
                </div> \
                <p class=\"mb-1\">" + user + "</p> \
                <small>" + message + "</small> \
            </a>"
        );
        $(".notifications-badge > #dropdownNotifications > span")
            .html(parseInt($(".notifications-badge > #dropdownNotifications > span").html()) + 1);

        var notificationsTable = $('#notificationsTable > tbody > tr:first');
        if (notificationsTable.length != 0) {
            notificationsTable.before(
                "<tr> \
                <td>" + localTime + "</td> \
                <td>" + localTime + "</td> \
                <td>" + type + "</td> \
                <td>" + title + "</td> \
                <td>" + message + "</td> \
                <td>" + user + "</td> \
                </tr>");

            connection.invoke("MarkNotificationsAsRead").catch(function (err) {
                return console.error(err.toString());
            });
            $(this).find(".badge").html("0");
        }       

        if (!document.hasFocus() & Notification.permission === "granted") {
            var n = new Notification(title, {
                body: msg
            });
            setTimeout(n.close.bind(n), 5000);

            n.addEventListener('click', function () {
                window.focus();
            });
        }
    });

    $("#dropdownNotifications").click(function () {
        connection.invoke("MarkNotificationsAsRead").catch(function (err) {
            return console.error(err.toString());
        });
        $(this).find(".badge").html("0");
    });

    connection.onclose(error => {
        sendButton.prop("disabled", true);
    })

    connection.onreconnected(connectionId => {
        sendButton.prop("disabled", false);
    })

    connection.start().then(function () {
        sendButton.prop("disabled", false);
    }).catch(function (err) {
        return console.error(err.toString());
    });

    sendButton.on("mouseup", function (event) {
        if ($(this).parent("form").hasClass("was-validated")) {
            var user = $("#userInput").val();
            var title = $("#titleInput").val();
            var message = $("#messageInput").val();
            connection.invoke("SendMessageToUser", "UserMessage", title, user, message).catch(function (err) {
                return console.error(err.toString());
            });
        }
        event.preventDefault();
    });
}
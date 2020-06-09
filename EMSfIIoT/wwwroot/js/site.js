// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function getCookie(name) {
    var v = document.cookie.match('(^|;) ?' + name + '=([^;]*)(;|$)');
    return v ? v[2] : null;
}
function setCookie(name, value, days) {
    var d = new Date;
    d.setTime(d.getTime() + 24 * 60 * 60 * 1000 * days);
    document.cookie = name + "=" + value + ";path=/;expires=" + d.toGMTString();
}
function deleteCookie(name) { setCookie(name, '', -1); }

function initFadeIn() {
    $("body").css("visibility", "visible");
    $("body").css("display", "none");
    $("body").fadeIn(1200);
}

$(document).ready(function () {
    var v = getCookie("sidebar");
    var x = window.matchMedia("(max-width: 768px)")

    //  Remove notificatons text
    if (x.matches) {
        $("#dropdownNotifications").contents().filter(function () {
            return (this.nodeType == 3);
        }).remove();
    }

    if (v == 'disabled' & !x.matches) {
        $('#sidebar').toggleClass('active');
        $('#sidebarCollapse').toggleClass('active');
    }

    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
        $(this).toggleClass('active');

        if ($('#sidebar').hasClass("active")) {
            setCookie('sidebar', 'active', 365);
        } else {
            setCookie('sidebar', 'disabled', 365);
        }
    });

    toastr.options = {
        "closeButton": true,
        "debug": false,
        "newestOnTop": true,
        "progressBar": true,
        "positionClass": "toast-top-right",
        "preventDuplicates": true,
        "onclick": null,
        "showDuration": "300",
        "hideDuration": "1000",
        "timeOut": "7000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    }

    /*
     * Handle timezone
    */

    var timezone = getCookie("timezone");
    if (timezone == null) {
        setCookie("timezone", String(moment().utcOffset()), 365)
    }

    $('[data-utc-time]').each(function () {
        var utcTime = $(this).data("utc-time");
        var localTime = moment.utc(utcTime, 'DD/MM/YYYY HH:mm:ss').local().format('DD/MM/YYYY HH:mm:ss');
        if (localTime === "Invalid date") {
            $(this).html("-");
        } else {
            $(this).html(localTime);
        }
    });

    var secs = 250;
    setTimeout('initFadeIn()', secs);

    Notification.requestPermission();
});
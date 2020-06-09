(function () {
    window.addEventListener("load", function () {
        setTimeout(function () {
            // Section 01 - Set url link 
            var logo = document.getElementsByClassName('link');
            logo[0].href = "https://emsfiiot.azurewebsites.net/";
            logo[0].target = "_blank";

            // Section 02 - Set logo
            logo[0].children[0].alt = "EMSfIIoT";
            logo[0].children[0].src = "/resources/logo.png";

            // Section 03 - Set favicon
            var link = document.querySelector("link[rel='icon']") || document.createElement('link');;
            document.head.removeChild(link);
            link = document.querySelector("link[rel='icon']") || document.createElement('link');
            document.head.removeChild(link);
            link = document.createElement('link');
            link.type = 'image/png';
            link.rel = 'shortcut icon';
            link.href = '/favicon.png';
            document.getElementsByTagName('head')[0].appendChild(link);
        });
    });
})();
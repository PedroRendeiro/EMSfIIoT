"use strict";

var items = document.currentScript.getAttribute('data-pagination');

var pagination = $(".pagination");

function updatePagination() {
    var rows = $(items + ":visible");

    var totalRecords = rows.length;
    var recPerPage = 10;
    var numberPages = Math.ceil(totalRecords / recPerPage);

    var currentItemsStart = 0;
    var currentPageStart = 1;

    pagination.html("");

    pagination.append("<li class=\"page-item disabled\" data-pagination=\"first\"><a class=\"page-link\" href=\"#\" aria-label=\"First\"><span aria-hidden=\"true\">&Lang;</span></a></li>");
    pagination.append("<li class=\"page-item disabled\" data-pagination=\"previous\"><a class=\"page-link\" href=\"#\" aria-label=\"Previous\"><span aria-hidden=\"true\">&lang;</span></a></li>");
    pagination.append("<li class=\"page-item disabled\" data-pagination=\"dotsFirst\" style=\"display:none\"><a class=\"page-link\" href=\"#\" ><span aria-hidden=\"true\">...</span></a></li>");
    pagination.append("<li class=\"page-item active\" data-pagination=\"1\"><a class=\"page-link\" href=\"#\">1</a></li>");
    for (var i = 2; i <= numberPages; i++) {
        pagination.append("<li class=\"page-item\" data-pagination=" + String(i) + "><a class=\"page-link\" href=\"#\">" + String(i) + "</a></li>");
    }
    pagination.append("<li class=\"page-item disabled\" data-pagination=\"dotsLast\"><a class=\"page-link\" href=\"#\" ><span aria-hidden=\"true\">...</span></a></li>");
    pagination.append("<li class=\"page-item\" data-pagination=\"next\"><a class=\"page-link\" href=\"#\" aria-label=\"Next\"><span aria-hidden=\"true\">&rang;</span></a></li>");
    pagination.append("<li class=\"page-item\" data-pagination=\"last\"><a class=\"page-link\" href=\"#\" aria-label=\"Last\"><span aria-hidden=\"true\">&Rang;</span></a></li>");
    if (totalRecords <= recPerPage) {
        $("li[data-pagination=\"next\"]").addClass("disabled");
        $("li[data-pagination=\"last\"]").addClass("disabled");
        $("li[data-pagination=\"dotsLast\"]").hide();
    }

    rows.hide().slice(currentItemsStart, currentItemsStart + recPerPage).show();

    var numbers = pagination.find("li")
        .not("[data-pagination=\"next\"]")
        .not("[data-pagination=\"previous\"]")
        .not("[data-pagination=\"first\"]")
        .not("[data-pagination=\"last\"]")
        .not(".disabled");

    numbers.each(function () {
        if ($(this).data("pagination") > 4) {
            $(this).hide();
        }
    })

    if (numberPages > 5e10) {
        $("<li class=\"page-item disabled\"><a class=\"page-link\" href=\"#\" ><span aria-hidden=\"true\">...</span></a></li>")
            .insertAfter(pagination.find("li").eq(4));
    }

    numbers.unbind();
    numbers.click(function (event) {
            event.preventDefault();

            var page = parseInt($(this).data("pagination"));

            currentItemsStart = (page - 1) * 10;
            rows.hide().slice(currentItemsStart, currentItemsStart + recPerPage).show();
        });

    $("[data-pagination=\"next\"]").unbind();
    $("[data-pagination=\"next\"]").click(function (event) {
        event.preventDefault();

        if (!$(this).hasClass("disabled")) {
            currentItemsStart += 10;
            rows.hide().slice(currentItemsStart, currentItemsStart + recPerPage).show();
        }
    });

    $("[data-pagination=\"last\"]").unbind();
    $("[data-pagination=\"last\"]").click(function (event) {
        event.preventDefault();

        if (!$(this).hasClass("disabled")) {
            currentItemsStart = Math.floor(totalRecords / recPerPage) * recPerPage;
            rows.hide().slice(currentItemsStart, currentItemsStart + recPerPage).show();
        }
    });

    $("[data-pagination=\"first\"]").unbind();
    $("[data-pagination=\"first\"]").click(function (event) {
        event.preventDefault();

        if (!$(this).hasClass("disabled")) {
            currentItemsStart = 0;
            rows.hide().slice(currentItemsStart, currentItemsStart + recPerPage).show();
        }
    });

    $("[data-pagination=\"previous\"]").unbind();
    $("[data-pagination=\"previous\"]").click(function (event) {
        event.preventDefault();

        if (!$(this).hasClass("disabled")) {
            currentItemsStart -= 10;
            rows.hide().slice(currentItemsStart, currentItemsStart + recPerPage).show();
        }
    });

    pagination.unbind();
    pagination.click(function () {
        if (totalRecords >= 0 & totalRecords <= recPerPage) {
            $("[data-pagination=\"next\"]").addClass("disabled");
            $("[data-pagination=\"previous\"]").addClass("disabled");
            $("[data-pagination=\"last\"]").addClass("disabled");
            $("[data-pagination=\"first\"]").addClass("disabled");
        } else if (currentItemsStart >= totalRecords - recPerPage & totalRecords > recPerPage) {
            $("[data-pagination=\"next\"]").addClass("disabled");
            $("[data-pagination=\"previous\"]").removeClass("disabled");
            $("[data-pagination=\"last\"]").addClass("disabled");
            $("[data-pagination=\"first\"]").removeClass("disabled");
        } else if (currentItemsStart <= 0) {
            $("[data-pagination=\"next\"]").removeClass("disabled");
            $("[data-pagination=\"previous\"]").addClass("disabled");
            $("[data-pagination=\"last\"]").removeClass("disabled");
            $("[data-pagination=\"first\"]").addClass("disabled");
        } else {
            $("[data-pagination=\"next\"]").removeClass("disabled");
            $("[data-pagination=\"previous\"]").removeClass("disabled");
            $("[data-pagination=\"last\"]").removeClass("disabled");
            $("[data-pagination=\"first\"]").removeClass("disabled");
        }

        currentPageStart = (currentItemsStart / recPerPage) + 1;

        if (currentPageStart > 3) {
            $("li[data-pagination=\"dotsFirst\"]").show();
        }
        else {
            $("li[data-pagination=\"dotsFirst\"]").hide();
        }

        if (currentPageStart < numberPages - 2) {
            $("li[data-pagination=\"dotsLast\"]").show();
        }
        else {
            $("li[data-pagination=\"dotsLast\"]").hide();
        }

        numbers.each(function () {
            if (currentPageStart === 1) {
                if ($(this).data("pagination") <= 4) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            } else if (currentPageStart === 2) {
                if ($(this).data("pagination") <= 4 | $(this).data("pagination") == 1) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            } else if (currentPageStart === 3) {
                if ($(this).data("pagination") <= 4 | $(this).data("pagination") < 3) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            } else if (currentPageStart === numberPages) {
                if ($(this).data("pagination") > numberPages - 4) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            } else if (currentPageStart === numberPages - 1) {
                if ($(this).data("pagination") > numberPages - 4 | $(this).data("pagination") == numberPages) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            } else if (currentPageStart === numberPages - 2) {
                if ($(this).data("pagination") > numberPages - 4 | $(this).data("pagination") > numberPages - 3) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            } else {
                if ($(this).data("pagination") >= currentPageStart - 1 & $(this).data("pagination") <= currentPageStart + 1) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            }
        });

        $("li").removeClass("active");
        $("[data-pagination=" + String((currentItemsStart / recPerPage) + 1) + "]").addClass("active");       

        pagination.focus();
    });
}

$(".searchFilter").on("keyup", function () {
    var value = $(this).val().toLowerCase().trim();
    $(items).filter(function () {
        $(this).toggle($(this).text().toLowerCase().trim().indexOf(value) > -1)
    });
    updatePagination();
});

$("table > thead th > i.fa-sort-alpha-up").click(function () {
    sortTable($('table'), "asc", $(this).parent().prevAll().length);
});
$("table > thead th > i.fa-sort-alpha-down").click(function () {
    sortTable($('table'), "desc", $(this).parent().prevAll().length);
});

function sortTable(table, order, column) {
    var asc = order === 'asc',
        tbody = table.find('tbody');

    tbody.find('tr').sort(function (a, b) {
        if (asc) {
            return $("td:eq(" + column + ")", a).text().localeCompare($("td:eq(" + column + ")", b).text());
        } else {
            return $("td:eq(" + column + ")", b).text().localeCompare($("td:eq(" + column + ")", a).text());
        }
    }).appendTo(tbody);

    $(items).show();
    updatePagination();
}

$(document).ready(function () {
    $("table > thead th > i").parent().data("order", "asc");
    updatePagination();
});
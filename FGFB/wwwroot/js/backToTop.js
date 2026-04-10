!function ($) {
    "use strict";

    $(".switch").on("click", function () {
        if ($("body").hasClass("light")) {
            $("body").removeClass("light");
            $(".switch").removeClass("switched");
        } else {
            $("body").addClass("light");
            $(".switch").addClass("switched");
        }
    });

    $(document).ready(function () {
        var progressPath = document.querySelector(".progress-wrap path");
        var progressWrap = document.querySelector(".progress-wrap");

        if (!progressPath || !progressWrap) {
            return;
        }

        var pathLength = progressPath.getTotalLength();

        progressPath.style.transition = progressPath.style.WebkitTransition = "none";
        progressPath.style.strokeDasharray = pathLength + " " + pathLength;
        progressPath.style.strokeDashoffset = pathLength;
        progressPath.getBoundingClientRect();
        progressPath.style.transition = progressPath.style.WebkitTransition = "stroke-dashoffset 10ms linear";

        var updateProgress = function () {
            var scroll = $(window).scrollTop();
            var height = $(document).height() - $(window).height();

            if (height <= 0) {
                progressPath.style.strokeDashoffset = pathLength;
                return;
            }

            var progress = pathLength - (scroll * pathLength / height);
            progressPath.style.strokeDashoffset = progress;
        };

        updateProgress();
        $(window).on("scroll", updateProgress);

        $(window).on("scroll", function () {
            if ($(this).scrollTop() > 50) {
                $(".progress-wrap").addClass("active-progress");
            } else {
                $(".progress-wrap").removeClass("active-progress");
            }
        });

        $(".progress-wrap").on("click", function (e) {
            e.preventDefault();
            $("html, body").animate({ scrollTop: 0 }, 550);
            return false;
        });
    });

}(jQuery);
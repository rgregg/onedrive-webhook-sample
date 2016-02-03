$(document).ready(function () {
    // Load stats from /api/stats end point

    $.ajax("/api/stats", {
        success: function (data, status, xhr) {
            $("#itemsInCameraRoll").text(data.itemCount);
            var sizeInGB = Math.round(1000 * data.totalSize / (1024 * 1024 * 1024)) / 1000;
            $("#sizeOfCameraRoll").text( sizeInGB + " GiB");
            $("#lastModifiedCameraRoll").text(data.lastModified);
        },
        error: function (xhr, textStatus, errorThrown) {
            $("#itemsInCameraRoll").text("Error");
            $("#sizeOfCameraRoll").text("Error");
            $("#lastModifiedCameraRoll").text("Error");
        }
    });

    refreshQueueDepth();

});

function refreshQueueDepth() {
    $.ajax("/api/queuedepth", {
        success: function (data, status, xhr) {
            $("#processingQueueDepth").text(data.depth);
        },
        error: function (xhr, textStatus, errorThrown) {
            $("#processingQueueDepth").text("Error");
        }
    })
}

function queueTestWebhook() {
    $.ajax("api/action/testhook", {
        success: function (data, status, xhr) {
            refreshQueueDepth();
        }
    });
}

/**
 * Text Field Plugin
 *
 * Adds basic demonstration functionality to .ms-TextField components.
 *
 * @param  {jQuery Object}  One or more .ms-TextField components
 * @return {jQuery Object}  The same components (allows for chaining)
 */
(function ($) {
    $.fn.TextField = function () {

        /** Iterate through each text field provided. */
        return this.each(function () {

            /** Does it have a placeholder? */
            if ($(this).hasClass("ms-TextField--placeholder")) {

                /** Hide the label on click. */
                $(this).on('click', function () {
                    $(this).find('.ms-Label').hide();
                });

                /** Show the label again when leaving the field. */
                $(this).find('.ms-TextField-field').on('blur', function () {

                    /** Only do this if no text was entered. */
                    if ($(this).val().length === 0) {
                        $(this).siblings('.ms-Label').show();
                    }
                });
            };

            /** Underlined - adding/removing a focus class */
            if ($(this).hasClass('ms-TextField--underlined')) {

                /** Add is-active class - changes border color to theme primary */
                $(this).find('.ms-TextField-field').on('focus', function () {
                    $(this).parent('.ms-TextField--underlined').addClass('is-active');
                });

                /** Remove is-active on blur of textfield */
                $(this).find('.ms-TextField-field').on('blur', function () {
                    $(this).parent('.ms-TextField--underlined').removeClass('is-active');
                });
            };

        });
    };
})(jQuery);
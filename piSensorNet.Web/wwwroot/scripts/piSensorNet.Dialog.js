(function(root, dialog, $) {
    dialog.footerClass = 'ui-dialog-footer';
    dialog.dialogSelector = 'div#dialog';
    dialog.dialogParentSelector = '[role="dialog"]';
    dialog.footerSourceSelector = '[data-target="footer"]';
    dialog.formSubmitSelector = '[data-action="form.submit"]';
    dialog.dialogCloseSelector = '[data-action="dialog.close"]';
    dialog.disableOnSubmitSelector = '[data-disable="submit"]';
    dialog.defaultNotyTimeout = 5000;

    var editor = function(successCallback) {
        return function(responseText, textStatus, jqXHR) {
            var $dialog = $(this);
            var $footer = $('<div />').addClass(dialog.footerClass);
            var $dialogFooter = $dialog.find(dialog.footerSourceSelector);

            $dialogFooter.detach();
            $footer.append($dialogFooter.html());

            var $dialogParent = $dialog.parent();

            $dialogParent.append($footer);
            $dialog.height($dialog.height() - $footer.height());

            var $form = $dialog.find('form');

            $.validator.unobtrusive.parse($form);

            $dialogParent.find(dialog.formSubmitSelector)
                .on('click.ActionFormSubmit',
                    function() {
                        $form.submit();
                    });

            $dialogParent.find(dialog.dialogCloseSelector)
                .on('click.ActionFormSubmit',
                    function() {
                        $dialog.dialog('close');
                    });

            $form.on('submit',
                function(event) {
                    event.preventDefault();

                    var _$form = $(this);
                    if (!_$form.valid())
                        return;

                    var $wholeDialog = $(dialog.dialogSelector).closest(dialog.dialogParentSelector);

                    $wholeDialog.loading();
                    $wholeDialog.find(dialog.disableOnSubmitSelector)
                        .prop('disabled', true);

                    $.ajax({
                        'url': _$form.attr('action'),
                        'contentType': 'application/json',
                        'dataType ': 'json',
                        'type': 'POST',
                        'cache': false,
                        'processData': false,
                        'data': JSON.stringify(_$form.serializeObject()),
                        'success': function(_result, _textStatus, _jqXHR) {
                            var _$dialog = $(dialog.dialogSelector);
                            var _$wholeDialog = _$dialog.closest(dialog.dialogParentSelector);

                            _$wholeDialog.loading('stop');
                            _$wholeDialog.find(dialog.disableOnSubmitSelector)
                                .prop('disabled', false);

                            if (!_result.success) {
                                var errors = _result.data;

                                console.log(errors);

                                _$form.applyModelState(errors);

                                return;
                            }

                            _$dialog.dialog('close');
                            
                            $.isFunction(successCallback) && successCallback.call(this);

                            noty({
                                'type': 'information',
                                'text': _result.data,
                                'timeout': dialog.defaultNotyTimeout
                            });
                        },
                        'error': function(jqXHR, textStatus, errorThrown) {
                            var _$dialog = $(dialog.dialogSelector);
                            var _$wholeDialog = _$dialog.closest(dialog.dialogParentSelector);

                            _$wholeDialog.loading('stop');
                            _$dialog.dialog('close');

                            noty({
                                'type': 'error',
                                'text': textStatus,
                                'timeout': false
                            });

                            debugger;
                        }
                    });
                });

            $dialog.dialog('open');
        }
    }

    dialog.editor = function(url, title, successCallback) {
        $(dialog.dialogSelector)
            .load(url, editor(successCallback))
            .dialog({
                'title': title,
                'dialogClass': 'no-close',
                'autoOpen': false,
                'modal': true,
                'width': $(window).width() * 0.95,
                'height': $(window).height() * 0.95,
                'draggable': false,
                'resizable': false,
                'closeOnEscape': false,
                'close': function() {
                    var $dialog = $(this);

                    $dialog.parent().find('.ui-dialog-footer').remove();
                    $dialog.empty();
                }
            });
    }
}(window.piSensorNet, window.piSensorNet.Dialog = window.piSensorNet.Dialog || {}, jQuery));
(function (root, dataTables, $) {
    dataTables.dateFormatter = function (format) {
        format = format || root.DateTimeFormat;

        return function(data, type, row, meta) {
            return moment(data).format(format);
        };
    }

    dataTables.localizer = function(prefix, property) {
        return function (data, type, row, meta) {
            if (!!property)
                data = data[property];

            return root.Resources.localize(prefix + '_' + data);
        };
    }

    dataTables.enumLocalizer = function(prefix) {
        return dataTables.localizer(prefix, 'name');
    }

    dataTables.actions = function (actions, properties) {
        var actionsGeneratorFunction = function (data, type, row, meta) {
            var actionKeys = Object.keys(actions);
            var allActions = [];
            var oSettings = meta.settings;
            
            for (var keyIndex = 0, actionKeysLength = actionKeys.length; keyIndex < actionKeysLength; ++keyIndex) {
                var actionKey = actionKeys[keyIndex];
                var actionData = actions[actionKey];

                if (!actionData['text'])
                    throw 'action["' + actionKey + '"] must contain non-null property "text"';

                var hasIsVisible = actionData.hasOwnProperty('isVisible');
                var isVisible = hasIsVisible
                    ? ($.isFunction(actionData['isVisible']) ? actionData['isVisible'](row) : actionData['isVisible'])
                    : true;
                var title = $.isFunction(actionData['title']) ? actionData['title'](row) : actionData['title'];
                var text = !!actionData['text'].jquery
                    ? actionData['text']
                    : ($.isFunction(actionData['text']) ? actionData['text'](row) : actionData['text']);
                var href = $.isFunction(actionData['href']) ? actionData['href'](row) : actionData['href'];

                if (!isVisible)
                    continue;

                var tagTitle = root.htmlDecode(title) || text;
                var tagHref = href || '#';

                var a = $('<a />')
                    .addClass(actionKey + 'Action')
                    .addClass('dataTables_action')
                    .attr('data-action', actionKey)
                    .attr('href', tagHref)
                    .attr('title', tagTitle)
                    .html(text);
                
                allActions.push(a.outerHtml());

                var column = oSettings.aoColumns[meta.col];
                if (column.nTf != null) {
                    var $footer = $(column.nTf);
                    $footer.find('a[data-action="' + actionKey + '"]')
                        .each(function () {
                            var $actionElement = $(this);
                            var actionKey = $actionElement.attr('data-action');

                            $actionElement.addClass(actionKey + 'Action')
                                .addClass('dataTables_action');
                                
                            $actionElement.attr('data-action-applied', true);
                            $actionElement.off('click.ApplyActions')
                                .on('click.ApplyActions',
                                    function () {
                                        var $clicked = $(this);
                                        var $th = $clicked.closest('th');
                                        var columnIndex = $th.attr('data-columnIndex');
                                        var api = oSettings.oInstance.api();
                                        var columnData = oSettings.aoColumns[columnIndex];
                                        var actionType = $clicked.attr('data-action');
                                        var actionHandler = columnData.handlers[actionType];

                                        if (!actionHandler(oSettings, -1, columnIndex, null))
                                            return false;

                                        api.draw();

                                        return false;
                                    });

                            if ($actionElement.attr('data-action-text') === 'true')
                                $actionElement.html(text);

                            if ($actionElement.attr('data-action-title') === 'true')
                                $actionElement.attr('title', tagTitle);

                            if ($actionElement.attr('data-action-href') === 'true')
                                $actionElement.attr('href', tagHref);
                        });
                }
            }

            return $('<div />').html(allActions.join('')).outerHtml();
        }

        var obj = {
            'data': null,
            'name': '__Actions',
            'class': 'dataTables_actions',
            'render': actionsGeneratorFunction,
            'sortable': false,
            'type': 'actions',
            'handlers': Object.keys(actions).reduce(function (result, item) {
                var handler = actions[item].handler;
                if (!!handler)
                    result[item] = handler;

                return result;
            }, {})
        };

        if (!!properties)
            $.extend(obj, properties);

        return obj;
    }
}(window.piSensorNet, window.piSensorNet.DataTables = window.piSensorNet.DataTables || {}, jQuery));
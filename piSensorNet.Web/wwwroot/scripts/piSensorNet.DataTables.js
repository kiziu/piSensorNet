(function(root, dataTables, $) {
    var getData = function(object, propertyName, context, argumentsArray, useJQuery) {
        var value = object[propertyName];

        if (useJQuery && value.jquery)
            return value;

        if ($.isFunction(value))
            return value.apply(context, argumentsArray);

        return value;
    }

    dataTables.dateFormatter = function(format) {
        format = format || root.DateTimeFormat;
        
        return function(data, type, row, meta) {
            return moment(data).format(format);
        };
    }

    dataTables.localizer = function(prefix, property) {
        return function(data, type, row, meta) {
            if (!!property)
                data = data[property];

            return root.Resources.localize(prefix + '_' + data);
        };
    }

    dataTables.enumLocalizer = function(prefix) {
        return dataTables.localizer(prefix, 'name');
    }

    dataTables.actionHandlers = function(settings) {
        var column = settings.aoColumns.filter(function(e) { return e.type === 'actions'; })[0] || null;
        if (!column)
            return;

        var $body = $(settings.nTBody);
        var $footer = $(column.nTf);
        var tableData = settings.aoData.select('_aData');

        var actions = column.init;
        var actionKeys = Object.keys(actions);
        for (var k = 0, kMax = actionKeys.length; k < kMax; ++k) {
            var actionKey = actionKeys[k];

            if (!$.isFunction(actions[actionKey].handler))
                throw 'aoColumns[type="actions"].handlers[' + actionKey + '] is not a function';

            $body.find('a[data-action="' + actionKey + '"]')
                .each(function() {
                    var $actionElement = $(this);

                    $actionElement.attr('data-action-applied', true);
                    $actionElement.off('click.ApplyActions')
                        .on('click.ApplyActions',
                            function() {
                                var clicked = this;
                                var $clicked = $(clicked);
                                var $td = $clicked.closest('td');
                                var rowIndex = $td.attr('data-rowIndex');
                                var columnIndex = $td.attr('data-columnIndex');
                                var api = settings.oInstance.api();
                                var _column = settings.aoColumns[columnIndex];
                                var _actionKey = $clicked.attr('data-action');
                                var actionHandler = _column.init[_actionKey].handler;
                                var data = api.row(rowIndex).data();

                                if (!actionHandler.call(clicked, settings, rowIndex, columnIndex, data))
                                    return false;

                                api.draw();

                                return false;
                            });
                });

            if (!column.nTf)
                continue;

            var actionData = actions[actionKey];

            var actionElements = $footer.find('a[data-action="' + actionKey + '"]');
            for (var i = 0, iMax = actionElements.length; i < iMax; ++i) {
                var actionElement = actionElements[i];
                var $actionElement = $(actionElement);
                var _actionKey = $actionElement.attr('data-action');

                var hasVisible = actionData.hasOwnProperty('visible');
                var args = [settings, -1, column.idx, tableData];

                var visible = hasVisible ? getData(actionData, 'visible', actionElement, args) : true;
                if (!visible) {
                    $actionElement.css('display', 'none');
                    return;
                }

                $actionElement.addClass(_actionKey + 'Action')
                    .addClass('dataTables_action')
                    .css('display', '');

                $actionElement.attr('data-action-applied', true);
                $actionElement.off('click.ApplyActions')
                    .on('click.ApplyActions',
                        function() {
                            var $clicked = $(this);
                            var $th = $clicked.closest('th');
                            var columnIndex = $th.attr('data-columnIndex');
                            var api = settings.oInstance.api();
                            var _column = settings.aoColumns[columnIndex];
                            var _actionKey = $clicked.attr('data-action');
                            var actionHandler = _column.init[_actionKey].handler;
                            var data = settings.aoData.select('_aData');

                            if (!actionHandler.call(this, settings, -1, columnIndex, data))
                                return false;

                            api.draw();

                            return false;
                        });

                var title = getData(actionData, 'title', actionElement, args);
                var text = getData(actionData, 'text', actionElement, args, true);
                var href = getData(actionData, 'href', actionElement, args);

                var tagTitle = piSensorNet.htmlDecode(title) || text;
                var tagHref = href || '#';

                if ($actionElement.attr('data-action-text') === 'true')
                    $actionElement.html(text);

                if ($actionElement.attr('data-action-title') === 'true')
                    $actionElement.attr('title', tagTitle);

                if ($actionElement.attr('data-action-href') === 'true')
                    $actionElement.attr('href', tagHref);
            }
        }
    }

dataTables.actions = function(init, properties) {
    var actionsGeneratorFunction = function(data, type, row, meta) {
        var actionKeys = Object.keys(init);
        var allActions = [];

        for (var keyIndex = 0, actionKeysLength = actionKeys.length; keyIndex < actionKeysLength; ++keyIndex) {
            var actionKey = actionKeys[keyIndex];
            var actionData = init[actionKey];

            if (!actionData['text'])
                throw 'action["' + actionKey + '"] must contain non-null property "text"';

            var hasVisible = actionData.hasOwnProperty('visible');
            var context = meta.settings.oInstance;
            var args = [meta.settings, meta.row, meta.col, row];

            var visible = hasVisible ? getData(actionData, 'visible', context, args) : true;
            if (!visible)
                continue;

            var title = getData(actionData, 'title', context, args);
            var text = getData(actionData, 'text', context, args, true);
            var href = getData(actionData, 'href', context, args);

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
        'init': init
    };

    if (!!properties)
        $.extend(obj, properties);

    return obj;
}
}
(window.piSensorNet, window.piSensorNet.DataTables = window.piSensorNet.DataTables || {}, jQuery));
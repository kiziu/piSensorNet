$.extend($.noty.defaults,
{
    'layout': 'topCenter',
    'theme': 'defaultTheme'
});


$.Loading.defaults.message = '';
$.Loading.defaults.zIndex = 10010;
$.Loading.defaults.onStop = function (loading) {
    loading.overlay.fadeOut(150);
    loading.overlay.remove();
    loading.element.removeData('jqueryLoading');

    delete loading;
};


$.extend($.fn.dataTable.defaults,
{
    'processing': true,
    'serverSide': true,
    'serverMethod': 'POST',
    'ajax': {
        'dataSrc': function (json) {
            var data = json['data'];

            $.extend(json, data);

            return data['items'];
        }
    },

    'displayLength': 25,

    'autoWidth': false,

    'dom': $.fn.dataTable.defaults.dom.replace('lfr', 'l%^r') + '$'
});

$.extend($.fn.DataTable.models.oSettings,
{
    'aoDrawCallback': [
        {
            'fn': function (oSettings) {
                var columns = oSettings.aoColumns;
                var i, length, j, length2;
                for (i = 0, length = columns.length; i < length; ++i) {
                    var column = columns[i];
                    var index = column.idx;

                    if (!!column.nTh)
                        $(column.nTh).attr('data-columnIndex', index);

                    if (!!column.nTf)
                        $(column.nTf).attr('data-columnIndex', index);
                }

                var dataRows = oSettings.aoData;

                for (i = 0, length = dataRows.length; i < length; ++i) {
                    var dataRow = dataRows[i];

                    if (!!dataRow.anCells)
                        for (j = 0, length2 = dataRow.anCells.length; j < length2; ++j) {
                            var cell = dataRow.anCells[j];
                            if (!cell)
                                continue;

                            $(cell)
                                .attr('data-rowIndex', i)
                                .attr('data-columnIndex', j);
                        }
                }
            },
            'sName': 'MarkCells'
        },
        {
            'fn': function (oSettings) {
                var columns = oSettings.aoColumns;
                var $body = $(oSettings.nTBody);
                for (var i = 0, iMax = columns.length; i < iMax; ++i) {
                    var column = columns[i];
                    if (column.type !== 'actions')
                        continue;

                    var handlers = column.handlers;
                    var actions = Object.keys(handlers);
                    for (var k = 0, kMax = actions.length; k < kMax; ++k) {
                        var action = actions[k];

                        if (!$.isFunction(handlers[action]))
                            throw 'aoColumns[' + i + '].handlers[' + action + '] is not a function';

                        $body.find('a[data-action="' + action + '"]')
                            .each(function () {
                                var $actionElement = $(this);

                                $actionElement.attr('data-action-applied', true);
                                $actionElement.off('click.ApplyActions')
                                    .on('click.ApplyActions',
                                        function () {
                                            var $clicked = $(this);
                                            var $td = $clicked.closest('td');
                                            var rowIndex = $td.attr('data-rowIndex');
                                            var columnIndex = $td.attr('data-columnIndex');
                                            var api = oSettings.oInstance.api();
                                            var columnData = oSettings.aoColumns[columnIndex];
                                            var actionType = $clicked.attr('data-action');
                                            var actionHandler = columnData.handlers[actionType];
                                            var data = api.row(rowIndex).data();

                                            if (!actionHandler(oSettings, rowIndex, columnIndex, data))
                                                return false;

                                            api.draw();

                                            return false;
                                        });
                            });
                    }
                }
            },
            'sName': 'ApplyActions',
            'bMovetoEnd': true
        }
    ],

    'aoRowCallback': [
        {
            'fn': function (nRow, oData, iRow, iDataRow) {
                $(nRow).attr('data-rowIndex', iRow);
            },
            'sName': 'MarkRows'
        }
    ]
});

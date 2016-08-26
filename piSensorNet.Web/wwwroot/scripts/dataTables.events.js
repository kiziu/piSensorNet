$.extend($.fn.DataTable.models.oSettings,
{
    'aoDrawCallback': [
        {
            'fn': function MarkCells(oSettings) {
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
            'fn': piSensorNet.DataTables.actionHandlers,
            'sName': 'ApplyActions',
            'bMovetoEnd': true
        }
    ],

    'aoRowCallback': [
        {
            'fn': function MarkRows(nRow, oData, iRow, iDataRow) {
                $(nRow).attr('data-rowIndex', iRow);
            },
            'sName': 'MarkRows'
        }
    ]
});
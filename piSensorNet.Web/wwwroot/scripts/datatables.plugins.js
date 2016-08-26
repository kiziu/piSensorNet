(function($) {
    // for cFeature, do not use any of the below nor B R S
    // ^ $ % *

    $.fn.dataTableExt.aoFeatures.push({
        'fnInit': function Refresh(oSettings) {
            if (oSettings.oInit.hasOwnProperty('bServerSide') && !oSettings.oInit.bServerSide)
                return null;

            var icon = $('<span />')
                .addClass('glyphicon glyphicon-refresh');

            var wrapper = $('<div />')
                .addClass('dataTables_refresh')
                .append(icon)
                .attr('title', oSettings.oLanguage.sRefresh);

            wrapper.on('click.refresh',
                function() {
                    oSettings.oInstance.api().ajax.reload();
                });

            return wrapper;
        },
        'cFeature': '^',
        'sFeature': 'Refresh'
    });

    $.fn.dataTableExt.aoFeatures.push({
        'fnInit': function ReorderDrawCallbacks(oSettings) {
            var callbacks = oSettings.aoDrawCallback;

            var toEnd = callbacks.filter(function(i) { return !!i.bMoveToEnd; });
            callbacks = callbacks.filter(function(i) { return !i.bMoveToEnd; }).concat(toEnd).reverse();

            oSettings.aoDrawCallback = callbacks;

            return null;
        },
        'cFeature': '$',
        'sFeature': 'ReorderDrawCallbacks'
    });

    $.fn.dataTableExt.aoFeatures.push({
        'fnInit': function Title(oSettings) {
            if (!oSettings.oInit.title)
                return null;

            var title = oSettings.oInit.title;

            title = !title.jQuery ? $('<div />').text(title) : title;

            title.addClass('dataTables_title');

            return title;
        },
        'cFeature': '%',
        'sFeature': 'Title'
    });
})(jQuery);
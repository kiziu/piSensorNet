$.extend($.noty.defaults,
{
    'layout': 'topCenter',
    'theme': 'defaultTheme'
});

$.Loading.defaults.message = '';
$.Loading.defaults.zIndex = 10010;
$.Loading.defaults.onStop = function(loading) {
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
        'dataSrc': function(json) {
            var data = json['data'];

            $.extend(json, data);

            return data['items'];
        }
    },

    'displayLength': 25,

    'autoWidth': false,

    'dom': $.fn.dataTable.defaults.dom.replace('lfr', 'l%^r') + '$'
});
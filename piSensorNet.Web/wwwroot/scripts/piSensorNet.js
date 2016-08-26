(function(root, $) {
    var _divConverter = $('<div />');

    root.htmlDecode = function(sInput) {
        return _divConverter.html(sInput).text();
    }

    root.htmlEncode = function(sInput) {
        if (!sInput)
            return null;

        return _divConverter.text(sInput).html();
    }
    
    Object.defineProperty(root,
        'ResourcesPath',
        {
            set: common.createSetterException('piSensorNet.ResourcesPath'),
            get: function () {
                return 'Resources.Manual';
            }
        });

    Object.defineProperty(root,
        'DateTimeFormat',
        {
            set: common.createSetterException('piSensorNet.DateTimeFormat'),
            get: function () {
                return 'D/MM/YYYY, HH:mm:ss';
            }
        });
}(window.piSensorNet = window.piSensorNet || {}, jQuery));
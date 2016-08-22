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
    
    root.createSetterException = function(propertyName) {
        return function() {
            throw 'setting the value of ' + propertyName + ' is not permitted';
        };
    }
    
    Object.defineProperty(root,
        'ResourcesPath',
        {
            set: root.createSetterException('piSensorNet.ResourcesPath'),
            get: function () {
                return 'Resources.Manual';
            }
        });

    Object.defineProperty(root,
        'DateTimeFormat',
        {
            set: root.createSetterException('piSensorNet.DateTimeFormat'),
            get: function () {
                return 'D/MM/YYYY, HH:mm:ss';
            }
        });
}(window.piSensorNet = window.piSensorNet || {}, jQuery));
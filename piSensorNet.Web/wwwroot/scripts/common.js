(function(common) {
    common.eachPair = function(oDictionary, callback) {
        var keys = Object.keys(oDictionary);

        for (var i = 0, iMax = keys.length; i < iMax; ++i) {
            var key = keys[i];
            var value = oDictionary[key];

            callback(key, value);
        }
    }

    common.argsToArray = function(oArguments) {
        var array = [];

        for (var i = 0, iMax = oArguments.length; i < iMax; ++i) {
            var value = oArguments[i];

            array.push(value);
        }

        return array;
    }
}(window.common = window.common || {}));
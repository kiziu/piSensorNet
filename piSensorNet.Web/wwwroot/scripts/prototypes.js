(function() {
    Object.defineProperty(Array.prototype,
        'select',
        {
            enumerable: false,
            value: function() {
                var length = arguments.length;
                var args = arguments;

                if (length === 1)
                    return this.map(function(e) { return e[args[0]]; });
                else if (length > 1)
                    return this.map(function(e) {
                        var o = {};
                        for (var i = 0; i < length; ++i)
                            o[args[i]] = e[args[i]];

                        return o;
                    });
                else
                    throw 'At least one property must be specified';
            }
        });
})();
(function (root, net, $) {
    var _nodeSelector = '[data-require="hubConnection"]';
    var _connection = null;
    var _onConnectedCallbacks = [];
    var _connectionID = null;
    var _hub = null;

    Object.defineProperty(net,
        'connectionID',
        {
            'set': root.createSetterException('piSensorNet.Net.connectionID'),
            'get': function() {
                return _connectionID;
            }
        });

    Object.defineProperty(net,
        'hub',
        {
            'set': root.createSetterException('piSensorNet.Net.hub'),
            'get': function() {
                return _hub;
            }
        });

    net.initHub = function ($connection) {
        _connection = $connection;

        net.changeNodesState(false);

        _hub = _connection.mainHub;

        _hub.logging = true;
        
        net.on('Error',
            function (message) {
                window.noty({
                    'text': message,
                    'type': 'error',
                    'modal': true
                });
            });

        return _hub;
    }

    net.connectHub = function () {
        _connection.hub.start({ transport: ['webSockets', 'serverSentEvents', 'longPolling'] })
            .done(function () {
                _connectionID = _connection.hub.id;
                net.changeNodesState(true);

                console.log('connected, ID: ' + _connectionID + '.');

                _onConnectedCallbacks.forEach(function (callback) { callback.apply(_connectionID); });
                _onConnectedCallbacks = [];
            });
    };

    net.onConnected = function (callback) {
        _onConnectedCallbacks.push(callback);
    }

    net.changeNodesState = function (bState) {
        var sState = bState ? null : 'disabled';

        $(_nodeSelector).prop('disabled', sState);
    };


    net.on = function (name, handler) {
        name = 'on' + name;

        console.log('client[' + name + ']: handler bound');

        _hub.client[name] = handler;
    };

    net.handle = function (oNode, functionName, args, callback) {
        if (oNode)
            oNode.disabled = true;

        console.log(functionName + ': calling...');
        _hub.server[functionName].apply(this, args)
            .done(function (result) {
                console.log(functionName + ': done');
                if (result != undefined)
                    console.log(result);

                $.isFunction(callback) && callback.call(this, result);

                if (oNode)
                    oNode.disabled = false;
            });
    };

    net.call = function (functionName, args, callback) {
        net.handle(null, functionName, args, callback);
    };
}(window.piSensorNet, window.piSensorNet.Net = window.piSensorNet.Net || {}, jQuery));
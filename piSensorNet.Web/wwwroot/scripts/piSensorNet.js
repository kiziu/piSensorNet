(function (piSensorNet, $) {
    var nodeSelector = '[data-require="hubConnection"]';
    var connection = null;
    var onConnectedCallbacks = [];

    piSensorNet.hubConnectionID = null;
    piSensorNet.hub = null;

    piSensorNet.initHub = function($connection) {
        connection = $connection;

        piSensorNet.changeNodesState(false);

        var hub = connection.mainHub;

        hub.logging = true;

        piSensorNet.hub = hub;

        piSensorNet.on('error',
            function(message) {
                noty({
                    'text': message,
                    'type': 'error',
                    'modal': true
                });
            });

        return hub;
    }

    piSensorNet.connectHub = function () {
        connection.hub.start({ transport: ['webSockets', 'serverSentEvents', 'longPolling'] })
            .done(function () {
                piSensorNet.hubConnectionID = connection.hub.id;
                piSensorNet.changeNodesState(true);

                console.log('Connected, ID: ' + piSensorNet.hubConnectionID + '.');

                onConnectedCallbacks.forEach(function (callback) { callback.apply(piSensorNet.hubConnectionID); });
                onConnectedCallbacks = [];
            });
    };

    piSensorNet.onConnected = function(callback) {
        onConnectedCallbacks.push(callback);
    }

    piSensorNet.changeNodesState = function (bState) {
        var sState = bState ? null : 'disabled';

        $(nodeSelector).prop('disabled', sState);
    };


    piSensorNet.on = function (name, handler) {
        piSensorNet.hub.client[name] = handler;
    };

    piSensorNet.handle = function (oNode, functionName, args, callback) {
        if (oNode)
            oNode.disabled = true;

        console.log(functionName + ': calling...');
        piSensorNet.hub.server[functionName].apply(this, args)
            .done(function (result) {
                console.log(functionName + ': done');
                console.log(result);

                $.isFunction(callback) && callback.call(this, result);

                if (oNode)
                    oNode.disabled = false;
            });
    };

    piSensorNet.call = function (functionName, args, callback) {
        piSensorNet.handle(null, functionName, args, callback);
    };

}(window.piSensorNet = window.piSensorNet || {}, jQuery));
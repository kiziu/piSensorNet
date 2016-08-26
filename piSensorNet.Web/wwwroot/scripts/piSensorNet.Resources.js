(function(root, resources, $) {
    resources.localize = function(resourceKey) {
        if (!resourceKey)
            throw 'sResourceKey must be non-empty';

        var resourcesPathItems = root.ResourcesPath.split('.');
        var resourcesPath = root;
        var path = 'piSensorNet';

        if (resourcesPath == null)
            throw 'Property ' + path + ' does not exist';

        for (var i = 0, resourcesPathItemsLength = resourcesPathItems.length; i < resourcesPathItemsLength; ++i) {
            var resourcesPathItem = resourcesPathItems[i];

            path += '.' + resourcesPathItem;
            resourcesPath = resourcesPath[resourcesPathItem];

            if (resourcesPath == null)
                throw 'Property ' + path + ' does not exist';
        }

        var resourceExists = resourcesPath.hasOwnProperty(resourceKey);

        return resourceExists ? resourcesPath[resourceKey] : resourceKey;
    }
}(window.piSensorNet, window.piSensorNet.Resources = window.piSensorNet.Resources || {}, jQuery));
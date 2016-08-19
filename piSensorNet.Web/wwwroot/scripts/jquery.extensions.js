
jQuery.fn.extend({
    'outerHtml': function() {
        var values = this.map(function() {
            return this.nodeType === 1
                ? this.outerHTML
                : undefined;
        });

        return values.length > 1 ? values : values[0];
    },

    'serializeObject': function() {
        var self = this,
            json = {},
            push_counters = {},
            patterns = {
                "validate": /^[a-zA-Z][a-zA-Z0-9_]*(?:\[(?:\d*|[a-zA-Z0-9_]+)\])*$/,
                "key": /[a-zA-Z0-9_]+|(?=\[\])/g,
                "push": /^$/,
                "fixed": /^\d+$/,
                "named": /^[a-zA-Z0-9_]+$/
            };


        this.build = function(base, key, value) {
            base[key] = value;
            return base;
        };

        this.push_counter = function(key) {
            if (push_counters[key] === undefined) {
                push_counters[key] = 0;
            }
            return push_counters[key]++;
        };

        $.each($(this).serializeArray(),
            function() {

                // skip invalid keys
                if (!patterns.validate.test(this.name)) {
                    return;
                }

                var k,
                    keys = this.name.match(patterns.key),
                    merge = this.value,
                    reverse_key = this.name;

                while ((k = keys.pop()) !== undefined) {

                    // adjust reverse_key
                    reverse_key = reverse_key.replace(new RegExp("\\[" + k + "\\]$"), '');

                    // push
                    if (k.match(patterns.push)) {
                        merge = self.build([], self.push_counter(reverse_key), merge);
                    }
                    // fixed
                    else if (k.match(patterns.fixed)) {
                        merge = self.build([], k, merge);
                    }
                    // named
                    else if (k.match(patterns.named)) {
                        merge = self.build({}, k, merge);
                    }
                }

                json = $.extend(true, json, merge);
            });

        return json;
    },

    'applyModelState': function (modelState) {
        var $form = this;

        if ($form.length > 1)
            $form = $($form[0]);

        if (!$form.is('form'))
            throw '(this) must be form';

        var $validationSummary = $form.find('[data-valmsg-summary="true"] > ul');
        $validationSummary.empty();

        common.eachPair(modelState, function(key, value) {
            if (!!key) {
                var error = value.Errors.filter(function(i) { return !!i.ErrorMessage || (!!i.Exception && i.Exception.Message); })[0] || null;
                if (error == null)
                    return;

                var $field = $form.find('[name="' + key + '"]');
                var $errorContainer = $form.find('[data-valmsg-for="' + key + '"]');
                $errorContainer.empty();

                $field.removeClass('input-validation-valid').addClass('input-validation-error');

                $errorContainer.removeClass('field-validation-valid').addClass('field-validation-error');
                $errorContainer.append($('<span />').addClass(key + '-error').text(error.ErrorMessage || error.Exception));
            }

            value.Errors.forEach(function (i) {
                $validationSummary.append($('<li />').text(i.ErrorMessage || (!!i.Exception && i.Exception.Message)));
            });
        });
    }
});
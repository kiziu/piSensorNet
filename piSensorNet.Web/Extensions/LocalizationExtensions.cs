using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using JetBrains.Annotations;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;
using piSensorNet.Web.Resources;

namespace piSensorNet.Web.Extensions
{
    public static class LocalizationExtensions
    {
        private static readonly ResourceManager Manager = new ResourceManager(Reflector.Instance<Enums>.Type);
        private static readonly EnumLocalizationHelper Helper = new EnumLocalizationHelper(Manager);

        [NotNull]
        public static string Localize(this Enum value)
            => Helper.Localize(value);

        [NotNull]
        public static IReadOnlyDictionary<string, string> LocalizeAllKeyed<TEnum>()
            where TEnum : struct 
            => Helper.LocalizeAllKeyed<TEnum>(Reflector.Instance<Enums>.Name + "_");

        internal class EnumLocalizationHelper
        {
            /// <summary>
            /// 0 - enum type name without suffix Enum (if was present), 1 - enum value name
            /// </summary>
            private const string DefaultResourcePattern = "{0}_{1}";

            private readonly IDictionary<Type, IDictionary<int, string>> _enumCache =
                new Dictionary<Type, IDictionary<int, string>>();

            private readonly object _enumCacheLocker = new object();

            private readonly ResourceManager _manager;

            public EnumLocalizationHelper([NotNull]ResourceManager manager)
            {
                _manager = manager;
            }

            [Pure]
            [NotNull]
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string Localize([CanBeNull] Enum enumValue)
            {
                if (enumValue == null)
                    return String.Empty;

                var enumType = enumValue.GetRealType();

                EnsureEnumCached(enumType);

                var key = GetKey(enumType, enumValue);

                var resource = _manager.GetString(key);
                var localized = resource ?? key;

                return localized;
            }

            [Pure]
            [NotNull]
            private string GetKey([NotNull] Type enumType, int intValue)
            {
                var key = _enumCache[enumType][intValue];

                return key;
            }

            [Pure]
            [NotNull]
            private string GetKey([NotNull] Type enumType, [NotNull] Enum enumValue)
            {
                var intValue = Convert.ToInt32(enumValue);

                var key = GetKey(enumType, intValue);

                return key;
            }

            private void EnsureEnumCached([NotNull] Type enumType)
            {
                lock (_enumCacheLocker)
                {
                    if (_enumCache.ContainsKey(enumType))
                        return;

                    var enumName = Reflector.Instance<Enum>.Name;
                    var enumTypeName = enumType.Name.Replace(enumName, String.Empty);
                    var enumValues = Enum.GetValues(enumType);
                    var items = new Dictionary<int, string>(enumValues.Length);

                    foreach (var item in enumValues)
                    {
                        var intValue = Convert.ToInt32(item);
                        var key = DefaultResourcePattern.AsFormatFor(enumTypeName, item);

                        items.Add(intValue, key);
                    }

                    _enumCache.Add(enumType, items);
                }
            }

            [Pure]
            [NotNull]
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public IReadOnlyDictionary<string, string> LocalizeAllKeyed<TEnum>(string prefix)
                where TEnum : struct
            {
                var enumType = Reflector.Instance<TEnum>.Type;

                EnsureEnumCached(enumType);

                var result = new Dictionary<string, string>(_enumCache[enumType].Count);

                foreach (var value in _enumCache[enumType])
                {
                    var enumValue = value.Key;
                    var key = GetKey(enumType, enumValue);

                    var resource = _manager.GetString(key);
                    var localized = resource ?? key;

                    result.Add(prefix + key, localized);
                }

                return result;
            }
        }
    }
}

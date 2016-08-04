using System;
using System.Collections.Generic;
using System.Linq;

namespace piSensorNet.Logic.FunctionHandlers.Base
{
    internal static class FunctionHandlerHelper
    {
        public static IReadOnlyDictionary<string, string> SplitPairs(string packet, char keyDelimiter, char valueDelimiter)
            => SplitSingle(packet, keyDelimiter).Select(i => i.Split(valueDelimiter)
                                                .Where(ii => !string.IsNullOrEmpty(ii)).ToList())
                                  .ToDictionary(i => i[0], i => i[1]);

        public static IReadOnlyCollection<string> SplitSingle(string packet, char keyDelimiter)
            => packet.Split(keyDelimiter)
                     .Where(i => !string.IsNullOrEmpty(i))
                     .ToList();
    }
}

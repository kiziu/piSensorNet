using System;
using System.IO;
using System.Linq;

namespace piSensorNet.Common.System
{
    public static class Processess
    {
        public static int? FindByFragment(string nameFragment, StringComparison comparisonType = StringComparison.InvariantCulture, params string[] excludedNameFragments)
        {
            var allProcessess = Directory.EnumerateDirectories("/proc");
            foreach (var process in allProcessess)
            {
                int processID;
                if (!int.TryParse(process.Substring(6), out processID))
                    continue;
                
                var commandFile = process + "/cmdline";
                if (!File.Exists(commandFile))
                    continue;

                var command = File.ReadAllText(commandFile);
                if (excludedNameFragments.Any(i => command.IndexOf(i, comparisonType) >= 0))
                    continue;

                if (command.IndexOf(nameFragment, comparisonType) >= 0)
                    return processID;
            }

            return null;
        }
    }
}
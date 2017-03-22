using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace piSensorNet.Common.Enums
{
    public enum FunctionTypeEnum
    {
        Unknown = 0,
        Identify = 1,
        FunctionList = 2,
        Voltage = 3,
        Report = 4,
        OwList = 5,
        OwDS18B20Temperature = 6,
        OwDS18B20TemperaturePeriodical = 7,
    }

    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public static partial class EnumExtensions
    {
        public static string ToFunctionName(this FunctionTypeEnum functionType)
        {
            switch (functionType)
            {
                case FunctionTypeEnum.Identify:
                    return "identify";

                case FunctionTypeEnum.FunctionList:
                    return "function_list";

                case FunctionTypeEnum.Voltage:
                    return "voltage";

                case FunctionTypeEnum.Report:
                    return "report";

                case FunctionTypeEnum.OwList:
                    return "ow_list";

                case FunctionTypeEnum.OwDS18B20Temperature:
                    return "ow_ds18b20_temperature";

                case FunctionTypeEnum.OwDS18B20TemperaturePeriodical:
                    return "ow_ds18b20_temperature_periodical";

                case FunctionTypeEnum.Unknown:
                    return null;

                default:
                    throw new ArgumentOutOfRangeException(nameof(functionType), functionType, "Value is out of range.");
            }
        }
    }
}
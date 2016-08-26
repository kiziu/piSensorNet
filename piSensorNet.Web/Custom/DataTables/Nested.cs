using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Custom;

namespace piSensorNet.Web.Custom.DataTables
{
    public abstract class Nested<T> : Dictionary<string, T> {}

    public class Nested : Nested<Nested>
    {
        public Nested Invoke(params object[] _)
        {
            return this;
        }

        public static implicit operator JsonLiteral(Nested _)
        {
            return null;
        }

        public static implicit operator string(Nested _)
        {
            return null;
        }
    }
}
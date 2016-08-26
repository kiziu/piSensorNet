using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace piSensorNet.Logic.Custom
{
    public sealed class HubMessageQueue : Queue<Expression<Action<IMainHubEngine>>>
    {}
}

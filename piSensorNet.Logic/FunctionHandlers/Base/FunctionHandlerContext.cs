using System;
using System.Collections.Generic;
using piSensorNet.Common;
using piSensorNet.Common.Custom.Interfaces;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Context;
using piSensorNet.Logic.TriggerDependencyHandlers.Base;
using piSensorNet.Logic.Triggers;
using piSensorNet.Logic.TriggerSourceHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers.Base
{
    public class FunctionHandlerContext : TriggerSourceHandlerHelperContext
    {
        public FunctionHandlerContext(IpiSensorNetConfiguration moduleConfiguration, PiSensorNetDbContext databaseContext, IReadOnlyDictionary<FunctionTypeEnum, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyMap<FunctionTypeEnum, int> functionTypes, IReadOnlyMap<string, int> functionNames, IReadOnlyDictionary<TriggerSourceTypeEnum, ITriggerSourceHandler> triggerSourceHandlers, IReadOnlyDictionary<int, TriggerDelegate> triggerDelegates, IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> triggerDependencyHandlers, DateTime triggerDateTime)
            : base(databaseContext, triggerSourceHandlers, triggerDelegates, triggerDependencyHandlers, triggerDateTime)
        {
            ModuleConfiguration = moduleConfiguration;
            QueryableFunctionHandlers = queryableFunctionHandlers;
            FunctionTypes = functionTypes;
            FunctionNames = functionNames;
        }

        public IpiSensorNetConfiguration ModuleConfiguration { get; }

        public IReadOnlyDictionary<FunctionTypeEnum, IQueryableFunctionHandler> QueryableFunctionHandlers { get; }
        public IReadOnlyMap<FunctionTypeEnum, int> FunctionTypes { get; }
        public IReadOnlyMap<string, int> FunctionNames { get; }
        
        protected new IReadOnlyDictionary<TriggerSourceTypeEnum, ITriggerSourceHandler> TriggerSourceHandlers { get; set; }
        protected new IReadOnlyDictionary<int, TriggerDelegate> TriggerDelegates { get; set; }
        protected new IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> TriggerDependencyHandlers { get; set; }
        protected new DateTime TriggerDateTime { get; set; }
    }
}
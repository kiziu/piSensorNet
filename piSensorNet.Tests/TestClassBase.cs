using System;
using System.Data;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Context;

namespace piSensorNet.Tests
{
    public abstract class TestClassBase : IDisposable
    {
        protected readonly PiSensorNetDbContext Context;
        protected readonly DbContextTransaction Transaction;

        protected virtual bool LogQueries { get; } = false;
        protected virtual bool InTransaction { get; } = true;

        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
        protected TestClassBase()
        {
            PiSensorNetDbContext.Initialize(Common.TestsConfiguration.ConnectionString);

            Context = PiSensorNetDbContext.Connect(Common.TestsConfiguration.ConnectionString);

            if (InTransaction)
                Transaction = Context.Database.BeginTransaction(IsolationLevel.ReadUncommitted);

            if (Constants.IsWindows && LogQueries)
                Context.Database.Log = Console.Write;
        }

        public void Dispose()
        {
            if (InTransaction)
                Transaction.Rollback();
        }
    }
}
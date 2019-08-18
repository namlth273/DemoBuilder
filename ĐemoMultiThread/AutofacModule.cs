using Autofac;
using AutoFixture;
using ĐemoMultiThread.WorkerPool;
using MediatR.Extensions.Autofac.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace ĐemoMultiThread
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.AddMediatR(ThisAssembly);
            builder.RegisterType<Publisher>().InstancePerLifetimeScope();
            builder.RegisterType<MemoryCache>().As<IMemoryCache>().InstancePerLifetimeScope();
            builder.RegisterType<ServiceBusClient>().As<IServiceBusClient>()
                .InstancePerLifetimeScope();
            builder.RegisterType<Fixture>().As<IFixture>();
            builder.RegisterType<ProcessMessageBlock>().As<IDataFlowHandler>();
            builder.RegisterGeneric(typeof(WorkerPool<>)).AsSelf();
            builder.RegisterType<WorkerPoolV3>().AsSelf();
        }
    }
}
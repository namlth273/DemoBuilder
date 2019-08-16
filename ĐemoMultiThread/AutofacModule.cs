using Autofac;
using AutoFixture;
using MediatR.Extensions.Autofac.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace ĐemoMultiThread
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.AddMediatR(ThisAssembly);
            builder.RegisterType<Publisher>().InstancePerLifetimeScope();
            builder.RegisterType<MemoryCache>().As<IMemoryCache>().InstancePerLifetimeScope();
            builder.RegisterType<TestDataFlowV3.ServiceBusClient>().As<TestDataFlowV3.IServiceBusClient>()
                .InstancePerLifetimeScope();
            builder.RegisterType<Fixture>().As<IFixture>();
        }
    }
}
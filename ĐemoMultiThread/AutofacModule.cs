using Autofac;
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
        }
    }
}
using Autofac;
using MediatR.Extensions.Autofac.DependencyInjection;

namespace ĐemoMultiThread
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.AddMediatR(ThisAssembly);
            builder.RegisterType<Publisher>().SingleInstance();
        }
    }
}
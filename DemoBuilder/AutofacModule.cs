using Autofac;
using DemoBuilder.Services;
using Flurl.Http.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace DemoBuilder
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ConfigurationService>().As<IConfigurationService>();
            builder.RegisterType<EnvironmentService>().As<IEnvironmentService>();
            builder.RegisterType<CarBuilderFacade>().As<ICarBuilderFacade>();

            builder.RegisterType<CarBaseBuilder>().As<ICarBuilderFacade>().Keyed<ICarBuilderFacade>(typeof(ICarBaseBuilder));
            builder.RegisterType<SideDoorBuilder>().As<ICarBuilderFacade>().Keyed<ICarBuilderFacade>(typeof(ISideDoorBuilder));
            builder.RegisterType<ChassisBuilder>().As<ICarBuilderFacade>().Keyed<ICarBuilderFacade>(typeof(IChassisBuilder));
            builder.RegisterType<FrontGlassBuilder>().As<ICarBuilderFacade>().Keyed<ICarBuilderFacade>(typeof(IFrontGlassBuilder));

            builder.RegisterGeneric(typeof(DependencyDictionary<,>)).As(typeof(IReadOnlyDictionary<,>)).SingleInstance();

            builder.RegisterAssemblyTypes(ThisAssembly)
                .Where(w => w.GetInterfaces().Any(a => a.IsClosedTypeOf(typeof(ICarFactory<>))))
                .AsImplementedInterfaces();

            builder.RegisterType<PerBaseUrlFlurlClientFactory>().As<IFlurlClientFactory>().SingleInstance();
            builder.RegisterType<DemoFlurlClient>().As<IDemoFlurlClient>();
        }
    }
}
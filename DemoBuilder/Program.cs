using Autofac;
using Autofac.Extensions.DependencyInjection;
using DemoBuilder.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DemoBuilder
{
    class Program
    {
        static void Main(string[] args) => MainAsync(args).GetAwaiter().GetResult();
        static async Task MainAsync(string[] args)
        {
            var container = ConfigureContainer();

            using (var scope = container.BeginLifetimeScope())
            {
                //var carBuilder = scope.Resolve<ICarBuilderFacade>();

                //var car = carBuilder.GetBuilder<ICarBaseBuilder>(new HybridCar())
                //    .AddBranch().AddColor().AddType()
                //    .GetBuilder<ISideDoorBuilder>()
                //    .SetDoorSize()
                //    .GetBuilder<IChassisBuilder>()
                //    .SetChassisSize()
                //    .GetBuilder<IFrontGlassBuilder>()
                //    .SetTransparentLevel()
                //    .Build();

                //var carNoDoorFactory = scope.Resolve<ICarFactory<CarNoDoor>>();
                //var hybridCarFactory = scope.Resolve<ICarFactory<HybridCar>>();

                //var carNoDoor = carNoDoorFactory.CreateObject();
                //var hybridCar = hybridCarFactory.CreateObject();

                //Console.WriteLine(JsonConvert.SerializeObject(car));
                //Console.WriteLine(JsonConvert.SerializeObject(hybridCar));
                //Console.WriteLine(JsonConvert.SerializeObject(carNoDoor));

                //var clientFactory = scope.Resolve<IHttpClientFactory>();

                //var client = clientFactory.CreateClient("DemoClient");

                //var request = new HttpRequestMessage(HttpMethod.Post, "/api/product/getall");
                var getRequest = new HttpRequestMessage(HttpMethod.Get, "/comments?postId=1");

                //var response = await client.SendAsync(request);

                //await response.EnsureSuccessStatusCodeAsync();

                //var products = await response.Content.ReadAsAsync<IList<Product>>();

                var demoClient = scope.Resolve<IDemoClient>();

                //var products = await demoClient.SendAsync<List<Product>>(request);

                var value = await demoClient.SendAsync<dynamic>(getRequest);

                //Console.WriteLine(JsonConvert.SerializeObject(products, Formatting.Indented));

                Console.WriteLine(value);
            }
        }

        static IContainer ConfigureContainer()
        {
            var services = new ServiceCollection();
            services.AddHttpClient<IDemoClient, DemoClient>((serviceProvider, client) =>
            {

                var configService = serviceProvider.GetRequiredService<IConfigurationService>();

                var baseUri = new Uri(configService.GetConfiguration()["BASE_URL"]);

                //client.BaseAddress = new Uri("http://localhost:8002/");
                //client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");

                client.BaseAddress = new Uri(baseUri.ToString());
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterModule<AutofacModule>();
            return builder.Build();
        }
    }

    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CarBuilderFacade>().As<ICarBuilderFacade>();

            builder.RegisterType<CarBaseBuilder>().As<ICarBuilderFacade>().Keyed<ICarBuilderFacade>(typeof(ICarBaseBuilder));
            builder.RegisterType<SideDoorBuilder>().As<ICarBuilderFacade>().Keyed<ICarBuilderFacade>(typeof(ISideDoorBuilder));
            builder.RegisterType<ChassisBuilder>().As<ICarBuilderFacade>().Keyed<ICarBuilderFacade>(typeof(IChassisBuilder));
            builder.RegisterType<FrontGlassBuilder>().As<ICarBuilderFacade>().Keyed<ICarBuilderFacade>(typeof(IFrontGlassBuilder));

            builder.RegisterGeneric(typeof(DependencyDictionary<,>)).As(typeof(IReadOnlyDictionary<,>)).SingleInstance();

            builder.RegisterAssemblyTypes(ThisAssembly)
                .Where(w => w.GetInterfaces().Any(a => a.IsClosedTypeOf(typeof(ICarFactory<>))))
                .AsImplementedInterfaces();

            builder.RegisterType<ConfigurationService>().As<IConfigurationService>();
            builder.RegisterType<EnvironmentService>().As<IEnvironmentService>();
        }
    }

    public interface ICarBase
    {
        string Branch { get; set; }
        string Type { get; set; }
        string Color { get; set; }
    }

    public interface ISideDoor : ICarBase
    {
        string DoorSize { get; set; }
    }

    public interface IChassis : ICarBase
    {
        string ChassisSize { get; set; }
    }

    public interface IFrontGlass
    {
        string TransparentLevel { get; set; }
    }

    public class CarNoDoor : IChassis
    {
        public string Branch { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
        public string ChassisSize { get; set; }
    }

    public class HybridCar : ISideDoor, IChassis, IFrontGlass
    {
        public string Branch { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
        public string DoorSize { get; set; }
        public string ChassisSize { get; set; }
        public string TransparentLevel { get; set; }
    }

    public interface ICarBaseBuilder : ICarBuilderFacade
    {
        ICarBaseBuilder AddBranch();
        ICarBaseBuilder AddType();
        ICarBaseBuilder AddColor();
    }

    public interface ISideDoorBuilder : ICarBuilderFacade
    {
        ISideDoorBuilder SetDoorSize();
    }

    public interface IChassisBuilder : ICarBuilderFacade
    {
        IChassisBuilder SetChassisSize();
    }

    public interface IFrontGlassBuilder : ICarBuilderFacade
    {
        IFrontGlassBuilder SetTransparentLevel();
    }

    public interface ICarBuilderFacade
    {
        ICarBase BaseCar { get; set; }
        ICarBase Build();
        T GetBuilder<T>(ICarBase carBase = null) where T : class, ICarBuilderFacade;
    }

    public class CarBuilderFacade : ICarBuilderFacade
    {
        private readonly IReadOnlyDictionary<Type, ICarBuilderFacade> _builders;
        public virtual ICarBase BaseCar { get; set; }

        protected CarBuilderFacade(IReadOnlyDictionary<Type, ICarBuilderFacade> builders)
        {
            _builders = builders;
        }

        public ICarBase Build() => BaseCar;

        public T GetBuilder<T>(ICarBase carBase = null) where T : class, ICarBuilderFacade
        {
            if (_builders[typeof(T)] is T builder)
            {
                builder.BaseCar = carBase ?? BaseCar;

                return builder;
            }

            return null;
        }
    }

    public class CarBaseBuilder : CarBuilderFacade, ICarBaseBuilder
    {
        public CarBaseBuilder(IReadOnlyDictionary<Type, ICarBuilderFacade> builders) : base(builders)
        {
        }

        public ICarBaseBuilder AddBranch()
        {
            BaseCar.Branch = "BMW";
            return this;
        }

        public ICarBaseBuilder AddType()
        {
            BaseCar.Type = "SUV";
            return this;
        }

        public ICarBaseBuilder AddColor()
        {
            BaseCar.Color = "Red";
            return this;
        }
    }

    public class SideDoorBuilder : CarBuilderFacade, ISideDoorBuilder
    {
        protected ISideDoor Car => BaseCar as ISideDoor;

        public SideDoorBuilder(IReadOnlyDictionary<Type, ICarBuilderFacade> builders) : base(builders)
        {
        }

        public ISideDoorBuilder SetDoorSize()
        {
            Car.DoorSize = "Big";
            return this;
        }
    }

    public class ChassisBuilder : CarBuilderFacade, IChassisBuilder
    {
        protected IChassis Car => BaseCar as IChassis;

        public ChassisBuilder(IReadOnlyDictionary<Type, ICarBuilderFacade> builders) : base(builders)
        {
        }

        public IChassisBuilder SetChassisSize()
        {
            Car.ChassisSize = "Big";
            return this;
        }
    }

    public class FrontGlassBuilder : CarBuilderFacade, IFrontGlassBuilder
    {
        protected IFrontGlass Car => BaseCar as IFrontGlass;

        public FrontGlassBuilder(IReadOnlyDictionary<Type, ICarBuilderFacade> builders) : base(builders)
        {
        }

        public IFrontGlassBuilder SetTransparentLevel()
        {
            Car.TransparentLevel = "Dark Smoke";
            return this;
        }
    }
}

using Autofac;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DemoBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = ConfigureContainer();

            using (var scope = container.BeginLifetimeScope())
            {
                var carBuilder = scope.Resolve<ICarBuilderFacade>();

                var car = carBuilder.GetBuilder<ICarBaseBuilder>(new HybridCar())
                    .AddBranch().AddColor().AddType()
                    .GetBuilder<ISideDoorBuilder>()
                    .SetDoorSize()
                    .GetBuilder<IChassisBuilder>()
                    .SetChassisSize()
                    .Build();

                var carNoDoor = carBuilder.GetBuilder<ICarBaseBuilder>(new CarNoDoor())
                    .AddBranch().AddColor().AddType()
                    .GetBuilder<IChassisBuilder>().SetChassisSize()
                    .Build();

                Console.WriteLine(JsonConvert.SerializeObject(car));
                Console.WriteLine(JsonConvert.SerializeObject(carNoDoor));
            }
        }

        static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<AutofacModule>();
            return builder.Build();
        }
    }

    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CarBuilderFacade>().As<ICarBuilderFacade>();
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

    public class CarNoDoor : IChassis
    {
        public string Branch { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
        public string ChassisSize { get; set; }
    }

    public class HybridCar : ISideDoor, IChassis
    {
        public string Branch { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
        public string DoorSize { get; set; }
        public string ChassisSize { get; set; }
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

    public interface ICarBuilderFacade
    {
        ICarBase BaseCar { get; set; }
        ICarBase Build();
        T GetBuilder<T>(ICarBase carBase = null) where T : class, ICarBuilderFacade;
    }

    public class CarBuilderFacade : ICarBuilderFacade
    {
        private readonly IDictionary<Type, Func<ICarBuilderFacade>> _builders;
        public virtual ICarBase BaseCar { get; set; }
        public ICarBase Build() => BaseCar;

        public CarBuilderFacade()
        {
            _builders = new Dictionary<Type, Func<ICarBuilderFacade>>
            {
                { typeof(ICarBaseBuilder), () => new CarBaseBuilder()},
                { typeof(ISideDoorBuilder), () => new SideDoorBuilder()},
                { typeof(IChassisBuilder), () => new ChassisBuilder()}
            };
        }

        public T GetBuilder<T>(ICarBase carBase = null) where T : class, ICarBuilderFacade
        {
            if (_builders[typeof(T)]() is T builder)
            {
                builder.BaseCar = carBase ?? BaseCar;

                return builder;
            }

            return null;
        }
    }

    public class CarBaseBuilder : CarBuilderFacade, ICarBaseBuilder
    {
        public override ICarBase BaseCar { get; set; }

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
        public override ICarBase BaseCar { get; set; }
        protected ISideDoor Car => BaseCar as ISideDoor;

        public ISideDoorBuilder SetDoorSize()
        {
            Car.DoorSize = "Big";
            return this;
        }
    }

    public class ChassisBuilder : CarBuilderFacade, IChassisBuilder
    {
        public override ICarBase BaseCar { get; set; }
        protected IChassis Car => BaseCar as IChassis;

        public IChassisBuilder SetChassisSize()
        {
            Car.ChassisSize = "Big";
            return this;
        }
    }
}

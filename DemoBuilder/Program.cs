using Autofac;
using Newtonsoft.Json;
using System;

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

                carBuilder.BaseCar = new HybridCar();

                var car = carBuilder
                    .Base.AddBranch().AddColor().AddType()
                    .SideDoor.SetDoorSize()
                    .Chassis.SetChassisSize().Build();

                carBuilder.BaseCar = new CarNoDoor();

                var carNoDoor = carBuilder
                    .Base.AddBranch().AddColor().AddType()
                    .Chassis.SetChassisSize()
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
        ICarBaseBuilder Base { get; }
        ISideDoorBuilder SideDoor { get; }
        IChassisBuilder Chassis { get; }

        ICarBase Build();
    }

    public class CarBuilderFacade : ICarBuilderFacade
    {
        public virtual ICarBase BaseCar { get; set; }

        public ICarBase Build() => BaseCar;

        public ICarBaseBuilder Base
        {
            get
            {
                var builder = new CarBaseBuilder { BaseCar = BaseCar };
                return builder;
            }
        }

        public ISideDoorBuilder SideDoor
        {
            get
            {
                var builder = new SideDoorBuilder { BaseCar = BaseCar as ISideDoor };
                return builder;
            }
        }

        public IChassisBuilder Chassis
        {
            get
            {
                var builder = new ChassisBuilder { BaseCar = BaseCar as IChassis };
                return builder;
            }
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

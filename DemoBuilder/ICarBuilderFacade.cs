using System;
using System.Collections.Generic;

namespace DemoBuilder
{
    public interface ICarBuilderFacade
    {
        ICarBase BaseCar { get; set; }
        ICarBase Build();
        T GetBuilder<T>(ICarBase carBase = null) where T : class, ICarBuilderFacade;
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
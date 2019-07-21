namespace DemoBuilder
{
    public interface ICarFactory<T>
        where T : ICarBase
    {
        T CreateObject();
    }

    public class CarNoDoorFactory : ICarFactory<CarNoDoor>
    {
        private readonly ICarBuilderFacade _carBuilder;

        public CarNoDoorFactory(ICarBuilderFacade carBuilder)
        {
            _carBuilder = carBuilder;
        }

        public CarNoDoor CreateObject()
        {
            var car = _carBuilder.GetBuilder<ICarBaseBuilder>(new CarNoDoor())
                .AddBranch().AddColor().AddType()
                .GetBuilder<IChassisBuilder>().SetChassisSize()
                .Build();

            return car as CarNoDoor;
        }
    }

    public class HybridCarFactory : ICarFactory<HybridCar>
    {
        private readonly ICarBuilderFacade _carBuilder;

        public HybridCarFactory(ICarBuilderFacade carBuilder)
        {
            _carBuilder = carBuilder;
        }

        public HybridCar CreateObject()
        {
            var car = _carBuilder.GetBuilder<ICarBaseBuilder>(new HybridCar())
                .AddBranch().AddColor().AddType()
                .GetBuilder<ISideDoorBuilder>()
                .SetDoorSize()
                .GetBuilder<IChassisBuilder>()
                .SetChassisSize()
                .GetBuilder<IFrontGlassBuilder>()
                .SetTransparentLevel()
                .Build();

            return car as HybridCar;
        }
    }
}
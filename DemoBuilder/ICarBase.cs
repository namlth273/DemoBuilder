namespace DemoBuilder
{
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
}
namespace Iirc.EnergyLimitsScheduling.Shared.Input.Readers
{
    using Iirc.EnergyLimitsScheduling.Shared.Input;

    public interface IInputReader
    {
        Instance ReadFromPath(string instancePath);
    }
}
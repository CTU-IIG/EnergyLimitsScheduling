namespace Iirc.EnergyLimitsScheduling.Shared.Input.Writers
{
    using Iirc.EnergyLimitsScheduling.Shared.Input;

    public interface IInputWriter
    {
        void WriteToPath(Instance instance, string instancePath);
    }
}
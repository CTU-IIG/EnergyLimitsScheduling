namespace Iirc.EnergyLimitsScheduling.DatasetGenerators.Generators
{
    using System.Collections.Generic;
    using Iirc.EnergyLimitsScheduling.Shared.Input;

    public interface IDatasetGenerator
    {
        IEnumerable<Instance> GenerateInstances(Prescription prescription);
    }
}
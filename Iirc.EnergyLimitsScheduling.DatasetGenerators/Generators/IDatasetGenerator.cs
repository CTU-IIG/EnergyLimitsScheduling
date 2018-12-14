// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="IDatasetGenerator.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.DatasetGenerators.Generators
{
    using System.Collections.Generic;
    using Iirc.EnergyLimitsScheduling.Shared.Input;

    public interface IDatasetGenerator
    {
        IEnumerable<Instance> GenerateInstances(Prescription prescription);
    }
}
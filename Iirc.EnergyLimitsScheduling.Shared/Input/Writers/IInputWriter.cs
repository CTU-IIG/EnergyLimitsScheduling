// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="IInputWriter.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Input.Writers
{
    using Iirc.EnergyLimitsScheduling.Shared.Input;

    public interface IInputWriter
    {
        void WriteToPath(Instance instance, string instancePath);
    }
}
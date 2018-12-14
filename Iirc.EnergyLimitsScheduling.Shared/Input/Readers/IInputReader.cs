// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="IInputReader.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Input.Readers
{
    using Iirc.EnergyLimitsScheduling.Shared.Input;

    public interface IInputReader
    {
        Instance ReadFromPath(string instancePath);
    }
}
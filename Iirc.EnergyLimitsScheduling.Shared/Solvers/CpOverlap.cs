// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="CpOverlap.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using System;

    public class CpOverlap : PythonScript<CpOverlap.SpecializedSolverConfig>
    {
        public CpOverlap() : base("cp_overlap")
        {
        }

        protected override void CheckInstanceValidity()
        {
            if (this.solverConfig.ContinuousStartTimes)
            {
                throw new ArgumentException("Solver cannot handle continuous start times.");
            }
        }

        public class SpecializedSolverConfig
        {

        }
    }
}

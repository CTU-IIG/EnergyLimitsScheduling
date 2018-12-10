namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using System;
    using System.Collections.Generic;
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using Iirc.Utils.SolverFoundations;

    public class SolverResult : ISolverResult
    {
        public Status Status { get; set; }

        public bool TimeLimitReached { get; set; }

        public TimeSpan RunningTime { get; set; }

        public StartTimes StartTimes { get; set; }
        
        public double? LowerBound { get; set; }
    }
}
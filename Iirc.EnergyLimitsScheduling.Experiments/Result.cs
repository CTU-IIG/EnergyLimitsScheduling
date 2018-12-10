namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using System;
    using System.Collections.Generic;
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Iirc.Utils.SolverFoundations;

    public class Result
    {
        public Status Status { get; set; }

        public bool TimeLimitReached { get; set; }

        public TimeSpan RunningTime { get; set; }

        public List<StartTimes.IndexedStartTime> StartTimes { get; set; }
        
        public double? LowerBound { get; set; }

        public static Result FromSolverResult(SolverResult solverResult)
        {
            return new Result
            {
                Status = solverResult.Status,
                TimeLimitReached = solverResult.TimeLimitReached,
                RunningTime = solverResult.RunningTime,
                StartTimes = solverResult.StartTimes == null ? null : solverResult.StartTimes.ToIndexedStartTimes(),
                LowerBound = solverResult.LowerBound
            };
        }
    }
}
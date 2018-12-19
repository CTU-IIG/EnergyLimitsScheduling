// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Result.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using System;
    using System.Collections.Generic;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Iirc.Utils.SolverFoundations;

    /// <summary>
    /// The result of running a solver on an instance.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time limit was reached, i.e., solver had to be interrupted.
        /// </summary>
        public bool TimeLimitReached { get; set; }

        /// <summary>
        /// Gets or sets the running time of the solver.
        /// </summary>
        public TimeSpan RunningTime { get; set; }

        /// <summary>
        /// Gets or sets the found start times of the operations.
        /// </summary>
        public List<StartTimes.IndexedStartTime> StartTimes { get; set; }
        
        /// <summary>
        /// Gets or sets the achieved lower bound.
        /// </summary>
        public double? LowerBound { get; set; }

        public static Result FromSolverResult(SolverResult solverResult)
        {
            return new Result
            {
                Status = solverResult.Status,
                TimeLimitReached = solverResult.TimeLimitReached,
                RunningTime = solverResult.RunningTime,
                StartTimes = solverResult.StartTimes?.ToIndexedStartTimes(),
                LowerBound = solverResult.LowerBound
            };
        }
    }
}
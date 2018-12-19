// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="PrescriptionSolverConfig.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using System;
    using System.Collections.Generic;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The solver configuration in the experimental setup.
    /// </summary>
    /// <remarks>If the properties are not provided, the default values from <see cref="SolverConfig"/> are used.
    /// </remarks>
    public class PrescriptionSolverConfig
    {
        /// <summary>
        /// Gets or sets the time limit given to the solver.
        /// </summary>
        public TimeSpan? TimeLimit { get; set; }     
        
        /// <summary>
        /// Gets or sets a value indicating the start times can be continuous.
        /// </summary>
        public bool? ContinuousStartTimes { get; set; }

        /// <summary>
        /// Gets or sets a number of parallel workers used by the solver.
        /// </summary>
        public int? NumWorkers { get; set; }

        public SolverConfig ToSolverConfig(JObject specializedSolverConfig)
        {
            var solverConfig = new SolverConfig();

            if (this.TimeLimit.HasValue)
            {
                solverConfig.TimeLimit = this.TimeLimit;
            }

            if (this.ContinuousStartTimes.HasValue)
            {
                solverConfig.ContinuousStartTimes = this.ContinuousStartTimes.Value;
            }

            if (this.NumWorkers.HasValue)
            {
                solverConfig.NumWorkers = this.NumWorkers.Value;
            }

            if (specializedSolverConfig != null) {
                solverConfig.SpecializedSolverConfig = specializedSolverConfig.ToObject<Dictionary<string, object>>();
            }

            return solverConfig;
        }

        public static PrescriptionSolverConfig Merge(PrescriptionSolverConfig general, PrescriptionSolverConfig specific)
        {
            return new PrescriptionSolverConfig
            {
                TimeLimit = specific == null || specific.TimeLimit.HasValue == false ?
                    general.TimeLimit : specific.TimeLimit.Value,
                ContinuousStartTimes = specific == null || specific.ContinuousStartTimes.HasValue == false ?
                    general.ContinuousStartTimes : specific.ContinuousStartTimes.Value,
                NumWorkers = specific == null || specific.NumWorkers.HasValue == false ?
                    general.NumWorkers : specific.NumWorkers.Value
            };
        }
    }
}
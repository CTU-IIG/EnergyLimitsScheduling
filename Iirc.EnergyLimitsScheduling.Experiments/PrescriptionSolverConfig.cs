namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class PrescriptionSolverConfig
    {
        public TimeSpan? TimeLimit { get; set; }     
        
        public bool? ContinuousStartTimes { get; set; }

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
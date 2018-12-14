// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Config.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.SolverCli
{
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    
    public class Config
    {
        public TimeSpan? TimeLimit { get; set; }     

        public string SolverName { get; set; }     
        
        public int? RandomSeed { get; set; }     
        
        public bool? WithEnergyLimits { get; set; }
        
        public bool? ContinuousStartTimes { get; set; }
        
        public List<StartTimes.IndexedStartTime> InitStartTimes { get; set; }     
        
        public List<StartTimes.IndexedStartTime> FixedOrder { get; set; }     

        public int? NumWorkers { get; set; }
        
        public JObject SpecializedSolverConfig { get; set; }     

        public SolverConfig ToSolverConfig()
        {
            var solverConfig = new SolverConfig();

            if (this.TimeLimit.HasValue)
            {
                solverConfig.TimeLimit = this.TimeLimit.Value;
            }

            if (this.WithEnergyLimits.HasValue)
            {
                solverConfig.WithEnergyLimits = this.WithEnergyLimits.Value;
            }

            if (this.ContinuousStartTimes.HasValue)
            {
                solverConfig.ContinuousStartTimes = this.ContinuousStartTimes.Value;
            }

            if (this.InitStartTimes != null)
            {
                solverConfig.InitStartTimes = this.InitStartTimes;
            }

            if (this.FixedOrder != null)
            {
                solverConfig.FixedOrder = this.FixedOrder;
            }

            if (this.NumWorkers.HasValue)
            {
                solverConfig.NumWorkers = this.NumWorkers.Value;
            }

            if (this.SpecializedSolverConfig != null)
            {
                solverConfig.SpecializedSolverConfig = this.SpecializedSolverConfig.ToObject<Dictionary<string, object>>();
            }

            return solverConfig;
        }
    }
}
// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Config.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.SolverCli
{
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    /// The program configuration.
    /// </summary>
    /// <remarks>If the properties are not provided, the default values from <see cref="SolverConfig"/> are used.
    /// </remarks>
    public class Config
    {
        /// <summary>
        /// Gets or sets the time limit given to the solver.
        /// </summary>
        public TimeSpan? TimeLimit { get; set; }     

        /// <summary>
        /// Gets or sets the name of the solver class.
        /// </summary>
        public string SolverName { get; set; }     
        
        /// <summary>
        /// Gets or sets a value indicating whether the energy limits are considered.
        /// </summary>
        public bool? WithEnergyLimits { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating the start times can be continuous.
        /// </summary>
        public bool? ContinuousStartTimes { get; set; }
        
        /// <summary>
        /// Gets or sets the initial start times of the operations.
        /// </summary>
        public List<StartTimes.IndexedStartTime> InitStartTimes { get; set; }     
        
        /// <summary>
        /// Gets or sets the start times of the operations, that define a fixed total ordering of the operations on the
        /// machines. The solution must have the same order of the operations.
        /// </summary>
        public List<StartTimes.IndexedStartTime> FixedOrder { get; set; }     

        /// <summary>
        /// Gets or sets a number of parallel workers used by the solver.
        /// </summary>
        public int? NumWorkers { get; set; }
        
        /// <summary>
        /// Gets or sets the solver configuration that is specific for the solver (see the specialized configuration
        /// class contained in the solvers for more details).
        /// </summary>
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
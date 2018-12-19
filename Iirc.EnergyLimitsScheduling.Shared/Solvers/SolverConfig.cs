// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="SolverConfig.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using System;
    using System.Collections.Generic;
    using Iirc.Utils.SolverFoundations;

    /// <summary>
    /// The configuration of the solver.
    /// </summary>
    [Serializable]
    public class SolverConfig : ISolverConfig
    {
        public SolverConfig()
        {
            this.TimeLimit = null;
            this.SpecializedSolverConfig = new Dictionary<string, object>();
            this.WithEnergyLimits = true;
            this.InitStartTimes = null;
            this.FixedOrder = null;
            this.ValidStartTimes = null;
            this.ContinuousStartTimes = false;
            this.NumWorkers = 0;
        }

        /// <summary>
        /// Gets or sets the time limit given to the solver.
        /// </summary>
        public TimeSpan? TimeLimit { get; set; }

        /// <summary>
        /// Gets or sets the solver configuration that is specific for the solver (see the specialized configuration
        /// class contained in the solvers for more details).
        /// </summary>
        public Dictionary<string, object> SpecializedSolverConfig { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the energy limits are considered. Default is true.
        /// </summary>
        public bool WithEnergyLimits { get; set; }
        
        /// <summary>
        /// Gets or sets the initial start times of the operations. Default is null, i.e., no start times.
        /// </summary>
        public List<StartTimes.IndexedStartTime> InitStartTimes { get; set; }
        
        /// <summary>
        /// Gets or sets the start times of the operations, that define a fixed total ordering of the operations on the
        /// machines. The solution must have the same order of the operations. Default is null, i.e., no start times.
        /// </summary>
        public List<StartTimes.IndexedStartTime> FixedOrder { get; set; }
        
        /// <summary>
        /// Gets or sets the range of start times of the operations that are valid. Default is null, i.e., no ranges.
        /// </summary>
        public List<StartTimes.IndexedStartTimeRange> ValidStartTimes { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating the start times can be continuous. Default is false.
        /// </summary>
        public bool ContinuousStartTimes { get; set; }

        /// <summary>
        /// Gets or sets a number of parallel workers used by the solver. Default is 0, meaning that the default
        /// settings of the solver is used.
        /// </summary>
        public int NumWorkers { get; set; }

        public bool IsOrderFixed => this.FixedOrder != null;
    }
}
namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using Iirc.Utils.SolverFoundations;

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
            this.ContinuousStartTimes = false;
            this.NumWorkers = 0;
        }

        public TimeSpan? TimeLimit { get; set; }

        public Dictionary<string, object> SpecializedSolverConfig { get; set; }
        
        public bool WithEnergyLimits { get; set; }
        
        public List<StartTimes.IndexedStartTime> InitStartTimes { get; set; }
        
        public List<StartTimes.IndexedStartTime> FixedOrder { get; set; }
        
        public List<StartTimes.IndexedStartTimeRange> ValidStartTimes { get; set; }
        
        public bool ContinuousStartTimes { get; set; }

        public int NumWorkers { get; set; }

        public bool IsOrderFixed
        {
            get { return this.FixedOrder != null; }
        }
    }
}
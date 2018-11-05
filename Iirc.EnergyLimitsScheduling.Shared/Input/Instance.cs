namespace Iirc.EnergyLimitsScheduling.Shared.Input
{
    using System.Collections.Generic;
    using Iirc.Utils.SolverFoundations;

    public class Instance : IInstance
    {
        public int NumMachines { get; }
        public Job[] Jobs { get; }
        public double EnergyLimit { get; }
        public int Horizon { get; }
        public int LengthMeteringInterval { get; }
        public int NumMeteringIntervals { get; }

        public object Metadata { get; }

        public Instance(
            int numMachines,
            Job[] jobs,
            double energyLimit,
            int horizon,
            int lengthMeteringInterval,
            object metadata = null)
        {
            this.NumMachines = numMachines;
            this.Jobs = jobs;
            this.EnergyLimit = energyLimit;
            this.Horizon = horizon;
            this.LengthMeteringInterval = lengthMeteringInterval;
            this.NumMeteringIntervals = this.Horizon / this.LengthMeteringInterval;
            this.Metadata = metadata;
        }
    }
}
// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Job.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Input
{
    public class Job
    {
        public int Id { get; }
        public int Index { get; }
        public Operation[] Operations { get; }

        public Job(int id, int index, Operation[] operations)
        {
            this.Id = id;
            this.Index = index;
            this.Operations = operations;
        }
    }
}
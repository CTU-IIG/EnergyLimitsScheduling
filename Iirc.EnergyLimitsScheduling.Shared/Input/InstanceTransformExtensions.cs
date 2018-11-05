namespace Iirc.EnergyLimitsScheduling.Shared.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class InstanceTransformExtensions
    {
        public static Instance ClampProcessingTime(
            this Instance instance,
            int minProcessingTime = 0,
            int maxProcessingTime = int.MaxValue)
        {
            var newJobs = instance.Jobs
                .Select(job => new Job(
                    job.Id,
                    job.Index,
                    job.Operations
                        .Select(operation => new Operation(
                            operation.Id,
                            operation.Index,
                            operation.JobIndex,
                            operation.MachineIndex,
                            Math.Clamp(operation.ProcessingTime, minProcessingTime, maxProcessingTime),
                            operation.PowerConsumption))
                        .ToArray()))
                .ToArray();

            return new Instance(
                instance.NumMachines,
                newJobs,
                instance.EnergyLimit,
                instance.Horizon,
                instance.LengthMeteringInterval);
        }

        public static Instance FloorPowerConsumption(this Instance instance)
        {
            var newJobs = instance.Jobs
                .Select(job => new Job(
                    job.Id,
                    job.Index,
                    job.Operations
                        .Select(operation => new Operation(
                            operation.Id,
                            operation.Index,
                            operation.JobIndex,
                            operation.MachineIndex,
                            operation.ProcessingTime,
                            Math.Floor(operation.PowerConsumption)))
                        .ToArray()))
                .ToArray();

            return new Instance(
                instance.NumMachines,
                newJobs,
                instance.EnergyLimit,
                instance.Horizon,
                instance.LengthMeteringInterval);
        }

        public static Instance SingleMachine(this Instance instance)
        {
            var newJobs = instance.Jobs
                .Select(job => new Job(
                    job.Id,
                    job.Index,
                    job.Operations
                        .Select(operation => new Operation(
                            operation.Id,
                            operation.Index,
                            operation.JobIndex,
                            0,
                            operation.ProcessingTime,
                            operation.PowerConsumption))
                        .ToArray()))
                .ToArray();

            return new Instance(
                1,
                newJobs,
                instance.EnergyLimit,
                instance.Horizon,
                instance.LengthMeteringInterval);
        }

        public static Instance SingleOperationJobs(
            this Instance instance)
        {
            var newJobs = instance.AllOperations()
                .Select(
                    (operation, index) => new Job(
                        index,
                        index,
                        new[]
                        {
                            new Operation(
                                index,
                                0,
                                index,
                                operation.MachineIndex,
                                operation.ProcessingTime,
                                operation.PowerConsumption)
                        }))
                .ToArray();

            return new Instance(
                instance.NumMachines,
                newJobs,
                instance.EnergyLimit,
                instance.Horizon,
                instance.LengthMeteringInterval);
        }
    }
}
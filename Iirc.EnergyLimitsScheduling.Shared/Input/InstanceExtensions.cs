namespace Iirc.EnergyLimitsScheduling.Shared.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.Utils.Math;

    public static class InstanceExtensions
    {
        public static int TotalNumOperations(this Instance instance)
        {
            return instance.Jobs.Sum(job => job.Operations.Length);
        }
        
        public static IEnumerable<Operation> AllOperations(this Instance instance)
        {
            return instance.Jobs.SelectMany(job => job.Operations);
        }

        public static IEnumerable<Operation> MachineOperations(this Instance instance, int machineIndex)
        {
            return instance.AllOperations().Where(operation => operation.MachineIndex == machineIndex);
        }
        
        public static IDictionary<Operation, int> Heads(this Instance instance)
        {
            var result = new Dictionary<Operation, int>();
            foreach (var job in instance.Jobs)
            {
                var head = 0;
                foreach (var operation in job.Operations)
                {
                    result[operation] = head;
                    head += operation.ProcessingTime;
                }
            }

            return result;
        }

        public static int LastMeteringIntervalIndex(this Instance instance)
        {
            return instance.NumMeteringIntervals - 1;
        }
        
        public static int FirstMeteringIntervalIndex(this Instance instance)
        {
            return 0;
        }

        public static IEnumerable<int> MeteringIntervals(this Instance instance)
        {
            return Enumerable.Range(0, instance.NumMeteringIntervals);
        }
        
        public static IEnumerable<int> Machines(this Instance instance)
        {
            return Enumerable.Range(0, instance.NumMachines);
        }

        public static int MeteringIntervalStart(this Instance instance, int meteringIntervalIndex)
        {
            return instance.LengthMeteringInterval * meteringIntervalIndex;
        }
        
        public static int MeteringIntervalEnd(this Instance instance, int meteringIntervalIndex)
        {
            return instance.LengthMeteringInterval * (meteringIntervalIndex + 1);
        }
        
        public static Operation FirstOperation(this Job job)
        {
            return job.Operations[0];
        }
        
        public static Operation LastOperation(this Job job)
        {
            return job.Operations[job.Operations.Length - 1];
        }
        
        public static Operation PreviousOperation(this Operation operation, Instance instance)
        {
            if (operation.Index == 0)
            {
                return null;
            }
            
            return instance.Jobs[operation.JobIndex].Operations[operation.Index - 1];
        }
        
        public static Operation NextOperation(this Operation operation, Instance instance)
        {
            var job = instance.Jobs[operation.JobIndex];
            if ((operation.Index + 1) == job.Operations.Length)
            {
                return null;
            }
            
            return job.Operations[operation.Index + 1];
        }

        public static IEnumerable<Tuple<int, int>> NonZeroOverlaps(
            this Operation operation, int startTime, Instance instance)
        {
            var completionTime = startTime + operation.ProcessingTime;
            
            for (int meteringIntervalIndex = startTime / instance.LengthMeteringInterval;
                meteringIntervalIndex < instance.NumMeteringIntervals;
                meteringIntervalIndex++)
            {
                var overlap = Intervals.OverlapLength(
                    startTime,
                    completionTime,
                    instance.MeteringIntervalStart(meteringIntervalIndex),
                    instance.MeteringIntervalEnd(meteringIntervalIndex));
                
                if (overlap == 0)
                {
                    yield break;
                }

                yield return Tuple.Create(meteringIntervalIndex, overlap);
            }
        }

        public static IEnumerable<double> Overlaps(this Operation operation, double startTime, Instance instance)
        {
            var completionTime = startTime + operation.ProcessingTime;
            return instance.MeteringIntervals()
                .Select(meteringIntervalIndex => Intervals.OverlapLength(
                    startTime,
                    completionTime,
                    instance.MeteringIntervalStart(meteringIntervalIndex),
                    instance.MeteringIntervalEnd(meteringIntervalIndex)));
        }
        
        public static IEnumerable<Tuple<int, double>> NonZeroOverlaps(
            this Operation operation, double startTime, Instance instance)
        {
            var comparer = NumericComparer.Default;
            var completionTime = startTime + operation.ProcessingTime;

            bool foundNonZeroOverlap = false;
            for (int meteringIntervalIndex = (int)startTime / instance.LengthMeteringInterval;
                meteringIntervalIndex < instance.NumMeteringIntervals;
                meteringIntervalIndex++)
            {
                var overlap = Intervals.OverlapLength(
                    startTime,
                    completionTime,
                    instance.MeteringIntervalStart(meteringIntervalIndex),
                    instance.MeteringIntervalEnd(meteringIntervalIndex));
                
                if (comparer.AreEqual(overlap, 0.0) && foundNonZeroOverlap)
                {
                    yield break;
                }
                
                foundNonZeroOverlap = true;
                yield return Tuple.Create(meteringIntervalIndex, overlap);
            }
        }

        public static int MaxNumNonZeroOvelapIntervals(this Operation operation, Instance instance)
        {
            return (int) Math.Ceiling(operation.ProcessingTime / ((double) instance.LengthMeteringInterval)) + 1;
        }
    }
}
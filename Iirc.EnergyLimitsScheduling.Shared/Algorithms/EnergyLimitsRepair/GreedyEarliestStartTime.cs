// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="GreedyEarliestStartTime.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Algorithms.EnergyLimitsRepair
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System;
    using System.Linq;
    using System.Runtime.Serialization;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;

    public class GreedyEarliestStartTime
    {
        internal Instance instance;

        internal StartTimes oldStartTimes;

        internal StartTimes newStartTimes;

        internal double[] energyConsumptions;

        internal double[] scheduleLengths;

        internal BasePriorityRule priorityRule;

        public StartTimes Repair(Instance instance, StartTimes startTimes, PriorityRule priorityRule)
        {
            this.instance = instance;
            this.oldStartTimes = startTimes;
            this.newStartTimes = new StartTimes();
            this.energyConsumptions = new double[this.instance.NumMeteringIntervals];
            this.scheduleLengths = new double[this.instance.NumMachines];
            switch (priorityRule)
            {
                case PriorityRule.StartTime:
                    this.priorityRule = new StartTimePriorityRule(this);
                    break;

                case PriorityRule.MostRemainingWork:
                    this.priorityRule = new MostRemainingWorkPriorityRule(this);
                    break;

                case PriorityRule.EarliestStartTime:
                    this.priorityRule = new EarliestStartTimePriorityRule(this);
                    break;

                case PriorityRule.EarliestStartTimeOverFixedOrder:
                    this.priorityRule = new EarliestStartTimeOverFixedOrderPriorityRule(this);
                    break;

                default:
                    throw new ArgumentException($"Unknown priority rule {this.priorityRule}");
            }

            foreach (var operation in this.priorityRule.OrderedOperations())
            {
                var newStartTime = this.FindEarliestFeasibleStartTime(operation);
                this.newStartTimes[operation] = newStartTime;
                this.scheduleLengths[operation.MachineIndex] = newStartTime + operation.ProcessingTime;

                foreach (var (meteringIntervalIndex, overlap) in operation.NonZeroOverlaps(newStartTime, this.instance))
                {
                    this.energyConsumptions[meteringIntervalIndex] += overlap * operation.PowerConsumption;
                }

                this.priorityRule.OperationScheduled(operation);
            }

            return this.newStartTimes;
        }

        /// <summary>
        /// </summary>
        /// <param name="operation"></param>
        /// <returns></returns>
        /// <exception cref="CannotRepairException"></exception>
        /// <remarks>
        /// Has no side-effects.
        /// </remarks>
        private double FindEarliestFeasibleStartTime(Operation operation)
        {
            var earliestStartTime = this.scheduleLengths[operation.MachineIndex];
            
            // TODO:
            /*
            var previousOperation = operation.PreviousOperation(this.instance);
            if (previousOperation != null)
            {
                earliestStartTime = Math.Max(earliestStartTime,
                    this.newStartTimes[previousOperation] + previousOperation.ProcessingTime);
            }

            var meteringIntervalIndex = earliestStartTime / this.instance.LengthMeteringInterval;
            while (meteringIntervalIndex < this.instance.NumMeteringIntervals)
            {
                var overlap = Intervals.OverlapLength(
                    earliestStartTime,
                    earliestStartTime + operation.ProcessingTime,
                    this.instance.MeteringIntervalStart(meteringIntervalIndex),
                    this.instance.MeteringIntervalEnd(meteringIntervalIndex));

                if (overlap == 0)
                {
                    break;
                }

                if (NumericComparer.Default.Greater(
                    overlap * operation.PowerConsumption + this.energyConsumptions[meteringIntervalIndex],
                    this.instance.EnergyLimit))
                {
                    // Current start time violates the energy limit.
                    var maxOverlap =
                        (int) ((this.instance.EnergyLimit - this.energyConsumptions[meteringIntervalIndex]) /
                               operation.PowerConsumption);

                    var newEarliestStartTime = instance.MeteringIntervalEnd(meteringIntervalIndex) - maxOverlap;

                    Debug.Assert(maxOverlap < this.instance.LengthMeteringInterval);
                    Debug.Assert(maxOverlap < operation.ProcessingTime);
                    Debug.Assert(maxOverlap < overlap);
                    Debug.Assert(earliestStartTime < newEarliestStartTime);
                    Debug.Assert(NumericComparer.Default.LessOrEqual(
                        maxOverlap * operation.PowerConsumption + this.energyConsumptions[meteringIntervalIndex],
                        this.instance.EnergyLimit));

                    earliestStartTime = newEarliestStartTime;
                }

                meteringIntervalIndex++;
            }

            if (earliestStartTime >= this.instance.Horizon)
            {
                throw new CannotRepairException();
            }
            */

            return earliestStartTime;
        }

        public enum PriorityRule
        {
            StartTime = 0,
            MostRemainingWork = 1,

            /// <summary>
            /// Selects the operation with the earliest start time (w.r.t. the energy limits). Ties are broken by
            /// selecting the operation with the smallest old start time.
            /// </summary>
            EarliestStartTime = 2,

            EarliestStartTimeOverFixedOrder = 3
        }

        internal abstract class BasePriorityRule
        {
            protected GreedyEarliestStartTime alg;

            public BasePriorityRule(GreedyEarliestStartTime alg)
            {
                this.alg = alg;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            /// <remarks>
            /// The order can be dynamic, i.e., the next returned operation may depend on the new start times.
            /// </remarks>
            public abstract IEnumerable<Operation> OrderedOperations();

            public virtual void OperationScheduled(Operation operation)
            {
            }
        }

        private class StartTimePriorityRule : BasePriorityRule
        {
            public StartTimePriorityRule(GreedyEarliestStartTime alg) : base(alg)
            {
            }

            public override IEnumerable<Operation> OrderedOperations()
            {
                return this.alg.oldStartTimes.OrderBy(pair => pair.Value).Select(pair => pair.Key);
            }
        }

        private class MostRemainingWorkPriorityRule : BasePriorityRule
        {
            public MostRemainingWorkPriorityRule(GreedyEarliestStartTime alg) : base(alg)
            {
            }

            public override IEnumerable<Operation> OrderedOperations()
            {
                var remaningWorks = new Dictionary<Operation, int>();
                foreach (var job in this.alg.instance.Jobs)
                {
                    var remainingWork = 0;
                    foreach (var operation in job.Operations.Reverse())
                    {
                        remainingWork += operation.ProcessingTime;
                        remaningWorks[operation] = remainingWork;
                    }
                }

                return this.alg.instance.AllOperations().OrderByDescending(operation => remaningWorks[operation]);
            }
        }

        private class EarliestStartTimePriorityRule : BasePriorityRule
        {
            private HashSet<Operation> frontalOperations;

            public EarliestStartTimePriorityRule(GreedyEarliestStartTime alg) : base(alg)
            {
            }

            public override IEnumerable<Operation> OrderedOperations()
            {
                this.frontalOperations = new HashSet<Operation>();
                foreach (var job in this.alg.instance.Jobs)
                {
                    this.frontalOperations.Add(job.FirstOperation());
                }

                while (this.frontalOperations.Any())
                {
                    var startTimes = new StartTimes();
                    foreach (var operation in this.frontalOperations)
                    {
                        startTimes[operation] = this.alg.FindEarliestFeasibleStartTime(operation);
                    }

                    // TODO (performance): not very efficient.
                    yield return startTimes
                        .OrderBy(pair => pair.Value)
                        .ThenBy(pair => this.alg.oldStartTimes[pair.Key])
                        .First()
                        .Key;
                }
            }

            public override void OperationScheduled(Operation operation)
            {
                this.frontalOperations.Remove(operation);
                var nextOperation = operation.NextOperation(this.alg.instance);
                if (nextOperation != null)
                {
                    this.frontalOperations.Add(nextOperation);
                }
            }
        }

        private class EarliestStartTimeOverFixedOrderPriorityRule : BasePriorityRule
        {
            private HashSet<Operation> frontalOperations;
            private Dictionary<int, List<Operation>> orderedOperationsOnMachines;
            private Dictionary<Operation, int> operationToMachineOrder;

            public EarliestStartTimeOverFixedOrderPriorityRule(GreedyEarliestStartTime alg) : base(alg)
            {
            }

            public override IEnumerable<Operation> OrderedOperations()
            {
                this.frontalOperations = new HashSet<Operation>();
                this.operationToMachineOrder = new Dictionary<Operation, int>();
                this.orderedOperationsOnMachines =
                    this.alg.oldStartTimes.GetOrderedOperationsOnMachines(this.alg.instance);
                foreach (var orderedOperationsOnMachine in this.orderedOperationsOnMachines.Values)
                {
                    if (orderedOperationsOnMachine.Any())
                    {
                        var firstOperation = orderedOperationsOnMachine.First();
                        if (firstOperation.Index == 0)
                        {
                            this.frontalOperations.Add(firstOperation);
                        }
                    }

                    var machineOrder = 0;
                    foreach (var operation in orderedOperationsOnMachine)
                    {
                        this.operationToMachineOrder[operation] = machineOrder;
                        machineOrder++;
                    }
                }

                while (this.frontalOperations.Any())
                {
                    var startTimes = new StartTimes();
                    foreach (var operation in this.frontalOperations)
                    {
                        startTimes[operation] = this.alg.FindEarliestFeasibleStartTime(operation);
                    }

                    // TODO (performance): not very efficient.
                    yield return startTimes
                        .OrderBy(pair => pair.Value)
                        .ThenBy(pair => this.alg.oldStartTimes[pair.Key])
                        .First()
                        .Key;
                }
            }

            public override void OperationScheduled(Operation operation)
            {
                this.frontalOperations.Remove(operation);

                // Next operation in job becomes frontal?
                {
                    var jobSuccessor = operation.NextOperation(this.alg.instance);
                    if (jobSuccessor != null)
                    {
                        var jobSuccessorMachineOrder = this.operationToMachineOrder[jobSuccessor];
                        if (jobSuccessorMachineOrder == 0 || this.alg.newStartTimes.ContainsOperation(
                                this.orderedOperationsOnMachines[jobSuccessor.MachineIndex][
                                    jobSuccessorMachineOrder - 1]))
                        {
                            this.frontalOperations.Add(jobSuccessor);
                        }
                    }
                }

                // Next operation on the same machine becomes frontal?
                {
                    var machineSuccessorMachineOrder = this.operationToMachineOrder[operation] + 1;
                    if (machineSuccessorMachineOrder < this.orderedOperationsOnMachines[operation.MachineIndex].Count())
                    {
                        var machineSuccessor =
                            this.orderedOperationsOnMachines[operation.MachineIndex][machineSuccessorMachineOrder];
                        if (machineSuccessor.Index == 0 ||
                            (this.alg.newStartTimes.ContainsOperation(
                                machineSuccessor.PreviousOperation(this.alg.instance))))
                        {
                            this.frontalOperations.Add(machineSuccessor);
                        }
                    }
                }
            }
        }

        [Serializable]
        public class CannotRepairException : Exception
        {
            public CannotRepairException()
            {
            }

            public CannotRepairException(string message) : base(message)
            {
            }

            public CannotRepairException(string message, Exception inner) : base(message, inner)
            {
            }

            protected CannotRepairException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
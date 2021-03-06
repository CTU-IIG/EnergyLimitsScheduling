// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="StartTimes.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using System.Collections;
    using System.Linq;
    using System.Collections.Generic;
    using Iirc.EnergyLimitsScheduling.Shared.Input;

    public class StartTimes : IEnumerable<KeyValuePair<Operation, double>>
    {
        private Dictionary<Operation, double> operationToStartTime;

        private double makespan;

        private bool dirty;

        public double Makespan
        {
            get
            {
                if (this.dirty)
                {
                    this.makespan = this.operationToStartTime.Any() ?
                        this.operationToStartTime.Max(pair => pair.Value + pair.Key.ProcessingTime)
                        : 0;
                    this.dirty = false;
                }

                return this.makespan;
            }
        }

        public StartTimes()
        {
            this.operationToStartTime = new Dictionary<Operation, double>();
            this.dirty = true;
            this.makespan = 0.0;
        }

        public StartTimes(Instance instance, List<IndexedStartTime> indexedStartTimes) : this()
        {
            if (indexedStartTimes == null)
            {
                return;
            }

            foreach (var indexedStartTime in indexedStartTimes)
            {
                var operation = instance.Jobs[indexedStartTime.JobIndex].Operations[indexedStartTime.OperationIndex];
                this.operationToStartTime[operation] = indexedStartTime.StartTime;
            }
        }

        public bool ContainsOperation(Operation operation)
        {
            return this.operationToStartTime.ContainsKey(operation);
        }

        public List<IndexedStartTime> ToIndexedStartTimes()
        {
            return this.operationToStartTime
                .Select(pair => new IndexedStartTime
                {
                    JobIndex = pair.Key.JobIndex,
                    OperationIndex = pair.Key.Index,
                    StartTime = pair.Value
                })
                .ToList();
        }

        public Dictionary<int, List<Operation>> GetOrderedOperationsOnMachines(Instance instance)
        {
            return this.operationToStartTime
                .GroupBy(pair => pair.Key.MachineIndex)
                .ToDictionary(
                    machinePairs => machinePairs.Key,
                    machinePairs => machinePairs.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToList());
        }

        IEnumerator<KeyValuePair<Operation, double>> IEnumerable<KeyValuePair<Operation, double>>.GetEnumerator()
        {
            return this.operationToStartTime.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.operationToStartTime.GetEnumerator();
        }

        public double this[Operation operation]
        {
            get { return this.operationToStartTime[operation]; }
            set
            {
                 this.operationToStartTime[operation] = value;
                 this.dirty = true;
            }
        }

        /// <summary>
        /// A start time of an operation.
        /// </summary>
        public struct IndexedStartTime
        {
            /// <summary>
            /// Gets or sets the index of a job of the operation.
            /// </summary>
            public int JobIndex { get; set; }
            
            /// <summary>
            /// Gets or sets the index of the operation.
            /// </summary>
            public int OperationIndex { get; set; }
            
            /// <summary>
            /// Gets or sets the start time of the operation.
            /// </summary>
            public double StartTime { get; set; }
        }
        
        /// <summary>
        /// A range of start times for an operation.
        /// </summary>
        public struct IndexedStartTimeRange
        {
            /// <summary>
            /// Gets or sets the index of a job of the operation.
            /// </summary>
            public int JobIndex { get; set; }
            
            /// <summary>
            /// Gets or sets the index of the operation.
            /// </summary>
            public int OperationIndex { get; set; }
            
            /// <summary>
            /// Gets or sets the minimum start time.
            /// </summary>
            public double StartTimeFrom { get; set; }
            
            /// <summary>
            /// Gets or sets the maximum start time.
            /// </summary>
            public double StartTimeTo { get; set; }
        }
    }
}
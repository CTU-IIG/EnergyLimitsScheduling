// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="BranchAndBoundNonZeroOverlap.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using Gurobi;
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.Utils.Collections;
    using Iirc.Utils.Gurobi;
    using Iirc.Utils.Math;
    using Iirc.Utils.SolverFoundations;
    using Iirc.Utils.Time;

    public class BranchAndBoundNonZeroOverlap : BaseSolver<BranchAndBoundNonZeroOverlap.SpecializedSolverConfig>
    {
        private MilpOverlaps milpOverlaps;

        private NumericComparer comparer;

        private GlobalInfo globalInfo;

        private int numNodes;

        private bool timeLimitReached;

        protected override StartTimes GetStartTimes()
        {
            return this.globalInfo.BestSolution;
        }

        protected override Status Solve()
        {
            this.milpOverlaps = new MilpOverlaps();
            this.comparer = NumericComparer.Default;
            this.timeLimitReached = false;
            this.globalInfo = new GlobalInfo(comparer);
            this.numNodes = 0;

            this.SetInitStartTimes();

            this.OperationBranchingNodeEntered(new PartialSolution(this.instance), 0.0);

            if (this.TimeLimitReached())
            {
                return this.globalInfo.HasSolution ? Status.Heuristic : Status.NoSolution;
            }
            else
            {
                return this.globalInfo.HasSolution ? Status.Optimal : Status.Infeasible;
            }
        }

        protected override bool TimeLimitReached()
        {
            return this.timeLimitReached;
        }

        private void SetInitStartTimes()
        {
            if (this.solverConfig.InitStartTimes == null)
            {
                return;
            }

            this.globalInfo.BestSolution = new StartTimes(this.instance, this.solverConfig.InitStartTimes);
        }

        private void OperationBranchingNodeEntered(
            PartialSolution partialSolution,
            double makespanLowerBound)
        {
            this.numNodes++;

            if (partialSolution.AllOperationsAllocated)
            {
                this.LeafNodeEntered(partialSolution, makespanLowerBound);
                return;
            }

            // Is fixed assignment to metering intervals feasible?
            this.milpOverlaps.Solve(
                this.instance,
                partialSolution.OperationToNonZeroOverlapMeteringIntervals,
                true,
                true,
                this.RemainingTime,
                makespanLowerBound,
                this.globalInfo.MakespanUpperBound);

            switch (this.milpOverlaps.Status)
            {
                case Status.Infeasible:
                    return;

                case Status.NoSolution:
                    if (this.milpOverlaps.TimeLimitReached)
                    {
                        this.timeLimitReached = true;
                        return;
                    }
                    else
                    {
                        Debug.Assert(this.milpOverlaps.MakespanUpperBoundReached);
                        return;
                    }

                default:
                    Debug.Assert(this.milpOverlaps.Status == Status.Optimal);
                    break;
            }

            // Compute lower bound.
            this.milpOverlaps.Solve(
                this.instance,
                partialSolution.OperationToNonZeroOverlapMeteringIntervals,
                true,
                false,
                this.RemainingTime,
                makespanLowerBound,
                this.globalInfo.MakespanUpperBound);

            switch (this.milpOverlaps.Status)
            {
                case Status.Infeasible:
                    return;

                case Status.NoSolution:
                    if (this.milpOverlaps.TimeLimitReached)
                    {
                        this.timeLimitReached = true;
                        return;
                    }
                    else
                    {
                        Debug.Assert(this.milpOverlaps.MakespanUpperBoundReached);
                        return;
                    }

                default:
                    Debug.Assert(this.milpOverlaps.Status == Status.Optimal);
                    break;
            }

            // Bounding.
            makespanLowerBound = this.milpOverlaps.Objective;

            if (this.solverConfig.ContinuousStartTimes == false)
            {
                if (this.comparer.Greater(makespanLowerBound, Math.Floor(makespanLowerBound)))
                {
                    makespanLowerBound = Math.Ceiling(makespanLowerBound);
                }
            }

            if (this.globalInfo.NotBetter(makespanLowerBound))
            {
                return;
            }

            foreach (var branchingOperation in this.SelectBranchingOperations(partialSolution))
            {
                var machineOrderedOperations = partialSolution.MachineIndexToOrderedOperations[branchingOperation.MachineIndex];

                // TODO: take into account symmetries from non-spanable operations.
                var firstAllocableMeteringInterval = machineOrderedOperations.Any() ?
                    partialSolution.OperationToNonZeroOverlapMeteringIntervals[machineOrderedOperations.Peek()].LastMeteringIntervalIndex
                    : 0;

                var maxIntervals = branchingOperation.MaxNumNonZeroOvelapIntervals(this.instance);
                var branchingOperationMeteringIntervals = new MeteringIntervalsSubset(
                    firstAllocableMeteringInterval,
                    firstAllocableMeteringInterval + maxIntervals - 1,
                    this.instance.LengthMeteringInterval);
                // TODO: either last or best solution
                while ((branchingOperationMeteringIntervals.FirstMeteringIntervalIndex + maxIntervals - 1) <= this.instance.LastMeteringIntervalIndex())
                {
                    if ((this.instance.MeteringIntervalStart(branchingOperationMeteringIntervals.LastMeteringIntervalIndex)
                        + partialSolution.MachineToRemainingProcessingTime[branchingOperation.MachineIndex] - branchingOperation.ProcessingTime) > this.instance.Horizon)
                    {
                        break;
                    }

                    for (var lastDecrement = 1; lastDecrement >= 0; lastDecrement--)
                    {
                        branchingOperationMeteringIntervals.LastMeteringIntervalIndex =
                            branchingOperationMeteringIntervals.FirstMeteringIntervalIndex + maxIntervals - 1 - lastDecrement;
                        Console.WriteLine($"n={this.numNodes} id={branchingOperation.Id} p={branchingOperation.ProcessingTime} lb={makespanLowerBound} ub={this.globalInfo.MakespanUpperBound} rem={partialSolution.RemainingOperations.Count()} f={branchingOperationMeteringIntervals.FirstMeteringIntervalIndex} l={branchingOperationMeteringIntervals.LastMeteringIntervalIndex}");

                        if (partialSolution.Push(branchingOperation, branchingOperationMeteringIntervals) == false)
                        {
                            this.OperationBranchingNodeEntered(partialSolution, makespanLowerBound);
                        }
                        partialSolution.Pop(branchingOperation.MachineIndex);

                        if (this.TimeLimitReached())
                        {
                            return;
                        }
                    }

                    branchingOperationMeteringIntervals.FirstMeteringIntervalIndex++;
                }
            }
        }

        private IEnumerable<Operation> SelectBranchingOperations(PartialSolution partialSolution)
        {
            /*
            // Find earliest start time machine index.
            var selectedMachineIndex = partialSolution.MachineIndexToOrderedOperations
                .IndexMin(machineOrderedOperations => machineOrderedOperations.Any() ?
                    partialSolution.OperationToNonZeroOverlapMeteringIntervals[machineOrderedOperations.Peek()].LastMeteringIntervalIndex
                    : 0);

            */
            var selectedMachineIndex = -1;
            foreach (var machineIndex in this.instance.Machines())
            {
                if (partialSolution.MachineToRemainingProcessingTime[machineIndex] > 0)
                {
                    selectedMachineIndex = machineIndex;
                    break;
                }
            }

            return partialSolution.RemainingOperations.Intersect(this.instance.MachineOperations(selectedMachineIndex)).ToArray();
        }

        private void LeafNodeEntered(
            PartialSolution partialSolution,
            double makespanLowerBound)
        {
            if (this.globalInfo.NotBetter(makespanLowerBound))
            {
                return;
            }

            this.milpOverlaps.Solve(
                this.instance,
                partialSolution.OperationToNonZeroOverlapMeteringIntervals,
                this.solverConfig.ContinuousStartTimes,
                true,
                this.RemainingTime,
                makespanLowerBound,
                this.globalInfo.MakespanUpperBound);

            switch (this.milpOverlaps.Status)
            {
                case Status.Optimal:
                    if (this.globalInfo.IsBetter(this.milpOverlaps.StartTimes))
                    {
                        this.globalInfo.BestSolution = this.milpOverlaps.StartTimes;
                        Console.WriteLine($"Found new best solution: {this.globalInfo.BestSolution.Makespan}");
                    }
                    break;

                case Status.Infeasible:
                    break;

                default:
                    Debug.Assert(this.milpOverlaps.Status == Status.NoSolution);

                    if (this.milpOverlaps.TimeLimitReached)
                    {
                        this.timeLimitReached = true;
                    }
                    else
                    {
                        Debug.Assert(this.milpOverlaps.MakespanUpperBoundReached);
                    }

                    break;
            }
        }

        private class GlobalInfo
        {
            private NumericComparer comparer;

            public GlobalInfo(NumericComparer comparer)
            {
                this.comparer = comparer;
            }

            public StartTimes BestSolution { get; set; }

            public double? MakespanUpperBound
            {
                get
                {
                    return this.BestSolution == null ? (double?)null : this.BestSolution.Makespan;
                }
            }

            public bool HasSolution
            {
                get
                {
                    return this.BestSolution != null;
                }
            }

            public bool NotBetter(double value)
            {
                if (this.BestSolution == null)
                {
                    return false;
                }

                return this.comparer.LessOrEqual(this.BestSolution.Makespan, value);
            }

            public bool IsBetter(StartTimes newSolution)
            {
                if (this.BestSolution == null)
                {
                    return true;
                }

                return this.comparer.Greater(this.BestSolution.Makespan, newSolution.Makespan);
            }
        }

        public class SpecializedSolverConfig
        {
        }

        public class PartialSolution
        {
            private readonly Instance instance;

            private readonly NumericComparer comparer;

            public PartialSolution(Instance instance)
            {
                this.instance = instance;
                this.comparer = NumericComparer.Default;

                this.MachineIndexToOrderedOperations =
                    Enumerable.Range(0, this.instance.NumMachines).Select(_ => new Stack<Operation>()).ToArray();
                this.OperationToNonZeroOverlapMeteringIntervals = new Dictionary<Operation, MeteringIntervalsSubset>();
                this.RemainingOperations = new HashSet<Operation>(instance.AllOperations());

                this.MachineToRemainingProcessingTime = Enumerable.Repeat(0, this.instance.NumMachines).ToArray();
                foreach (var operation in this.instance.AllOperations())
                {
                    this.MachineToRemainingProcessingTime[operation.MachineIndex] += operation.ProcessingTime;
                }

                this.MachineToAvailableTimes = Enumerable
                    .Range(0, this.instance.NumMachines)
                    .Select(_ => Enumerable.Repeat(this.instance.LengthMeteringInterval, this.instance.NumMeteringIntervals).ToArray())
                    .ToArray();

                this.MeteringIntervalToRemainingEnergies = Enumerable.Repeat(this.instance.EnergyLimit, this.instance.NumMeteringIntervals).ToArray();
            }

            public Stack<Operation>[] MachineIndexToOrderedOperations { get; private set; }

            public IDictionary<Operation, MeteringIntervalsSubset> OperationToNonZeroOverlapMeteringIntervals { get; private set; }

            public ISet<Operation> RemainingOperations { get; private set; }

            public int[] MachineToRemainingProcessingTime { get; private set; }

            public int[][] MachineToAvailableTimes { get; private set; }

            public double[] MeteringIntervalToRemainingEnergies { get; private set; }

            public bool AllOperationsAllocated
            {
                get
                {
                    return !this.RemainingOperations.Any();
                }
            }

            public bool Push(Operation operation, MeteringIntervalsSubset meteringIntervalsSubset)
            {
                var violating = false;

                this.MachineIndexToOrderedOperations[operation.MachineIndex].Push(operation);
                this.OperationToNonZeroOverlapMeteringIntervals[operation] = meteringIntervalsSubset;
                this.RemainingOperations.Remove(operation);
                this.MachineToRemainingProcessingTime[operation.MachineIndex] -= operation.ProcessingTime;

                if (meteringIntervalsSubset.Count == 1)
                {
                    this.MachineToAvailableTimes[operation.MachineIndex][meteringIntervalsSubset.FirstMeteringIntervalIndex] -=
                        operation.ProcessingTime;
                    this.MeteringIntervalToRemainingEnergies[meteringIntervalsSubset.FirstMeteringIntervalIndex] -= 
                        operation.ProcessingTime * operation.PowerConsumption;

                    if (this.MachineToAvailableTimes[operation.MachineIndex][meteringIntervalsSubset.FirstMeteringIntervalIndex] < 0)
                    {
                        violating = true;
                    }

                    if (this.comparer.Less(this.MeteringIntervalToRemainingEnergies[meteringIntervalsSubset.FirstMeteringIntervalIndex], 0.0))
                    {
                        violating = true;
                    }
                }
                else if (meteringIntervalsSubset.Count >= 3)
                {
                    foreach (var meteringIntervalIndex in meteringIntervalsSubset.SkipFirst().SkipLast())
                    {
                        this.MachineToAvailableTimes[operation.MachineIndex][meteringIntervalsSubset.FirstMeteringIntervalIndex] -=
                            this.instance.LengthMeteringInterval;
                        this.MeteringIntervalToRemainingEnergies[meteringIntervalsSubset.FirstMeteringIntervalIndex] -= 
                            this.instance.LengthMeteringInterval * operation.PowerConsumption;

                        if (this.MachineToAvailableTimes[operation.MachineIndex][meteringIntervalIndex] < 0)
                        {
                            violating = true;
                        }

                        if (this.comparer.Less(this.MeteringIntervalToRemainingEnergies[meteringIntervalIndex], 0.0))
                        {
                            violating = true;
                        }
                    }
                }

                return violating;
            }

            public void Pop(int machineIndex)
            {
                var operation = this.MachineIndexToOrderedOperations[machineIndex].Pop();
                var meteringIntervalsSubset = this.OperationToNonZeroOverlapMeteringIntervals[operation];

                this.OperationToNonZeroOverlapMeteringIntervals.Remove(operation);
                this.RemainingOperations.Add(operation);
                this.MachineToRemainingProcessingTime[operation.MachineIndex] += operation.ProcessingTime;

                if (meteringIntervalsSubset.Count == 1)
                {
                    this.MachineToAvailableTimes[operation.MachineIndex][meteringIntervalsSubset.FirstMeteringIntervalIndex] +=
                        operation.ProcessingTime;
                    this.MeteringIntervalToRemainingEnergies[meteringIntervalsSubset.FirstMeteringIntervalIndex] += 
                        operation.ProcessingTime * operation.PowerConsumption;
                }
                else if (meteringIntervalsSubset.Count >= 3)
                {
                    foreach (var meteringIntervalIndex in meteringIntervalsSubset.SkipFirst().SkipLast())
                    {
                        this.MachineToAvailableTimes[operation.MachineIndex][meteringIntervalsSubset.FirstMeteringIntervalIndex] +=
                            this.instance.LengthMeteringInterval;
                        this.MeteringIntervalToRemainingEnergies[meteringIntervalsSubset.FirstMeteringIntervalIndex] += 
                            this.instance.LengthMeteringInterval * operation.PowerConsumption;
                    }
                }
            }
        }
    }

    public class MilpOverlaps
    {
        private Instance ins;

        private IDictionary<Operation, MeteringIntervalsSubset> operationToNonZeroOverlapMeteringIntervals;

        private bool continuousOverlaps;

        private bool onlyFixedOperations;

        protected GRBEnv env;

        protected GRBModel model;

        protected MeteringIntervalsSubset modelMeteringIntervals;

        private Variables vars;

        private Dictionary<Operation, int> operationToRemainingProcessingTime;

        private MeteringIntervalsSubset[] machineToAllocableMeteringIntervals;

        private List<int>[] availableTimes;

        private List<double> availableEnergy;
        
        private ISet<Operation> consideredOperations;

        private double? makespanLowerBound;
        private double? makespanUpperBound;

        private Timer timer;

        public void Solve(
            Instance ins,
            IDictionary<Operation, MeteringIntervalsSubset> operationToNonZeroOverlapMeteringIntervals,
            bool continuousOverlaps,
            bool onlyFixedOperations = false,
            TimeSpan? timeLimit = null,
            double? makespanLowerBound = null,
            double? makespanUpperBound = null)
        {
            this.timer = new Timer(timeLimit);
            this.timer.Start();

            this.Status = Status.NoSolution;
            this.StartTimes = null;
            this.TimeLimitReached = false;

            this.ins = ins;
            this.operationToNonZeroOverlapMeteringIntervals = operationToNonZeroOverlapMeteringIntervals;
            this.continuousOverlaps = continuousOverlaps;
            this.makespanLowerBound = makespanLowerBound;
            this.makespanUpperBound = makespanUpperBound;
            this.onlyFixedOperations = onlyFixedOperations;

            if (this.onlyFixedOperations)
            {
                this.consideredOperations = new HashSet<Operation>(this.operationToNonZeroOverlapMeteringIntervals.Keys);
            }
            else
            {
                this.consideredOperations = new HashSet<Operation>(this.ins.AllOperations());
            }

            this.ComputeInitialMeteringIntervals();

            this.Prepare();
            if (this.Status == Status.Infeasible)
            {
                return;
            }

            this.env = new GRBEnv();
            this.model = new GRBModel(this.env);
            this.SetModelParameters();

            this.CreateVariables();
            this.CreateConstraints();
            this.CreateObjective();

            this.Status = this.IterativeSolve();

            if (this.operationToNonZeroOverlapMeteringIntervals.Count == this.ins.TotalNumOperations()
                && (this.Status == Status.Optimal || this.Status == Status.Heuristic))
            {
                this.StartTimes = this.GetStartTimes();
            }

            this.Cleanup();
        }

        private void ComputeInitialMeteringIntervals()
        {
            var firstAllocableMeteringIntervalIndices = Enumerable.Repeat(0, this.ins.NumMachines).ToArray();

            foreach (var (operation, nonZeroOverlapMeteringIntervals) in this.operationToNonZeroOverlapMeteringIntervals)
            {
                firstAllocableMeteringIntervalIndices[operation.MachineIndex] = Math.Max(
                    firstAllocableMeteringIntervalIndices[operation.MachineIndex],
                    nonZeroOverlapMeteringIntervals.LastMeteringIntervalIndex);
            }

            var machineToRemainingProcessingTime = Enumerable.Repeat(0, this.ins.NumMachines).ToArray();
            foreach (var operation in this.consideredOperations)
            {
                if (this.operationToNonZeroOverlapMeteringIntervals.ContainsKey(operation) == false)
                {
                    machineToRemainingProcessingTime[operation.MachineIndex] += operation.ProcessingTime;
                }
            }

            int lastMeteringIntervalIndex = -1;
            foreach (var machineIndex in this.ins.Machines())
            {
                var first = firstAllocableMeteringIntervalIndices[machineIndex];
                var last = machineToRemainingProcessingTime[machineIndex] == 0 ?
                    first
                    : (this.ins.MeteringIntervalStart(first) + machineToRemainingProcessingTime[machineIndex]) / this.ins.LengthMeteringInterval;
                
                lastMeteringIntervalIndex = Math.Max(lastMeteringIntervalIndex, last);
            }

            if (this.makespanLowerBound.HasValue)
            {
                lastMeteringIntervalIndex = Math.Max(
                    lastMeteringIntervalIndex,
                    (int)Math.Ceiling(((int)this.makespanLowerBound.Value) / ((double)this.ins.LengthMeteringInterval)) - 1);
            }
            lastMeteringIntervalIndex = Math.Min(this.ins.LastMeteringIntervalIndex(), lastMeteringIntervalIndex);

            this.machineToAllocableMeteringIntervals = firstAllocableMeteringIntervalIndices
                .Select(firstAllocableMeteringIntervalIndex => new MeteringIntervalsSubset(
                    firstAllocableMeteringIntervalIndex,
                    lastMeteringIntervalIndex,
                    this.ins.LengthMeteringInterval))
                .ToArray();

            this.modelMeteringIntervals =
                new MeteringIntervalsSubset(0, lastMeteringIntervalIndex, this.ins.LengthMeteringInterval);
            this.availableTimes = this.ins.Machines()
                .Select(_ => Enumerable.Repeat(
                    this.ins.LengthMeteringInterval,
                    this.modelMeteringIntervals.Count).ToList())
                .ToArray();
            this.availableEnergy = Enumerable.Repeat(
                this.ins.EnergyLimit,
                this.modelMeteringIntervals.Count)
                .ToList();
        }

        private void Prepare()
        {
            var comparer = NumericComparer.Default;

            this.operationToRemainingProcessingTime = new Dictionary<Operation, int>();
            foreach (var operation in this.consideredOperations)
            {
                this.operationToRemainingProcessingTime[operation] = operation.ProcessingTime;
            }

            foreach (var (operation, nonZeroOverlapMeteringIntervals) in this.operationToNonZeroOverlapMeteringIntervals)
            {
                if (nonZeroOverlapMeteringIntervals.Count == 1)
                {
                    var meteringIntervalIndex = nonZeroOverlapMeteringIntervals.First();
                    this.availableTimes[operation.MachineIndex][meteringIntervalIndex] -= operation.ProcessingTime;
                    this.availableEnergy[meteringIntervalIndex] -= operation.ProcessingTime * operation.PowerConsumption;
                    this.operationToRemainingProcessingTime[operation] = 0;
                }
                else if (nonZeroOverlapMeteringIntervals.Count == 2)
                {
                    // NOP, cannot determine overlaps.
                }
                else if (nonZeroOverlapMeteringIntervals.Count >= 3)
                {
                    foreach (var meteringIntervalIndex in nonZeroOverlapMeteringIntervals.SkipFirst().SkipLast())
                    {
                        this.availableTimes[operation.MachineIndex][meteringIntervalIndex] -= this.ins.LengthMeteringInterval;
                        this.availableEnergy[meteringIntervalIndex] -= this.ins.LengthMeteringInterval * operation.PowerConsumption;
                        this.operationToRemainingProcessingTime[operation] -= this.ins.LengthMeteringInterval;
                    }
                }
            }

            // Check violation of energy limits.
            for (var meteringIntervalIndex = 0; meteringIntervalIndex < this.availableEnergy.Count; meteringIntervalIndex++)
            {
                if (comparer.Less(this.availableEnergy[meteringIntervalIndex], 0.0))
                {
                    this.Status = Status.Infeasible;
                    return;
                }
                else
                {
                    this.availableEnergy[meteringIntervalIndex] = Math.Max(0.0, this.availableEnergy[meteringIntervalIndex]);
                }
            }
        }

        private Status IterativeSolve()
        {
            var numericComparer = NumericComparer.Default;

            while (true)
            {
                if (this.makespanUpperBound.HasValue
                    && numericComparer.LessOrEqual(this.makespanLowerBound.Value, this.ins.MeteringIntervalStart(this.modelMeteringIntervals.LastMeteringIntervalIndex)))
                {
                    this.MakespanUpperBoundReached = true;
                }

                if (this.timer.RemainingTime.HasValue)
                {
                    this.model.Parameters.TimeLimit = this.timer.RemainingTime.Value.TotalSeconds;
                }

                Debug.Assert(this.modelMeteringIntervals.LastMeteringIntervalIndex <= this.ins.LastMeteringIntervalIndex());

                this.model.Optimize();
                var status = this.model.GetResultStatus();

                if (status == Status.Infeasible)
                {
                    if (this.onlyFixedOperations)
                    {
                        return Status.Infeasible;
                    }
                    else
                    {
                        if (this.modelMeteringIntervals.LastMeteringIntervalIndex == this.ins.LastMeteringIntervalIndex())
                        {
                            return Status.Infeasible;
                        }
                        else
                        {
                            this.AppendMeteringInterval();
                        }
                    }
                }
                else if (status == Status.Optimal)
                {
                    this.Objective = this.model.ObjVal + this.ins.MeteringIntervalStart(this.modelMeteringIntervals.LastMeteringIntervalIndex);
                    if (this.makespanUpperBound.HasValue
                        && numericComparer.LessOrEqual(this.makespanLowerBound.Value, this.Objective))
                    {
                        this.MakespanUpperBoundReached = true;
                    }

                    return Status.Optimal;
                }
                else
                {
                    Debug.Assert(status == Status.NoSolution);
                    Debug.Assert(this.model.TimeLimitReached());
                    this.TimeLimitReached = true;
                    return Status.NoSolution;
                }
            }
        }

        private void AppendMeteringInterval()
        {
            // TODO (performance): better way then just recreating the model?
            this.model.Remove(this.model.GetVars());
            this.model.Remove(this.model.GetConstrs());

            this.modelMeteringIntervals.LastMeteringIntervalIndex += 1;
            foreach (var machineAllocableMeteringIntervals in this.machineToAllocableMeteringIntervals)
            {
                machineAllocableMeteringIntervals.LastMeteringIntervalIndex =
                    this.modelMeteringIntervals.LastMeteringIntervalIndex;
            }

            this.availableEnergy.Add(this.ins.EnergyLimit);
            foreach (var machineAvailableTimes in this.availableTimes)
            {
                machineAvailableTimes.Add(this.ins.LengthMeteringInterval);
            }

            this.CreateVariables();
            this.CreateConstraints();
            this.CreateObjective();
        }

        private void CreateVariables()
        {
            this.vars = new Variables();

            this.vars.Obj = this.model.AddVar(0, GRB.INFINITY, 0,
                this.continuousOverlaps ? GRB.CONTINUOUS : GRB.INTEGER, "obj");

            this.vars.Overlaps = new Dictionary<Operation, Dictionary<int, GRBVar>>();
            foreach (var operation in this.consideredOperations)
            {
                var meteringIntervalIndices = Enumerable.Empty<int>();
                if (this.operationToNonZeroOverlapMeteringIntervals.TryGetValue(operation, out var nonZeroOverlapMeteringIntervals))
                {
                    if (nonZeroOverlapMeteringIntervals.Count <= 2)
                    {
                        meteringIntervalIndices = nonZeroOverlapMeteringIntervals.AsEnumerable();
                    }
                    else if (nonZeroOverlapMeteringIntervals.Count >= 3)
                    {
                        meteringIntervalIndices = new int[]
                        {
                            nonZeroOverlapMeteringIntervals.FirstMeteringIntervalIndex,
                            nonZeroOverlapMeteringIntervals.LastMeteringIntervalIndex
                        };
                    }
                }
                else
                {
                    meteringIntervalIndices = this.machineToAllocableMeteringIntervals[operation.MachineIndex];
                }

                this.vars.Overlaps[operation] = new Dictionary<int, GRBVar>();
                foreach (var meteringIntervalIndex in meteringIntervalIndices)
                {
                    this.vars.Overlaps[operation][meteringIntervalIndex] = this.model.AddVar(
                        0, GRB.INFINITY, 0, this.continuousOverlaps ? GRB.CONTINUOUS : GRB.INTEGER, $"overlap_{operation.Id}{meteringIntervalIndex}");
                }
            }
        }

        private void CreateConstraints()
        {
            foreach (var machineIndex in this.ins.Machines())
            {
                var machineOperations = this.consideredOperations.Intersect(this.ins.MachineOperations(machineIndex));

                if (machineOperations.Any())
                {
                    var overlaps = machineOperations
                        .Where(operation => this.vars.Overlaps[operation].ContainsKey(this.modelMeteringIntervals.LastMeteringIntervalIndex))
                        .Select(operation => this.vars.Overlaps[operation][this.modelMeteringIntervals.LastMeteringIntervalIndex]);

                    this.model.AddConstr(
                        overlaps.Quicksum()
                        + (this.ins.LengthMeteringInterval - this.availableTimes[machineIndex][this.modelMeteringIntervals.LastMeteringIntervalIndex])
                        <=
                        this.vars.Obj,
                        $"obj_{machineIndex}");
                }
            }

            foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
            {
                var overlapsTimesPowerConsumption = this.consideredOperations
                    .Where(operation => this.vars.Overlaps[operation].ContainsKey(meteringIntervalIndex))
                    .Select(operation => this.vars.Overlaps[operation][meteringIntervalIndex] * operation.PowerConsumption);

                if (overlapsTimesPowerConsumption.Any())
                {
                    this.model.AddConstr(
                        overlapsTimesPowerConsumption.Quicksum()
                        <=
                        this.availableEnergy[meteringIntervalIndex],
                        $"maxEnergy_{meteringIntervalIndex}");
                }
            }

            foreach (var machineIndex in this.ins.Machines())
            {
                var machineOperations = this.consideredOperations.Intersect(this.ins.MachineOperations(machineIndex));

                if (machineOperations.Any())
                {
                    foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                    {
                        var overlaps = machineOperations
                            .Where(operation => this.vars.Overlaps[operation].ContainsKey(meteringIntervalIndex))
                            .Select(operation => this.vars.Overlaps[operation][meteringIntervalIndex]);

                        this.model.AddConstr(
                            overlaps.Quicksum()
                            <=
                            this.availableTimes[machineIndex][meteringIntervalIndex],
                            $"maxAvailableTime_{machineIndex}{meteringIntervalIndex}");
                    }
                }
            }

            foreach (var operation in this.consideredOperations)
            {
                var overlaps = this.vars.Overlaps[operation];
                if (overlaps.Any())
                {
                    this.model.AddConstr(
                        overlaps.Values.Quicksum() == this.operationToRemainingProcessingTime[operation],
                        $"totalOverlapIsRemainingProcessingTime_{operation.Id}");
                }
            }
        }

        private void CreateObjective()
        {
            this.model.SetObjective(this.vars.Obj + 0, GRB.MINIMIZE);
        }

        private void SetModelParameters()
        {
            this.model.Parameters.FeasibilityTol = NumericComparer.DefaultTolerance;
            this.model.Parameters.OutputFlag = 0;
            // this.model.Parameters.Threads = Math.Max(0, this.solverConfig.NumWorkers);
        }

        private StartTimes GetStartTimes()
        {
            var startTimes = new StartTimes();
            var comparer = NumericComparer.Default;

            var fills = this.ins.Machines()
                .Select(_ => Enumerable.Repeat(0.0, this.ins.NumMeteringIntervals).ToArray())
                .ToArray();

            // Start times should be computed using only overlap variables.

            var inOperations = new List<Operation>();
            foreach (var operation in this.consideredOperations)
            {
                var nonZeroOverlapIntervals = this.operationToNonZeroOverlapMeteringIntervals[operation];
                if (nonZeroOverlapIntervals.Count == 1)
                {
                    inOperations.Add(operation);
                }
                else
                {
                    startTimes[operation] =
                        this.ins.MeteringIntervalEnd(nonZeroOverlapIntervals.FirstMeteringIntervalIndex) -
                        this.vars.Overlaps[operation][nonZeroOverlapIntervals.FirstMeteringIntervalIndex].ToDouble();
                    fills[operation.MachineIndex][nonZeroOverlapIntervals.LastMeteringIntervalIndex] +=
                        this.vars.Overlaps[operation][nonZeroOverlapIntervals.LastMeteringIntervalIndex].ToDouble();
                }
            }

            foreach (var operation in inOperations)
            {
                var nonZeroMeteringInterval = this.operationToNonZeroOverlapMeteringIntervals[operation].FirstMeteringIntervalIndex;
                startTimes[operation] =
                    this.ins.MeteringIntervalStart(nonZeroMeteringInterval)
                    + fills[operation.MachineIndex][nonZeroMeteringInterval];
                fills[operation.MachineIndex][nonZeroMeteringInterval] += operation.ProcessingTime;
            }

            Debug.Assert(comparer.AreEqual(this.Objective, startTimes.Makespan));

            return startTimes;
        }

        private void Cleanup()
        {
            this.model.Dispose();
            this.env.Dispose();
            this.timer.Stop();
        }

        public double Objective { get; private set; }

        public Status Status { get; private set; }

        public StartTimes StartTimes { get; private set; }

        public bool MakespanUpperBoundReached { get; private set; }

        public bool TimeLimitReached { get; private set; }

        public class Variables
        {
            public GRBVar Obj { get; set; }

            public Dictionary<Operation, Dictionary<int, GRBVar>> Overlaps { get; set; }
        }
    }
}
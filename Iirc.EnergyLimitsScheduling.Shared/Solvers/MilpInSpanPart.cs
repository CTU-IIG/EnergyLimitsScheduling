namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using Gurobi;
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.Utils.Collections;
    using Iirc.Utils.Gurobi;
    using Iirc.Utils.Math;
    using Iirc.Utils.SolverFoundations;

    public class MilpInSpanPart : BaseSolver<MilpInSpanPart.SpecializedSolverConfig>
    {
        protected GRBEnv env;

        protected GRBModel model;

        protected MeteringIntervalsSubset modelMeteringIntervals;

        private Variables vars;

        protected override Status Solve()
        {
            this.env = new GRBEnv();
            this.model = new GRBModel(this.env);

            this.SetModelParameters();

            switch (this.specializedSolverConfig.HorizonOptimizationStrategy)
            {
                case HorizonOptimizationStrategy.Whole:
                    return this.WholeHorizonOptimizationStrategy();

                case HorizonOptimizationStrategy.Decreasing:
                    return this.DecreasingOptimizationStrategy();

                default:
                    throw new ArgumentException($"Invalid optimization strategy {this.specializedSolverConfig.HorizonOptimizationStrategy}");
            }
        }

        private Status WholeHorizonOptimizationStrategy()
        {
            this.modelMeteringIntervals = new MeteringIntervalsSubset(
                this.instance.FirstMeteringIntervalIndex(),
                this.instance.LastMeteringIntervalIndex(),
                this.instance.LengthMeteringInterval);
            this.CreateVariables();
            this.CreateConstraints();
            this.CreateObjective();
            this.SetInitStartTimes();
            this.model.SetTimeLimit(this.RemainingTime);
            this.model.Optimize();

            return this.model.GetResultStatus();
        }

        private Status DecreasingOptimizationStrategy()
        {
            var comparer = NumericComparer.Default;

            StartTimes initStartTimes = null;
            var numMeteringIntervals = this.instance.NumMeteringIntervals;
            if (this.solverConfig.InitStartTimes != null)
            {
                initStartTimes = new StartTimes(this.instance, this.solverConfig.InitStartTimes);
                numMeteringIntervals = (int)(((int)Math.Round(initStartTimes.Makespan) - 1) / this.instance.LengthMeteringInterval) + 1;
            }

            this.modelMeteringIntervals = new MeteringIntervalsSubset(
                this.instance.FirstMeteringIntervalIndex(),
                initStartTimes == null ?
                    this.instance.LastMeteringIntervalIndex()
                    : Math.Min(
                        this.instance.LastMeteringIntervalIndex(),
                        this.instance.FirstMeteringIntervalIndex() + numMeteringIntervals - 1),
                this.instance.LengthMeteringInterval);
            Status? previousStatus = null;
            while (true)
            {
                Console.WriteLine($"Current horizon: 0 - {this.modelMeteringIntervals.Horizon}");
                this.CreateVariables();
                this.CreateConstraints();
                this.CreateObjective();
                this.SetInitStartTimes(initStartTimes);
                this.model.SetTimeLimit(this.RemainingTime);
                this.model.Optimize();

                var resultStatus = this.model.GetResultStatus();

                if (resultStatus == Status.Optimal)
                {
                    if (comparer.Greater(this.model.ObjVal, 0.0))
                    {
                        // i.e. LB>0
                        return Status.Optimal;
                    }
                }
                else if (resultStatus == Status.Heuristic)
                {
                    Debug.Assert(this.TimeLimitReached());
                    return Status.Heuristic;
                }
                else if (resultStatus == Status.NoSolution)
                {
                    Debug.Assert(this.TimeLimitReached());

                    // In case of time out, gurobi should at least return the start solution if set.
                    Debug.Assert(previousStatus.HasValue == false);
                    return Status.NoSolution;
                }
                else if (resultStatus == Status.Infeasible)
                {
                    return Status.Infeasible;
                }

                Debug.Assert(resultStatus == Status.Optimal);
                previousStatus = Status.Optimal;
                initStartTimes = this.GetStartTimes();

                if (this.modelMeteringIntervals.Count <= this.specializedSolverConfig.OptimizationWindow)
                {
                    return Status.Optimal;
                }

                // Decrease the considered metering intervals.
                this.modelMeteringIntervals = new MeteringIntervalsSubset(
                    this.modelMeteringIntervals.FirstMeteringIntervalIndex,
                    this.modelMeteringIntervals.LastMeteringIntervalIndex - this.specializedSolverConfig.OptimizationWindow,
                    this.instance.LengthMeteringInterval);
            }
        }

        private void SetModelParameters()
        {
            this.model.Parameters.FeasibilityTol = NumericComparer.DefaultTolerance;
            this.model.Parameters.Threads = Math.Max(0, this.solverConfig.NumWorkers);
            if (this.specializedSolverConfig.LazyEnergyConstraints)
            {
                this.model.Parameters.LazyConstraints = 1;
            }
        }

        protected override void CheckInstanceValidity()
        {
            if (this.instance.Jobs.Any(job => job.Operations.Length >= 2))
            {
                throw new ArgumentException("Solver cannot handle instances with jobs having more than one operation.");
            }

            if (this.specializedSolverConfig.OptimizationWindow > 1
                && this.solverConfig.ContinuousStartTimes
                && this.specializedSolverConfig.BigMObjectiveConstraints == false)
            {
                throw new ArgumentException("Solver cannot handle larger optimization window than 1 if continuous start times are required.");
            }
        }

        protected override void CheckConfigValidity()
        {
            if (this.specializedSolverConfig.BigMObjectiveConstraints
                && this.specializedSolverConfig.HorizonOptimizationStrategy != HorizonOptimizationStrategy.Whole)
            {
                throw new ArgumentException($"Does not make sense to use different horizon optimization strategy than ${HorizonOptimizationStrategy.Whole} if BigM objective is used.");
            }
        }

        private void CreateVariables()
        {
            this.vars = new Variables();

            this.vars.Obj = this.model.AddVar(0, GRB.INFINITY, 0,
                this.solverConfig.ContinuousStartTimes ? GRB.CONTINUOUS : GRB.INTEGER, "obj");

            this.vars.NonZeroOverlap = new Dictionary<Operation, GRBVar[]>();

            if (this.specializedSolverConfig.OnBoundaryMaxThreeConstraint == false)
            {
                this.vars.OnBoundary = new Dictionary<Operation, GRBVar[]>();
            }

            this.vars.Overlap = new Dictionary<Operation, GRBVar[]>();
            this.vars.StartIn = new Dictionary<Operation, GRBVar[]>();


            if (this.specializedSolverConfig.BigMObjectiveConstraints)
            {
                this.vars.HasOverlappingOperation = new Dictionary<int, GRBVar[]>();
            }

            foreach (var operation in this.instance.AllOperations())
            {
                if (this.specializedSolverConfig.OnBoundaryMaxThreeConstraint == false)
                {
                    this.vars.OnBoundary[operation] = new GRBVar[this.modelMeteringIntervals.Count];
                }

                this.vars.Overlap[operation] = new GRBVar[this.modelMeteringIntervals.Count];
                this.vars.NonZeroOverlap[operation] = new GRBVar[this.modelMeteringIntervals.Count];

                if (this.IsSpannable(operation))
                {
                    this.vars.StartIn[operation] = new GRBVar[this.modelMeteringIntervals.Count];
                }

                foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                {
                    if (this.specializedSolverConfig.OnBoundaryMaxThreeConstraint == false)
                    {
                        this.vars.OnBoundary[operation][meteringIntervalIndex] =
                            this.model.AddVar(0, 1, 0, GRB.BINARY,
                                $"onBoundary_{operation.Id}{meteringIntervalIndex}");
                        this.vars.OnBoundary[operation][meteringIntervalIndex].BranchPriority = 0;
                    }

                    this.vars.Overlap[operation][meteringIntervalIndex] =
                        this.model.AddVar(
                            0, GRB.INFINITY, 0,
                            this.solverConfig.ContinuousStartTimes ? GRB.CONTINUOUS : GRB.INTEGER,
                            $"overlap_{operation.Id}{meteringIntervalIndex}");
                    this.vars.Overlap[operation][meteringIntervalIndex].BranchPriority = 0;

                    this.vars.NonZeroOverlap[operation][meteringIntervalIndex] =
                        this.model.AddVar(0, 1, 0, GRB.BINARY,
                            $"nonZeroOverlap_{operation.Id}{meteringIntervalIndex}");
                    this.vars.NonZeroOverlap[operation][meteringIntervalIndex].BranchPriority = 1000;


                   if (this.IsSpannable(operation))
                    {
                        this.vars.StartIn[operation][meteringIntervalIndex] =
                            this.model.AddVar(0, 1, 0, GRB.BINARY,
                                $"startIn_{operation.Id}{meteringIntervalIndex}");
                        this.vars.StartIn[operation][meteringIntervalIndex].BranchPriority = 2000;
                    }
                }
            }

            foreach (var machineIndex in this.instance.Machines())
            {
                if (this.specializedSolverConfig.BigMObjectiveConstraints)
                {
                    this.vars.HasOverlappingOperation[machineIndex] = new GRBVar[this.modelMeteringIntervals.Count];
                }
                
                foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                {
                    if (this.specializedSolverConfig.BigMObjectiveConstraints)
                    {
                        this.vars.HasOverlappingOperation[machineIndex][meteringIntervalIndex] = 
                            this.model.AddVar(0, 1, 0, GRB.BINARY,
                                $"hasOverlappingOperation_{machineIndex}{meteringIntervalIndex}");
                    }
                }
            }
        }

        private void CreateConstraints()
        {
            this.model.Remove(this.model.GetConstrs());
            this.model.Remove(this.model.GetSOSs());
            this.model.Update();

            var optimizationMeteringIntervals = this.GetOptimizationMeteringIntervals();

            if (this.specializedSolverConfig.BigMObjectiveConstraints)
            {
                foreach (var machineIndex in this.instance.Machines())
                {
                    var machineOperations = this.instance.MachineOperations(machineIndex).ToArray();
                    foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                    {
                        model.AddConstr(
                            meteringIntervalIndex * this.instance.LengthMeteringInterval * this.vars.HasOverlappingOperation[machineIndex][meteringIntervalIndex]
                            + machineOperations
                                .Quicksum(operation => this.vars.Overlap[operation][meteringIntervalIndex])
                            <=
                            this.vars.Obj,
                            $"obj_{machineIndex}{meteringIntervalIndex}");
                        
                        model.AddConstr(
                            machineOperations
                                .Quicksum(operation => this.vars.NonZeroOverlap[operation][meteringIntervalIndex])
                            <=
                            this.instance.LengthMeteringInterval * this.vars.HasOverlappingOperation[machineIndex][meteringIntervalIndex],
                            $"obj_{machineIndex}{meteringIntervalIndex}");
                    }
                }
            }
            else
            {
                foreach (var machineIndex in this.instance.Machines())
                {
                    var runningMultiplier = 1.0;
                    foreach (var meteringIntervalIndex in optimizationMeteringIntervals)
                    {
                        model.AddConstr(
                            runningMultiplier *
                            this.instance.MachineOperations(machineIndex)
                                .Quicksum(operation => this.vars.Overlap[operation][meteringIntervalIndex])
                            <=
                            this.vars.Obj,
                            $"obj_{machineIndex}");
                        runningMultiplier *= this.instance.LengthMeteringInterval;
                    }
                }
            }

            foreach (var machineIndex in this.instance.Machines())
            {
                foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                {
                    this.model.AddConstr(
                        this.instance.MachineOperations(machineIndex)
                            .Quicksum(operation => this.vars.Overlap[operation][meteringIntervalIndex])
                        <=
                        this.instance.LengthMeteringInterval,
                        $"totalOverlap_{meteringIntervalIndex}{machineIndex}");
                }
            }

            foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
            {
                this.model.AddConstr(
                    this.instance.AllOperations()
                        .Quicksum(operation =>
                            this.vars.Overlap[operation][meteringIntervalIndex] * operation.PowerConsumption)
                    <=
                    this.instance.EnergyLimit,
                    $"energyLimit_{meteringIntervalIndex}");
            }

            foreach (var operation in this.instance.AllOperations())
            {
                this.model.AddConstr(
                    this.modelMeteringIntervals
                        .Quicksum(meteringIntervalIndex => this.vars.Overlap[operation][meteringIntervalIndex])
                    ==
                    operation.ProcessingTime,
                    $"totalOverlapIsProcessingTime_{operation.Id}");
            }

            foreach (var operation in this.instance.AllOperations().Where(this.NotSpannable))
            {
                // Problems with SOS constraints when containing only one variable.
                // Moreover, if we have only one variable, then this constraint is already enforced by
                // totalOverlapIsProcessingTime_
                if (this.modelMeteringIntervals.Count > 1)
                {
                    var vars = this.modelMeteringIntervals
                        .Select(meteringIntervalIndex => this.vars.Overlap[operation][meteringIntervalIndex])
                        .ToArray();

                    this.model.AddSOS(
                        vars,
                        Enumerable.Range(1, vars.Length).Select(x => (double)x).ToArray(),
                        GRB.SOS_TYPE2);
                }
            }

            foreach (var operation in this.instance.AllOperations().Where(this.IsSpannable))
            {
                this.model.AddConstr(
                    this.modelMeteringIntervals
                        .Quicksum(meteringIntervalIndex => this.vars.StartIn[operation][meteringIntervalIndex])
                    ==
                    1,
                    $"spannableStartsSomewhere_{operation.Id}")
                    .SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
            }

            foreach (var operation in this.instance.AllOperations().Where(this.IsSpannable))
            {
                foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                {
                    this.model.AddConstr(
                        this.vars.Overlap[operation][meteringIntervalIndex]
                        <=
                        this.instance.LengthMeteringInterval *
                        EnumerableExtensions
                            .RangeTo(
                                Math.Max(this.modelMeteringIntervals.FirstMeteringIntervalIndex,
                                    meteringIntervalIndex - operation.MaxNumNonZeroOvelapIntervals(this.instance) +
                                    1),
                                meteringIntervalIndex)
                            .Quicksum(meteringIntervalIndexOther =>
                                this.vars.StartIn[operation][meteringIntervalIndexOther]),
                        $"spannableSlackedOverlap_{operation.Id}{meteringIntervalIndex}")
                        .SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                }
            }

            foreach (var operation in this.instance.AllOperations().Where(this.IsSpannable))
            {
                foreach (var meteringIntervalIndex in this.modelMeteringIntervals.SkipFirst().SkipLast())
                {
                    this.model.AddConstr(
                        this.instance.LengthMeteringInterval *
                        (this.vars.NonZeroOverlap[operation][meteringIntervalIndex - 1] +
                            this.vars.NonZeroOverlap[operation][meteringIntervalIndex + 1] - 1)
                        <=
                        this.vars.Overlap[operation][meteringIntervalIndex],
                        $"spanOverlapLb_{operation.Id}{meteringIntervalIndex}")
                        .SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                }
            }

            foreach (var operation in this.instance.AllOperations())
            {
                foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                {
                    var bigM = Math.Min(this.instance.LengthMeteringInterval, operation.ProcessingTime);
                    this.model.AddConstr(
                        this.vars.Overlap[operation][meteringIntervalIndex]
                        <=
                        bigM * this.vars.NonZeroOverlap[operation][meteringIntervalIndex],
                        $"nonZeroOverlapLb_{operation.Id}{meteringIntervalIndex}")
                        .SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                }
            }

            if (this.specializedSolverConfig.OnBoundaryMaxThreeConstraint)
            {
                foreach (var machineIndex in this.instance.Machines())
                {
                    var machineOperations = this.instance.MachineOperations(machineIndex);
                    foreach (var (operation, operationOther) in machineOperations.OrderedPairs())
                    {
                        if (operation.ProcessingTime >= 2
                            && operationOther.ProcessingTime >= 2
                            && (operation.ProcessingTime + operationOther.ProcessingTime) <= 2*this.instance.LengthMeteringInterval)
                        {
                            foreach (var meteringIntervalIndex in this.modelMeteringIntervals.SkipLast())
                            {
                                this.model.AddConstr(
                                    this.vars.NonZeroOverlap[operation][meteringIntervalIndex] +
                                    this.vars.NonZeroOverlap[operation][meteringIntervalIndex + 1] +
                                    this.vars.NonZeroOverlap[operationOther][meteringIntervalIndex] +
                                    this.vars.NonZeroOverlap[operationOther][meteringIntervalIndex + 1]
                                    <=
                                    3,
                                    $"onBoundaryUb_{machineIndex}{operation.Id}{operationOther.Id}{meteringIntervalIndex}")
                                    .SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var operation in this.instance.AllOperations())
                {
                    foreach (var meteringIntervalIndex in this.modelMeteringIntervals.SkipLast())
                    {
                        this.model.AddConstr(
                            this.vars.NonZeroOverlap[operation][meteringIntervalIndex] +
                            this.vars.NonZeroOverlap[operation][meteringIntervalIndex + 1] - 1
                            <=
                            this.vars.OnBoundary[operation][meteringIntervalIndex],
                            $"onBoundaryLb_{operation.Id}{meteringIntervalIndex}")
                            .SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                    }
                }

                foreach (var machineIndex in this.instance.Machines())
                {
                    var machineOperations = this.instance.MachineOperations(machineIndex);
                    foreach (var meteringIntervalIndex in this.modelMeteringIntervals.SkipLast())
                    {
                        this.model.AddConstr(
                            machineOperations.Quicksum(operation => this.vars.OnBoundary[operation][meteringIntervalIndex])
                            <=
                            1,
                            $"atMostOneBoundary_{machineIndex}{meteringIntervalIndex}");
                    }
                }
            }

        }

        private void CreateObjective()
        {
            this.model.SetObjective(this.vars.Obj + 0, GRB.MINIMIZE);
        }

        private MeteringIntervalsSubset GetOptimizationMeteringIntervals()
        {
            switch (this.specializedSolverConfig.HorizonOptimizationStrategy)
            {
                case HorizonOptimizationStrategy.Whole:
                    return this.modelMeteringIntervals;

                case HorizonOptimizationStrategy.Decreasing:
                    return new MeteringIntervalsSubset(
                        Math.Max(
                            this.modelMeteringIntervals.FirstMeteringIntervalIndex,
                            this.modelMeteringIntervals.LastMeteringIntervalIndex - this.specializedSolverConfig.OptimizationWindow + 1),
                        this.modelMeteringIntervals.LastMeteringIntervalIndex,
                        this.instance.LengthMeteringInterval);

                default:
                    throw new ArgumentException($"Invalid optimization strategy {this.specializedSolverConfig.HorizonOptimizationStrategy}");
            }
        }

        private void PrintNonZeroOverlaps()
        {
            var s = new StringBuilder();

            foreach (var operation in this.instance.AllOperations())
            {
                foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                {
                    s.Append($"{operation.JobIndex} {operation.Index} {meteringIntervalIndex} {this.vars.NonZeroOverlap[operation][meteringIntervalIndex].ToInt()}");
                    s.Append(",");
                }
            }

            Console.WriteLine(s.ToString());
        }

        protected override void CheckSolution(StartTimes startTimes)
        {
            var comparer = NumericComparer.Default;

            // Overlaps.
            foreach (var operation in this.instance.AllOperations())
            {
                var startTimesOverlaps = operation.Overlaps(startTimes[operation], this.instance).ToArray();
                var modelOverlaps = this.vars.Overlap[operation].ToDoubles().ToArray();
                foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                {
                    var startTimesOverlap = startTimesOverlaps[meteringIntervalIndex];
                    var modelOverlap = modelOverlaps[meteringIntervalIndex];
                    Debug.Assert(comparer.AreEqual(startTimesOverlap, modelOverlap));
                }
            }
        }

        protected override StartTimes GetStartTimes()
        {
            var startTimes = new StartTimes();
            var comparer = NumericComparer.Default;

            // TODO (refactor): change to earliest start times
            var shifts = this.instance.Machines()
                .Select(_ => this.modelMeteringIntervals
                    .Select(meteringIntervalIndex =>
                        (double)this.instance.MeteringIntervalStart(meteringIntervalIndex))
                    .ToArray())
                .ToArray();

            // Start times should be computed using only overlap variables.

            var inOperations = new List<Operation>();
            foreach (var operation in this.instance.AllOperations())
            {
                var overlaps = this.vars.Overlap[operation].Select(overlap => overlap.ToDouble()).ToArray();
                if (overlaps.SkipLast().TryWhereNonZero(out var firstNonZeroOverlapMeteringIntervalIndex))
                {
                    if (comparer.Greater(overlaps[firstNonZeroOverlapMeteringIntervalIndex + 1], 0.0))
                    {
                        startTimes[operation] =
                            this.instance.MeteringIntervalEnd(firstNonZeroOverlapMeteringIntervalIndex) -
                            overlaps[firstNonZeroOverlapMeteringIntervalIndex];
                        for (var meteringIntervalIndex = firstNonZeroOverlapMeteringIntervalIndex + 1;
                            meteringIntervalIndex <= this.modelMeteringIntervals.LastMeteringIntervalIndex;
                            meteringIntervalIndex++)
                        {
                            var overlap = overlaps[meteringIntervalIndex];
                            if (comparer.AreEqual(overlap, 0.0))
                            {
                                break;
                            }

                            shifts[operation.MachineIndex][meteringIntervalIndex] += overlap;
                        }
                    }
                    else
                    {
                        inOperations.Add(operation);
                    }
                }
                else
                {
                    inOperations.Add(operation);
                }
            }

            foreach (var operation in inOperations)
            {
                if (this.vars.Overlap[operation].TryWhereNonZero(out var meteringIntervalIndex))
                {
                    startTimes[operation] = shifts[operation.MachineIndex][meteringIntervalIndex];
                    shifts[operation.MachineIndex][meteringIntervalIndex] += operation.ProcessingTime;
                }
            }

            return startTimes;
        }

        private void SetInitStartTimes()
        {
            if (this.solverConfig.InitStartTimes == null)
            {
                return;
            }

            this.SetInitStartTimes(new StartTimes(this.instance, this.solverConfig.InitStartTimes));
        }

        private void SetInitStartTimes(StartTimes initStartTimes)
        {
            if (initStartTimes == null)
            {
                return;
            }

            foreach (var (operation, startTime) in initStartTimes)
            {
                foreach (var (meteringIntervalIndex, overlap) in operation.NonZeroOverlaps(startTime, this.instance))
                {
                    this.vars.Overlap[operation][meteringIntervalIndex].Start = overlap;
                }
            }
        }

        protected override void Cleanup()
        {
            this.model.Dispose();
            this.env.Dispose();
        }

        protected override bool TimeLimitReached()
        {
            return this.model.TimeLimitReached();
        }

        private bool IsSpannable(Operation operation)
        {
            return operation.ProcessingTime > this.instance.LengthMeteringInterval;
        }

        private bool NotSpannable(Operation operation)
        {
            return this.IsSpannable(operation) == false;
        }

        public class Variables
        {
            public GRBVar Obj { get; set; }

            public Dictionary<Operation, GRBVar[]> NonZeroOverlap { get; set; }
            public Dictionary<Operation, GRBVar[]> OnBoundary { get; set; }
            public Dictionary<Operation, GRBVar[]> Overlap { get; set; }
            public Dictionary<Operation, GRBVar[]> StartIn { get; set; }
            public Dictionary<int, GRBVar[]> HasOverlappingOperation { get; set; }
        }

        public enum HorizonOptimizationStrategy
        {
            Whole = 0,
            Decreasing = 1
        }

        public class SpecializedSolverConfig
        {
            [DefaultValue(5)]
            public int OptimizationWindow { get; set; }

            [DefaultValue(HorizonOptimizationStrategy.Decreasing)]
            public HorizonOptimizationStrategy HorizonOptimizationStrategy { get; set; }

            [DefaultValue(false)]
            public bool LazyEnergyConstraints { get; set; }

            [DefaultValue(true)]
            public bool OnBoundaryMaxThreeConstraint { get; set; }

            [DefaultValue(false)]
            public bool BigMObjectiveConstraints { get; set; }
        }
    }
}
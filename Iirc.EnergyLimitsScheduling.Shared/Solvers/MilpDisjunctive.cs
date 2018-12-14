// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="MilpDisjunctive.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Gurobi;
    using Newtonsoft.Json.Linq;
    using Iirc.Utils.Math;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using Iirc.Utils.Gurobi;
    using Iirc.Utils.Collections;
    using Iirc.Utils.SolverFoundations;

    public class MilpDisjunctive : BaseSolver<MilpDisjunctive.SpecializedSolverConfig>
    {
        protected GRBEnv env;

        protected GRBModel model;

        private Variables vars;

        private double bigM;

        protected MeteringIntervalsSubset modelMeteringIntervals;

        protected override Status Solve()
        {
            this.env = new GRBEnv();
            this.model = new GRBModel(this.env);

            this.SetModelParameters();
            this.SetModelMeteringIntervals();
            this.CreateVariables();
            this.CreateConstraints();
            this.CreateObjective();
            this.SetInitStartTimes();
            this.model.SetTimeLimit(this.RemainingTime);
            this.model.Optimize();

            return this.model.GetResultStatus();
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

        protected override void CheckInstanceValidity()
        {
            if (this.solverConfig.ContinuousStartTimes)
            {
                throw new ArgumentException("Solver cannot handle continuous start times.");
            }
        }

        private void SetModelParameters()
        {
            this.model.Parameters.FeasibilityTol = NumericComparer.DefaultTolerance;
            this.model.Parameters.Threads = Math.Max(0, this.solverConfig.NumWorkers);
            if (this.solverConfig.WithEnergyLimits)
            {
                if (this.specializedSolverConfig.LazyEnergyConstraints)
                {
                    this.model.Parameters.LazyConstraints = 1;
                }
            }
        }

        private void SetModelMeteringIntervals()
        {
            if (this.solverConfig.InitStartTimes == null)
            {
                this.modelMeteringIntervals = new MeteringIntervalsSubset(
                    this.instance.FirstMeteringIntervalIndex(),
                    this.instance.LastMeteringIntervalIndex(),
                    this.instance.LengthMeteringInterval);
            }
            else
            {
                var initStartTimes = new StartTimes(this.instance, this.solverConfig.InitStartTimes);
                var numMeteringIntervals = (int)(((int)Math.Round(initStartTimes.Makespan) - 1) / this.instance.LengthMeteringInterval) + 1;
                this.modelMeteringIntervals = new MeteringIntervalsSubset(
                    this.instance.FirstMeteringIntervalIndex(),
                    Math.Min(
                        this.instance.LastMeteringIntervalIndex(),
                        this.instance.FirstMeteringIntervalIndex() + numMeteringIntervals - 1),
                    this.instance.LengthMeteringInterval);
            }
        }

        private void CreateVariables()
        {
            this.vars = new Variables();

            this.vars.Makespan = this.model.AddVar(0, GRB.INFINITY, 0, GRB.INTEGER, "makespan");

            this.vars.StartTime = new Dictionary<Operation, GRBVar>();
            foreach (var operation in this.instance.AllOperations())
            {
                this.vars.StartTime[operation] =
                    this.model.AddVar(0, GRB.INFINITY, 0, GRB.INTEGER, $"startTime_{operation.Id}");
            }

            if (this.solverConfig.IsOrderFixed == false)
            {
                this.vars.Precedence = new Dictionary<Operation, Dictionary<Operation, GRBVar>>();
                foreach (var operation in this.instance.AllOperations())
                {
                    this.vars.Precedence[operation] = new Dictionary<Operation, GRBVar>();
                    foreach (var operationOther in this.instance.AllOperations())
                    {
                        this.vars.Precedence[operation][operationOther] =
                            this.model.AddVar(0, 1, 0, GRB.BINARY, $"precedence_{operation.Id}{operationOther.Id}");
                    }
                }
            }

            if (this.solverConfig.WithEnergyLimits)
            {
                this.vars.StartInterval = new Dictionary<Operation, GRBVar[]>();
                this.vars.CompleteInterval = new Dictionary<Operation, GRBVar[]>();
                this.vars.OverlapInterval = new Dictionary<Operation, GRBVar[]>();
                this.vars.OverlapLeftInterval = new Dictionary<Operation, GRBVar[]>();
                this.vars.OverlapRightInterval = new Dictionary<Operation, GRBVar[]>();
                foreach (var operation in this.instance.AllOperations())
                {
                    this.vars.StartInterval[operation] = new GRBVar[this.modelMeteringIntervals.Count];
                    this.vars.CompleteInterval[operation] = new GRBVar[this.modelMeteringIntervals.Count];
                    this.vars.OverlapInterval[operation] = new GRBVar[this.modelMeteringIntervals.Count];
                    this.vars.OverlapLeftInterval[operation] = new GRBVar[this.modelMeteringIntervals.Count];
                    this.vars.OverlapRightInterval[operation] = new GRBVar[this.modelMeteringIntervals.Count];

                    foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                    {
                        if (this.specializedSolverConfig.MinMaxOverlapConstraints)
                        {
                            this.vars.OverlapLeftInterval[operation][meteringIntervalIndex] =
                                this.model.AddVar(-GRB.INFINITY, GRB.INFINITY, 0, GRB.INTEGER, $"overlapLeftInterval_{operation.Id}{meteringIntervalIndex}");
                            
                            this.vars.OverlapRightInterval[operation][meteringIntervalIndex] =
                                this.model.AddVar(-GRB.INFINITY, GRB.INFINITY, 0, GRB.INTEGER, $"overlapRightInterval_{operation.Id}{meteringIntervalIndex}");
                        }
                        else
                        {
                            this.vars.StartInterval[operation][meteringIntervalIndex] =
                                this.model.AddVar(0, 1, 0, GRB.BINARY, $"startInterval_{operation.Id}{meteringIntervalIndex}");

                            this.vars.CompleteInterval[operation][meteringIntervalIndex] =
                                this.model.AddVar(0, 1, 0, GRB.BINARY,
                                    $"completeInterval_{operation.Id}{meteringIntervalIndex}");
                        }

                        this.vars.OverlapInterval[operation][meteringIntervalIndex] =
                            this.model.AddVar(0, this.instance.LengthMeteringInterval, 0, GRB.INTEGER,
                                $"overlapInterval_{operation.Id}{meteringIntervalIndex}");
                    }
                }
            }
        }

        private void CreateConstraints()
        {
            this.model.Remove(this.model.GetConstrs());
            this.model.Remove(this.model.GetSOSs());
            this.model.Update();

            this.SetBigM();

            foreach (var operation in this.instance.AllOperations())
            {
                this.model.AddConstr(
                    this.vars.Makespan >= this.vars.CompletionTime(operation),
                    $"makespan_{operation.Id}");
            }

            foreach (var job in this.instance.Jobs)
            {
                foreach (var (operation, nextOperation) in job.Operations.SuccessionPairs())
                {
                    this.model.AddConstr(
                        this.vars.CompletionTime(operation) <= this.vars.StartTime[nextOperation],
                        $"jobPrecedence_{operation.Id}");
                }
            }

            if (this.solverConfig.IsOrderFixed)
            {
                var orderedOperationsOnMachines =
                    new StartTimes(this.instance, this.solverConfig.FixedOrder).GetOrderedOperationsOnMachines(
                        this.instance);

                foreach (var (_, orderedOperationsOnMachine) in orderedOperationsOnMachines)
                {
                    foreach (var (operation, nextOperation) in orderedOperationsOnMachine.SuccessionPairs())
                    {
                        this.model.AddConstr(
                            this.vars.CompletionTime(operation) <= this.vars.StartTime[nextOperation],
                            $"fixedOrderPrecedence_{operation.Id}{nextOperation.Id}");
                    }
                }
            }
            else
            {
                for (var machineIndex = 0; machineIndex < this.instance.NumMachines; machineIndex++)
                {
                    var machineOperations = this.instance.MachineOperations(machineIndex)
                        .OrderBy(operation => operation.JobIndex)
                        .ToArray();

                    for (var index = 0; index < machineOperations.Length; index++)
                    {
                        var operation = machineOperations[index];
                        for (var indexOther = index + 1; indexOther < machineOperations.Length; indexOther++)
                        {
                            var operationOther = machineOperations[indexOther];

                            this.model.AddConstr(
                                this.vars.CompletionTime(operation)
                                <=
                                this.vars.StartTime[operationOther] +
                                this.bigM * (1 - this.vars.Precedence[operation][operationOther]),
                                $"machinePrecedence_{operation.Id}");

                            this.model.AddConstr(
                                this.vars.CompletionTime(operationOther)
                                <=
                                this.vars.StartTime[operation] +
                                this.bigM * this.vars.Precedence[operation][operationOther],
                                $"machinePrecedenceReverse_{operation.Id}");
                        }
                    }
                }
            }
            
            if (this.solverConfig.WithEnergyLimits)
            {
                if (this.specializedSolverConfig.MinMaxOverlapConstraints)
                {
                    foreach (var operation in this.instance.AllOperations())
                    {
                        foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                        {
                            this.model.AddGenConstrMax(
                                this.vars.OverlapLeftInterval[operation][meteringIntervalIndex],
                                new[] {this.vars.StartTime[operation]},
                                this.instance.MeteringIntervalStart(meteringIntervalIndex),
                                $"overlapLeftInterval_{operation.Id}{meteringIntervalIndex}");

                            this.model.AddGenConstrMin(
                                this.vars.OverlapRightInterval[operation][meteringIntervalIndex],
                                new[] {this.vars.StartTime[operation]},
                                this.instance.MeteringIntervalEnd(meteringIntervalIndex) - operation.ProcessingTime,
                                $"overlapRightInterval_{operation.Id}{meteringIntervalIndex}");
                            
                            this.model.AddConstr(
                                this.vars.OverlapInterval[operation][meteringIntervalIndex]
                                >=
                                (this.vars.OverlapRightInterval[operation][meteringIntervalIndex] + operation.ProcessingTime) - this.vars.OverlapLeftInterval[operation][meteringIntervalIndex],
                                $"overlapInterval_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
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
                                this.vars.StartTime[operation]
                                >=
                                this.instance.LengthMeteringInterval * (meteringIntervalIndex + 1) *
                                (1 - this.vars.StartInterval[operation][meteringIntervalIndex]),
                                $"startIntervalLb_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);

                            this.model.AddConstr(
                                this.vars.StartTime[operation]
                                <=
                                this.instance.LengthMeteringInterval * (meteringIntervalIndex + 1)
                                - 1
                                + bigM * (1 - this.vars.StartInterval[operation][meteringIntervalIndex]),
                                $"startIntervalUb_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);

                            this.model.AddConstr(
                                this.vars.StartInterval[operation][meteringIntervalIndex + 1]
                                >=
                                this.vars.StartInterval[operation][meteringIntervalIndex],
                                $"startIntervalStep_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                        }

                        this.model.AddConstr(
                            this.vars.StartInterval[operation][this.modelMeteringIntervals.LastMeteringIntervalIndex]
                            ==
                            1,
                            $"startIntervalLastSet_{operation.Id}");
                    }

                    foreach (var operation in this.instance.AllOperations())
                    {
                        foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                        {
                            this.model.AddConstr(
                                this.vars.CompletionTime(operation)
                                >=
                                this.instance.LengthMeteringInterval * meteringIntervalIndex *
                                (1 - this.vars.CompleteInterval[operation][meteringIntervalIndex])
                                + 1,
                                $"completeIntervalLb_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);

                            this.model.AddConstr(
                                this.vars.CompletionTime(operation)
                                <=
                                this.instance.LengthMeteringInterval * meteringIntervalIndex
                                + this.bigM * (1 - this.vars.CompleteInterval[operation][meteringIntervalIndex]),
                                $"completeIntervalLb_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);

                            if (meteringIntervalIndex < this.modelMeteringIntervals.LastMeteringIntervalIndex)
                            {
                                this.model.AddConstr(
                                    this.vars.CompleteInterval[operation][meteringIntervalIndex + 1]
                                    >=
                                    this.vars.CompleteInterval[operation][meteringIntervalIndex],
                                    $"completeIntervalStep_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                            }
                        }
                    }

                    foreach (var operation in this.instance.AllOperations())
                    {
                        foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                        {
                            this.model.AddConstr(
                                this.vars.OverlapInterval[operation][meteringIntervalIndex]
                                <=
                                this.instance.LengthMeteringInterval *
                                (this.vars.StartInterval[operation][meteringIntervalIndex] -
                                this.vars.CompleteInterval[operation][meteringIntervalIndex]),
                                $"overlapUb_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                        }
                    }
                    
                    foreach (var operation in this.instance.AllOperations())
                    {
                        foreach (var meteringIntervalIndex in this.modelMeteringIntervals.SkipFirst().SkipLast())
                        {
                            this.model.AddConstr(
                                this.vars.OverlapInterval[operation][meteringIntervalIndex]
                                >=
                                this.instance.LengthMeteringInterval *
                                (this.vars.StartInterval[operation][meteringIntervalIndex - 1]
                                - this.vars.CompleteInterval[operation][meteringIntervalIndex + 1]),
                                $"overlapLb(d)_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                            
                            this.model.AddConstr(
                                this.vars.OverlapInterval[operation][meteringIntervalIndex]
                                >=
                                this.instance.LengthMeteringInterval * (meteringIntervalIndex + 1) *
                                (1 - this.vars.StartInterval[operation][meteringIntervalIndex - 1])
                                - this.vars.StartTime[operation]
                                - this.instance.LengthMeteringInterval *
                                this.vars.CompleteInterval[operation][meteringIntervalIndex + 1],
                                $"overlapLb(e)_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);

                            this.model.AddConstr(
                                this.vars.OverlapInterval[operation][meteringIntervalIndex]
                                >=
                                this.vars.CompletionTime(operation)
                                - this.instance.LengthMeteringInterval * (meteringIntervalIndex + 1)
                                + this.instance.LengthMeteringInterval *
                                this.vars.StartInterval[operation][meteringIntervalIndex - 1]
                                - this.bigM * (1 - this.vars.CompleteInterval[operation][meteringIntervalIndex + 1]),
                                $"overlapLb(b)_{operation.Id}{meteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                        }

                        this.model.AddConstr(
                            this.vars.OverlapInterval[operation][0]
                            >=
                            this.instance.LengthMeteringInterval * this.vars.StartInterval[operation][0]
                            - this.vars.StartTime[operation]
                            - this.instance.LengthMeteringInterval * this.vars.CompleteInterval[operation][1],
                            $"overlapLbFirst(e)_{operation.Id}{0}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                        
                        this.model.AddConstr(
                            this.vars.OverlapInterval[operation][this.modelMeteringIntervals.LastMeteringIntervalIndex]
                            >=
                            this.vars.CompletionTime(operation)
                            - this.modelMeteringIntervals.Horizon
                            + this.instance.LengthMeteringInterval *
                            this.vars.StartInterval[operation][this.modelMeteringIntervals.LastMeteringIntervalIndex - 1],
                            $"overlapLbLast(b)_{operation.Id}{this.modelMeteringIntervals.LastMeteringIntervalIndex}").SetLazy(this.specializedSolverConfig.LazyEnergyConstraints);
                    }
                }
                
                foreach (var operation in this.instance.AllOperations())
                {
                    this.model.AddConstr(
                        this.modelMeteringIntervals.Quicksum(meteringIntervalIndex =>
                            this.vars.OverlapInterval[operation][meteringIntervalIndex])
                        ==
                        operation.ProcessingTime,
                        $"totalOverlapIsProcTime_{operation.Id}");
                }

                foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                {
                    this.model.AddConstr(
                        this.instance.AllOperations().Quicksum(operation =>
                                operation.PowerConsumption * this.vars.OverlapInterval[operation][meteringIntervalIndex])
                        <=
                        this.instance.EnergyLimit,
                        $"energyLimit_{meteringIntervalIndex}");
                }
            }
        }

        private void CreateObjective()
        {
            this.model.SetObjective(this.vars.Makespan + 0, GRB.MINIMIZE);
        }

        protected override double? GetLowerBound()
        {
            return this.model.ObjBound;
        }

        protected override StartTimes GetStartTimes()
        {
            var startTimes = new StartTimes();

            foreach (var operation in this.instance.AllOperations())
            {
                startTimes[operation] = this.vars.StartTime[operation].ToDouble();
            }

            return startTimes;
        }

        private void SetBigM()
        {
            if (this.solverConfig.WithEnergyLimits)
            {
                this.bigM = this.modelMeteringIntervals.Horizon;
            }
            else
            {
                this.bigM = this.instance.AllOperations().Sum(operation => operation.ProcessingTime);
            }
        }

        protected override void CheckSolution(StartTimes startTimes)
        {
            var comparer = NumericComparer.Default;

            if (this.solverConfig.WithEnergyLimits)
            {
                if (this.specializedSolverConfig.MinMaxOverlapConstraints == false)
                {
                    // Start in interval.
                    foreach (var operation in this.instance.AllOperations())
                    {
                        var startIntervals = this.vars.StartInterval[operation]
                            .Select(startInterval => startInterval.ToBool())
                            .ToArray();

                        foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                        {
                            if (startIntervals[meteringIntervalIndex])
                            {
                                Debug.Assert(comparer.LessOrEqual(
                                    startTimes[operation],
                                    (this.instance.MeteringIntervalEnd(meteringIntervalIndex) - 1)));
                            }
                            else
                            {
                                Debug.Assert(comparer.LessOrEqual(
                                    this.instance.MeteringIntervalEnd(meteringIntervalIndex),
                                    startTimes[operation]));
                            }
                        }
                    }

                    // Completion in interval.
                    foreach (var operation in this.instance.AllOperations())
                    {
                        var completionIntervals = this.vars.CompleteInterval[operation]
                            .Select(completeInterval => completeInterval.ToBool())
                            .ToArray();
                        
                        foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                        {
                            if (completionIntervals[meteringIntervalIndex])
                            {
                                Debug.Assert(comparer.LessOrEqual(
                                    startTimes[operation] + operation.ProcessingTime,
                                    this.instance.MeteringIntervalStart(meteringIntervalIndex)));
                            }
                            else
                            {
                                Debug.Assert(comparer.LessOrEqual(
                                    this.instance.MeteringIntervalStart(meteringIntervalIndex) + 1,
                                    startTimes[operation] + operation.ProcessingTime));
                            }
                        }
                    }
                }

                // Overlaps.
                foreach (var operation in this.instance.AllOperations())
                {
                    var startTime = startTimes[operation];
                    var overlapFromStartTime = this.modelMeteringIntervals
                        .Select(meteringIntervalIndex => Intervals.OverlapLength(
                            startTime,
                            startTime + operation.ProcessingTime,
                            this.instance.MeteringIntervalStart(meteringIntervalIndex),
                            this.instance.MeteringIntervalEnd(meteringIntervalIndex)))
                        .ToArray();
                    var overlapFromVar = this.modelMeteringIntervals
                        .Select(meteringIntervalIndex =>
                            this.vars.OverlapInterval[operation][meteringIntervalIndex].ToDouble())
                        .ToArray();

                    foreach (var meteringIntervalIndex in this.modelMeteringIntervals)
                    {
                        Debug.Assert(comparer.AreEqual(
                            overlapFromStartTime[meteringIntervalIndex],
                            overlapFromVar[meteringIntervalIndex]));
                    }
                }
            }
        }

        private void SetInitStartTimes()
        {
            if (this.solverConfig.InitStartTimes == null)
            {
                return;
            }

            var initStartTimes = new StartTimes(this.instance, this.solverConfig.InitStartTimes);
            foreach (var (operation, startTime) in initStartTimes)
            {
                this.vars.StartTime[operation].Start = startTime;
            }
        }

        private class Variables
        {
            public GRBVar Makespan { get; set; }

            public Dictionary<Operation, GRBVar> StartTime { get; set; }

            public Dictionary<Operation, Dictionary<Operation, GRBVar>> Precedence { get; set; }

            public Dictionary<Operation, GRBVar[]> StartInterval { get; set; }

            public Dictionary<Operation, GRBVar[]> CompleteInterval { get; set; }

            public Dictionary<Operation, GRBVar[]> OverlapInterval { get; set; }
            
            public Dictionary<Operation, GRBVar[]> OverlapLeftInterval { get; set; }
            public Dictionary<Operation, GRBVar[]> OverlapRightInterval { get; set; }

            public GRBLinExpr CompletionTime(Operation operation)
            {
                return this.StartTime[operation] + operation.ProcessingTime;
            }
        }

        public class SpecializedSolverConfig
        {
            [DefaultValue(false)]
            public bool LazyEnergyConstraints { get; set; }

            [DefaultValue(false)]
            public bool MinMaxOverlapConstraints { get; set; }
        }
    }
}
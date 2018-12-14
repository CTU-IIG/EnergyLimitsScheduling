// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="FeasibilityChecker.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Algorithms
{
    using System;
    using System.Linq;
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Iirc.Utils.Collections;
    using Iirc.Utils.Math;

    public class FeasibilityChecker
    {
        private Instance instance;

        private StartTimes startTimes;

        private SolverConfig solverConfig;


        private NumericComparer comparer;

        public FeasibilityStatus Status { get; private set; }
        public Operation Operation { get; private set; }
        public Operation NextOperation { get; private set; }

        public FeasibilityStatus Check(Instance instance, StartTimes startTimes, SolverConfig solverConfig)
        {
            this.instance = instance;
            this.startTimes = startTimes;
            this.solverConfig = solverConfig;
            this.comparer = NumericComparer.Default;

            this.Operation = null;
            this.NextOperation = null;

            var feasible =
                this.EveryOperationHasStartTime()
                && this.JobPrecedencesSatisfied()
                && this.OperationsNotOverlapping();

            if (feasible && this.solverConfig.WithEnergyLimits)
            {
                feasible =
                    this.OperationsWithinHorizon()
                    && this.EnergyLimitsSatisfied();
            }

            if (feasible)
            {
                this.Status = FeasibilityStatus.Feasible;
            }

            return this.Status;
        }

        private bool EveryOperationHasStartTime()
        {
            foreach (var operation in this.instance.AllOperations())
            {
                if (this.startTimes.ContainsOperation(operation) == false)
                {
                    this.Status = FeasibilityStatus.OperationHasNoStartTime;
                    return false;
                }
            }

            return true;
        }

        private bool JobPrecedencesSatisfied()
        {
            foreach (var job in this.instance.Jobs)
            {
                foreach (var (operation, nextOperation) in job.Operations.SuccessionPairs())
                {
                    if (this.comparer.Greater(
                        this.startTimes[operation] + operation.ProcessingTime,
                        this.startTimes[nextOperation]))
                    {
                        this.Operation = operation;
                        this.NextOperation = nextOperation;
                        this.Status = FeasibilityStatus.JobPrecedenceViolated;
                        return false;
                    }
                }
            }

            return true;
        }

        private bool OperationsNotOverlapping()
        {
            for (int machineIndex = 0; machineIndex < this.instance.NumMachines; machineIndex++)
            {
                var machineOperations = this.instance.MachineOperations(machineIndex);

                // Compare succeeding (w.r.t. start times) operations.
                machineOperations = machineOperations.OrderBy(operation => this.startTimes[operation]);
                foreach (var (operation, nextOperation) in machineOperations.SuccessionPairs())
                {
                    if (this.comparer.Greater(
                        this.startTimes[operation] + operation.ProcessingTime,
                        this.startTimes[nextOperation]))
                    {
                        this.Status = FeasibilityStatus.OverlappingOperations;
                        return false;
                    }
                }
            }

            return true;
        }

        private bool OperationsWithinHorizon()
        {
            foreach (var operation in this.instance.AllOperations())
            {
                if (this.comparer.Greater(
                    this.startTimes[operation] + operation.ProcessingTime,
                    this.instance.Horizon))
                {
                    this.Status = FeasibilityStatus.OperationOutsideHorizon;
                    return false;
                }
            }

            return true;
        }

        private bool EnergyLimitsSatisfied()
        {
            if (EnergyConsumption.AreEnergyLimitsSatisfied(this.instance, this.startTimes) == false)
            {
                this.Status = FeasibilityStatus.EnergyLimitViolated;
                return false;
            }

            return true;
        }

        public enum FeasibilityStatus
        {
            Feasible = 0,
            OperationHasNoStartTime = 1,
            JobPrecedenceViolated = 2,
            OverlappingOperations = 3,
            OperationOutsideHorizon = 4,
            EnergyLimitViolated = 5
        }
    }
}
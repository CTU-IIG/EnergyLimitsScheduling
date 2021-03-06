﻿// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseSolver.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using System;
    using Newtonsoft.Json.Linq;
    using Iirc.Utils.SolverFoundations;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using System.Diagnostics;
    using Newtonsoft.Json;

    public abstract class BaseSolver<SpecializedSolverConfig> : ISolver<Instance, SolverConfig, SolverResult>
    {
        protected Instance instance;

        protected SolverConfig solverConfig;
        
        protected SpecializedSolverConfig specializedSolverConfig;

        protected Stopwatch stopwatch;

        public SolverResult Solve(SolverConfig solverConfig, Instance instance)
        {
            this.stopwatch = new Stopwatch();
            stopwatch.Restart();

            this.instance = instance;
            this.solverConfig = solverConfig;

            // TODO (refactor): move settings to own class
            this.specializedSolverConfig = JObject
                .FromObject(solverConfig.SpecializedSolverConfig)
                .ToObject<SpecializedSolverConfig>(new JsonSerializer { DefaultValueHandling = DefaultValueHandling.Populate});
            
            this.CheckConfigValidity();
            this.CheckInstanceValidity();
            
            var status = this.Solve();
            StartTimes startTimes = null;
            if (status == Status.Heuristic || status == Status.Optimal)
            {
                startTimes = this.GetStartTimes();
#if DEBUG
                this.CheckSolution(startTimes);
#endif
            }
            var timeLimitReached = this.TimeLimitReached();
            var lowerBound = this.GetLowerBound();
            this.Cleanup();
            
            stopwatch.Stop();
            return new SolverResult
            {
                Status = status,
                StartTimes = startTimes,
                TimeLimitReached = timeLimitReached,
                RunningTime = stopwatch.Elapsed,
                LowerBound = lowerBound
            };
        }

        protected TimeSpan? RemainingTime
        {
            get
            {
                if (this.solverConfig.TimeLimit.HasValue == false) {
                    return null;
                }

                var delta = this.solverConfig.TimeLimit.Value - this.stopwatch.Elapsed;
                return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
            }
        }

        protected virtual void CheckInstanceValidity()
        {
        }
        
        protected virtual void CheckConfigValidity()
        {
        }
        
        protected virtual void Cleanup()
        {
        }

        protected virtual void CheckSolution(StartTimes startTimes)
        {
        }
        
        protected virtual double? GetLowerBound()
        {
            return null;
        }

        protected abstract Status Solve();
        
        protected abstract StartTimes GetStartTimes();
        
        protected abstract bool TimeLimitReached();
    }
}
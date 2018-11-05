
namespace Iirc.EnergyLimitsScheduling.SolverCli
{
    using CommandLine;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Diagnostics;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using Iirc.EnergyLimitsScheduling.Shared.Input.Readers;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.Utils.SolverFoundations;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Iirc.EnergyLimitsScheduling.Shared.Algorithms;

    class Program
    {
        public static int Main(string[] args)
        {
            return new Parser(parserConfig => parserConfig.HelpWriter = Console.Out)
                .ParseArguments<CmdOptions>(args)
                .MapResult(
                    (CmdOptions opts) => Run(opts),
                    errs => 1
                );
        }

        private static int Run(CmdOptions opts)
        {
            try
            {
                var config = Program.GetConfig(opts);
                var solverConfig = config.ToSolverConfig();
                var instance = Program.GetInstance(opts);

                Program.CheckInstance(instance);

                var solverResult = Program.Solve(config, solverConfig, instance);
                Console.WriteLine($"Running time: {solverResult.RunningTime}");
                if (solverResult.Status == Status.Heuristic || solverResult.Status == Status.Optimal)
                {
                    Console.WriteLine($"Makespan: {solverResult.StartTimes.Makespan}");
                    Console.WriteLine(JsonConvert.SerializeObject(solverResult.StartTimes.ToIndexedStartTimes()));
                    // Console.WriteLine(JsonConvert.SerializeObject(solverConfig));
                    //Console.WriteLine(JsonConvert.SerializeObject(instance));
                }

                return 0;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static Config GetConfig(CmdOptions opts)
        {
            if (!File.Exists(opts.ConfigPath))
            {
                throw new FileNotFoundException($"Config file {opts.ConfigPath} does not exist.");
            }

            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(opts.ConfigPath));
        }

        private static SolverConfig GetSolverConfig(CmdOptions opts)
        {
            if (!File.Exists(opts.ConfigPath))
            {
                throw new FileNotFoundException($"Config file {opts.ConfigPath} does not exist.");
            }

            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(opts.ConfigPath));
            return config.ToSolverConfig();
        }

        private static Instance GetInstance(CmdOptions opts)
        {
            if (!File.Exists(opts.InstancePath))
            {
                throw new FileNotFoundException($"Instance file {opts.InstancePath} does not exist.");
            }

            return new ExtendedEnergyLimits().ReadFromPath(opts.InstancePath);
        }

        private static void CheckInstance(Instance instance)
        {
            var instanceChecker = new InstanceChecker();
            var instanceStatus = instanceChecker.Check(instance);

            if (instanceStatus != InstanceChecker.InstanceStatus.Ok)
            {
                throw new Exception($"Incorrect instance: {instanceStatus}");
            }
        }

        private static SolverResult Solve(Config config, SolverConfig solverConfig, Instance instance)
        {
            var solver = new SolverFactory().Create(config.SolverName);
            var solverResult = solver.Solve(solverConfig, instance);

            if (solverResult.Status == Status.Heuristic || solverResult.Status == Status.Optimal)
            {
                var feasibilityChecker = new FeasibilityChecker();
                var feasibilitCheckStatus = feasibilityChecker.Check(instance, solverResult.StartTimes, solverConfig);
                if (feasibilitCheckStatus != FeasibilityChecker.FeasibilityStatus.Feasible)
                {
                    throw new Exception($"Solution not feasible: {feasibilitCheckStatus}");
                }
            }
            
            return solverResult;
        }
    }
}
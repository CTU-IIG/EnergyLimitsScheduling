// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CommandLine;
    using Iirc.EnergyLimitsScheduling.Shared.Algorithms;
    using Iirc.EnergyLimitsScheduling.Shared.Input.Readers;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Iirc.Utils.SolverFoundations;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using static Iirc.EnergyLimitsScheduling.Shared.Algorithms.FeasibilityChecker;

    class Program
    {
        public static void Main(string[] args)
        {
            new Parser(parserConfig => parserConfig.HelpWriter = Console.Out)
                .ParseArguments<CmdOptions>(args)
                .WithParsed(opts => Run(opts));
        }

        private static int Run(CmdOptions opts)
        {
            if (!File.Exists(opts.PrescriptionPath))
            {
                Console.WriteLine($"Prescription file {opts.PrescriptionPath} does not exist.");
                return 1;
            }

            var rawPrescription = JObject.Parse(File.ReadAllText(opts.PrescriptionPath));
            var prescription = rawPrescription.ToObject<Prescription>();

            if (!Directory.Exists(opts.DatasetsPath))
            {
                Console.WriteLine($"Datasets directory {opts.DatasetsPath} does not exist.");
                return 1;
            }

            if (!Directory.Exists(opts.ResultsPath))
            {
                Directory.CreateDirectory(opts.ResultsPath);
            }

            foreach (var datasetName in prescription.DatasetNames)
            {
                var datasetPath = Program.DatasetPath(opts, datasetName);
                if (!Directory.Exists(datasetPath))
                {
                    Console.WriteLine($"Dataset directory {datasetPath} does not exist.");
                    return 1;
                }
            }

            if (opts.FromScratch)
            {
                foreach (var datasetName in prescription.DatasetNames)
                {
                    foreach (var solverPrescription in prescription.Solvers)
                    {
                        var solverResultsPath = Program.SolverPrescriptionResultsPath(opts, solverPrescription.Id);
                        if (Directory.Exists(solverResultsPath))
                        {
                            Directory.Delete(solverResultsPath, true);
                        }
                    }
                }
            }

            var objectLock = new object();
            foreach (var datasetName in prescription.DatasetNames)
            {
                var instancePaths = Directory.EnumerateFiles(Program.DatasetPath(opts, datasetName)).ToList();
                foreach (var solverPrescription in prescription.Solvers)
                {
                    var prescriptionSolverConfig = PrescriptionSolverConfig.Merge(
                        prescription.GlobalConfig,
                        solverPrescription.Config);

                    Parallel.ForEach(
                        instancePaths,
                        new ParallelOptions { MaxDegreeOfParallelism = opts.NumThreads },
                        (instancePath) => {
                            try
                            {
                                Console.WriteLine($"Solving {instancePath} using {solverPrescription.Id}");

                                var resultPath = Program.ResultPath(
                                    opts, solverPrescription.Id, Path.GetFileName(instancePath));

                                if (opts.FromScratch == false && File.Exists(resultPath))
                                {
                                    Console.WriteLine($"{instancePath} using {solverPrescription.Id} already solved");
                                    return;
                                }

                                var instance = new ExtendedEnergyLimits().ReadFromPath(instancePath);

                                SolverConfig solverConfig;
                                lock (objectLock)
                                {
                                    solverConfig =
                                        prescriptionSolverConfig.ToSolverConfig(solverPrescription.SpecializedSolverConfig);
                                }

                                if (solverPrescription.InitStartTimesFrom != null)
                                {
                                    var initStartTimesResultPath = Program.ResultPath(
                                        opts,
                                        solverPrescription.InitStartTimesFrom,
                                        Path.GetFileName(instancePath));

                                    var initStartTimesResult = JsonConvert.DeserializeObject<Result>(
                                        File.ReadAllText(initStartTimesResultPath));

                                    if (initStartTimesResult.Status == Status.Optimal
                                        || initStartTimesResult.Status == Status.Heuristic)
                                    {
                                        solverConfig.InitStartTimes = initStartTimesResult.StartTimes;
                                    }
                                }

                                var solver = new SolverFactory().Create(solverPrescription.SolverName);

                                var solverResult = solver.Solve(solverConfig, instance);

                                if (solverResult.Status == Status.Optimal || solverResult.Status == Status.Heuristic)
                                {
                                    var feasibilityChecker = new FeasibilityChecker();
                                    var feasibilityStatus = feasibilityChecker.Check(instance, solverResult.StartTimes, solverConfig);
                                    if (feasibilityStatus != FeasibilityStatus.Feasible)
                                    {
                                        throw new Exception($"Feasibility check failed: {feasibilityStatus}, {instancePath}, {solverPrescription.Id}");
                                    }
                                }

                                lock (objectLock)
                                {
                                    if (!Directory.Exists(Path.GetDirectoryName(resultPath)))
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
                                    }
                                }

                                File.WriteAllText(resultPath, JsonConvert.SerializeObject(Result.FromSolverResult(solverResult)));
                            }
                            catch (Exception)
                            {
                                Console.WriteLine($"Error while solving {instancePath} using {solverPrescription.Id}");
                                throw;
                            }
                        });
                }
            }

            return 0;
        }

        public static string DatasetPath(CmdOptions opts, string datasetName)
        {
            return Path.Combine(opts.DatasetsPath, datasetName);
        }

        public static string PrescriptionResultsPath(CmdOptions opts)
        {
            return Path.Combine(opts.ResultsPath, Path.GetFileNameWithoutExtension(opts.PrescriptionPath));
        }

        public static string SolverPrescriptionResultsPath(CmdOptions opts, string solverId)
        {
            return Path.Combine(Program.PrescriptionResultsPath(opts), solverId);
        }

        public static string ResultPath(CmdOptions opts, string solverId, string instanceFilename)
        {
            return Path.Combine(Program.SolverPrescriptionResultsPath(opts, solverId), instanceFilename);
        }
    }
}
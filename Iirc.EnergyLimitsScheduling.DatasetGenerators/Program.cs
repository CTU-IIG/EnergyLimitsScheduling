namespace Iirc.EnergyLimitsScheduling.DatasetGenerators
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using CommandLine;
    using Iirc.EnergyLimitsScheduling.DatasetGenerators.Generators;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.EnergyLimitsScheduling.Shared.Input.Writers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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

            var prescription = JsonConvert.DeserializeObject<Prescription>(
                File.ReadAllText(opts.PrescriptionPath),
                new JsonSerializerSettings()
                {
                    DefaultValueHandling = DefaultValueHandling.Populate
                });

            if (!Directory.Exists(opts.DatasetsPath))
            {
                Directory.CreateDirectory(opts.DatasetsPath);
            }

            var allDatasetGeneratorTypes = Program.GetDatasetGeneratorTypes();
            Type datasetGeneratorType;
            if (allDatasetGeneratorTypes.TryGetValue(prescription.DatasetGeneratorName, out datasetGeneratorType) == false)
            {
                Console.WriteLine($"Dataset generator {prescription.DatasetGeneratorName} does not exist.");
                return 1;
            }

            var datasetGenerator = (IDatasetGenerator) Activator.CreateInstance(datasetGeneratorType);

            var outputDirPath = Path.Combine(
                opts.DatasetsPath,
                Path.GetFileNameWithoutExtension(opts.PrescriptionPath));

            if (Directory.Exists(outputDirPath))
            {
                Directory.Delete(outputDirPath, true);
            }

            Directory.CreateDirectory(outputDirPath);

            var instanceWriter = new ExtendedEnergyLimits();
            var instanceIndex = 0;
            foreach (var instance in datasetGenerator.GenerateInstances(prescription))
            {
                var instanceChecker = new InstanceChecker();
                var instanceStatus = instanceChecker.Check(instance);
                if (instanceStatus != InstanceChecker.InstanceStatus.Ok)
                {
                    Console.WriteLine($"Instance check: {instanceStatus}");
                    return 1;
                }

                instanceWriter.WriteToPath(
                    instance,
                    Path.Combine(outputDirPath, $"{instanceIndex}.json"));
                instanceIndex++;
            }

            return 0;
        }

        private static Dictionary<string, Type> GetDatasetGeneratorTypes()
        {
            var datasetGeneratorInterfaceType = typeof(IDatasetGenerator);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => datasetGeneratorInterfaceType.IsAssignableFrom(type))
                .Where(type => type.IsClass)
                .ToDictionary(type => type.Name, type => type);
        }
    }
}
// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtendedEnergyLimits.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Input.Writers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;

    public class ExtendedEnergyLimits : IInputWriter
    {
        public void WriteToPath(Instance instance, string instancePath)
        {
            File.WriteAllText(instancePath, JsonConvert.SerializeObject(ExtendedEnergyLimits.ToJsonInstance(instance)));
        }

        public static JsonInstance ToJsonInstance(Instance instance)
        {
            var jsonJobs = instance.Jobs
                .Select(job => new JsonJob
                {
                    Id = job.Id,
                    Operations = job.Operations.
                        Select(operation => new JsonOperation
                        {
                            Id = operation.Id,
                            MachineIndex = operation.MachineIndex,
                            ProcessingTime = operation.ProcessingTime,
                            PowerConsumption = operation.PowerConsumption
                        })
                        .ToArray()
                })
                .ToArray();

            return new JsonInstance
            {
                NumMachines = instance.NumMachines,
                Jobs = jsonJobs,
                EnergyLimit = instance.EnergyLimit,
                Horizon = instance.Horizon,
                LengthMeteringInterval = instance.LengthMeteringInterval,
                Metadata = instance.Metadata
            };
        }
    }

    public class JsonOperation
    {
        public int Id { get; set; }
        public int MachineIndex { get; set; }
        public int ProcessingTime { get; set; }
        public double PowerConsumption { get; set; }
    }

    public class JsonJob
    {
        public int Id { get; set; }
        public JsonOperation[] Operations { get; set; }
    }

    public class JsonInstance
    {
        public int NumMachines { get; set; }
        public JsonJob[] Jobs { get; set; }
        public double EnergyLimit { get; set; }
        public int Horizon { get; set; }
        public int LengthMeteringInterval { get; set; }
        public object Metadata { get; set; }
    }
}
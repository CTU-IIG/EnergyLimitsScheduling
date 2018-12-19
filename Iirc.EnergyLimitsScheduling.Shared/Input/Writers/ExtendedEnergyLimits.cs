// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtendedEnergyLimits.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Input.Writers
{
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

    /// <summary>
    /// Class used for deserializing the operation from JSON.
    /// </summary>
    public class JsonOperation
    {
        /// <summary>
        /// Gets or sets the unique identifier of the operation within the instance.
        /// </summary>
        /// <remarks>Can be any number as long as two different operations (belonging to the same or different jobs)
        /// have different ids.</remarks>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the index of the dedicated machine.
        /// </summary>
        /// <remarks>Must be a number in range [0, <see cref="JsonInstance.NumMachines"/>-1].</remarks>
        public int MachineIndex { get; set; }
        
        /// <summary>
        /// Gets or sets the processing time.
        /// </summary>
        public int ProcessingTime { get; set; }
        
        /// <summary>
        /// Gets or sets the power consumption.
        /// </summary>
        public double PowerConsumption { get; set; }
    }

    /// <summary>
    /// Class used for deserializing the job from JSON.
    /// </summary>
    public class JsonJob
    {
        /// <summary>
        /// Gets or sets the unique identifier of the job within the instance.
        /// </summary>
        /// <remarks>Can be any number as long as two different jobs have different ids.</remarks>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the operations of this job.
        /// </summary>
        public JsonOperation[] Operations { get; set; }
    }

    /// <summary>
    /// Class used for deserializing the instances from JSON.
    /// </summary>
    public class JsonInstance
    {
        /// <summary>
        /// Gets or sets the number of machines.
        /// </summary>
        public int NumMachines { get; set; }
        
        /// <summary>
        /// Gets or sets the jobs.
        /// </summary>
        public JsonJob[] Jobs { get; set; }
        
        /// <summary>
        /// Gets or sets the energy limit.
        /// </summary>
        public double EnergyLimit { get; set; }
        
        /// <summary>
        /// Gets or sets the length of the scheduling horizon.
        /// </summary>
        public int Horizon { get; set; }
        
        /// <summary>
        /// Gets or sets the length of the metering intervals.
        /// </summary>
        public int LengthMeteringInterval { get; set; }
        
        /// <summary>
        /// Gets or sets the metadata describing the instance, e.g., parameters used for generate the instance.
        /// </summary>
        public object Metadata { get; set; }
    }
}
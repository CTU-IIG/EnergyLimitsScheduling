namespace Iirc.EnergyLimitsScheduling.Shared.Input.Readers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Iirc.EnergyLimitsScheduling.Shared.Input.Writers;
    using Iirc.Utils.Text;
    using Newtonsoft.Json;

    public class ExtendedEnergyLimits : IInputReader
    {
        public Instance ReadFromPath(string instancePath)
        {
            var jsonInstance = JsonConvert.DeserializeObject<JsonInstance>(File.ReadAllText(instancePath));

            var jobs = jsonInstance.Jobs
                .Select((jsonJob, jobIndex) => new Job(
                    jsonJob.Id,
                    jobIndex,
                    jsonJob.Operations
                        .Select((jsonOperation, operationIndex) => new Operation(
                            jsonOperation.Id,
                            operationIndex,
                            jobIndex,
                            jsonOperation.MachineIndex,
                            jsonOperation.ProcessingTime,
                            jsonOperation.PowerConsumption))
                        .ToArray()))
                .ToArray();

            return new Instance(
                jsonInstance.NumMachines,
                jobs,
                jsonInstance.EnergyLimit,
                jsonInstance.Horizon,
                jsonInstance.LengthMeteringInterval,
                jsonInstance.Metadata);
        }
    }
}
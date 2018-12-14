// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleEnergyLimits.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Shared.Input.Readers
{
    using System.Collections.Generic;
    using System.IO;
    using Iirc.Utils.Text;

    public class SimpleEnergyLimits : IInputReader
    {
        private const int FirstJobLine = 1;

        private string[] Lines { get; set; }

        private int NumJobs { get; set; }
        private int NumMachines { get; set; }
        private double EnergyLimit { get; set; }
        private int Horizon { get; set; }
        private int LengthMeteringInterval { get; set; }
        private List<Job> Jobs { get; set; }

        public Instance ReadFromPath(string instancePath)
        {
            this.Lines = File.ReadAllText(instancePath).SanitizeWhitespace().SplitNewlines();

            this.GetInstanceParameters();
            this.GetJobs();

            return new Instance(
                this.NumMachines,
                this.Jobs.ToArray(),
                this.EnergyLimit,
                this.Horizon,
                this.LengthMeteringInterval);
        }

        private void GetInstanceParameters()
        {
            var splitFirstLine = this.Lines[0].Split(' ');

            this.NumJobs = int.Parse(splitFirstLine[0]);
            this.NumMachines = int.Parse(splitFirstLine[1]);
            this.EnergyLimit = double.Parse(splitFirstLine[2]);
            this.Horizon = int.Parse(splitFirstLine[3]);
            this.LengthMeteringInterval = int.Parse(splitFirstLine[4]);
        }

        private void GetJobs()
        {
            this.Jobs = new List<Job>();

            int nextFreeJobId = 0;
            int nextFreeOperationId = 0;

            for (int jobIndex = 0; jobIndex < this.NumJobs; jobIndex++)
            {
                var splitJobLine = this.Lines[FirstJobLine + jobIndex].Split(' ');

                var operations = new List<Operation>();
                var numOperations = splitJobLine.Length / 3;
                for (int operationIndex = 0; operationIndex < numOperations; operationIndex++)
                {
                    operations.Add(new Operation(
                        nextFreeOperationId++,
                        operationIndex,
                        jobIndex,
                        int.Parse(splitJobLine[operationIndex * 3]),
                        int.Parse(splitJobLine[operationIndex * 3 + 1]),
                        double.Parse(splitJobLine[operationIndex * 3 + 2])));
                }

                this.Jobs.Add(new Job(
                    nextFreeJobId++,
                    jobIndex,
                    operations.ToArray()));
            }
        }
    }
}
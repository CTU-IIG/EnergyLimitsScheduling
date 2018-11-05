namespace Iirc.EnergyLimitsScheduling.DatasetGenerators.Generators
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.EnergyLimitsScheduling.Shared.Input.Writers;
    using Iirc.Utils.Math;
    using Iirc.Utils.Random.Distributions;
    using Newtonsoft.Json.Linq;

    public class SingleOperationJobsSpannableRatio : IDatasetGenerator
    {
        private Prescription prescription;
        private SpecializedPrescription specializedPrescription;
        private Random random;
        private NumericComparer NumericComparer;

        public IEnumerable<Instance> GenerateInstances(Prescription prescription)
        {
            this.prescription = prescription;
            this.specializedPrescription = JObject
                .FromObject(prescription.SpecializedPrescription)
                .ToObject<SpecializedPrescription>();
            this.random = this.prescription.RandomSeed.HasValue ?
                new Random(this.prescription.RandomSeed.Value) : new Random();
            this.NumericComparer = NumericComparer.Default;

            foreach (var numJobs in this.specializedPrescription.NumJobs)
            {
                foreach (var numMachines in this.specializedPrescription.NumMachines)
                {
                    foreach (var powerConsumptionLbMultiplier in this.specializedPrescription.PowerConsumptionLbMultiplier)
                    {
                        foreach (var spannableRatio in this.specializedPrescription.SpannableRatio)
                        {
                            foreach (var repetition in Enumerable.Range(0, this.prescription.NumRepetitions))
                            {
                                yield return this.GenerateInstance(
                                    numJobs,
                                    numMachines,
                                    powerConsumptionLbMultiplier,
                                    spannableRatio,
                                    repetition);
                            }
                        }
                    }
                }
            }
        }

        public Instance GenerateInstance(
            int numJobs,
            int numMachines,
            double powerConsumptionLbMultiplier,
            double spannableRatio,
            int repetition)
        {
            var numSpannableOperations = (int)(numJobs * spannableRatio);

            var jobs = new List<Job>();
            foreach (var jobIndex in Enumerable.Range(0, numJobs))
            {
                int processingTime = jobIndex < numSpannableOperations ?
                    this.random.Next(16, this.specializedPrescription.MaxProcessingTime)
                    : this.random.Next(
                        this.specializedPrescription.MinProcessingTime,
                        this.specializedPrescription.LengthMeteringInterval);

                var powerConsumptionDistribution = new UniformDistribution(
                    powerConsumptionLbMultiplier * this.specializedPrescription.EnergyLimit / (1.0 * numMachines * this.specializedPrescription.LengthMeteringInterval),
                    2 * this.specializedPrescription.EnergyLimit / (1.0 * numMachines * this.specializedPrescription.LengthMeteringInterval),
                    this.random);

                var powerConsumption = powerConsumptionDistribution.Sample();
                if (this.NumericComparer.Greater(
                    Math.Min(processingTime, this.specializedPrescription.LengthMeteringInterval) * powerConsumption,
                    this.specializedPrescription.EnergyLimit))
                {
                    powerConsumption =
                        this.specializedPrescription.EnergyLimit
                        / Math.Min(processingTime, this.specializedPrescription.LengthMeteringInterval);

                }

                var machineIndex = this.random.Next(0, numMachines);

                jobs.Add(new Job(
                    jobIndex,
                    jobIndex,
                    new[]
                    {
                        new Operation(jobIndex, 0, jobIndex, machineIndex, processingTime, powerConsumption)
                    }));
            }

            // TODO: probably use heuristic.
            var horizon = ((jobs.Sum(job => job.FirstOperation().ProcessingTime) / this.specializedPrescription.LengthMeteringInterval) + 1) * this.specializedPrescription.LengthMeteringInterval;

            var metadata = new {
                powerConsumptionLbMultiplier = powerConsumptionLbMultiplier,
                spannableRatio = spannableRatio,
                repetition = repetition,
                numMachines = numMachines,
                numJobs = numJobs
            };

            return new Instance(
                numMachines,
                jobs.ToArray(),
                this.specializedPrescription.EnergyLimit,
                horizon,
                this.specializedPrescription.LengthMeteringInterval,
                metadata);
        }

        private class SpecializedPrescription
        {
            public int[] NumJobs { get; set; }
            public int[] NumMachines { get; set; }
            public int LengthMeteringInterval { get; set; }
            public int MinProcessingTime { get; set; }
            public int MaxProcessingTime { get; set; }
            public double EnergyLimit { get; set; }
            public double[] PowerConsumptionLbMultiplier { get; set; }
            public double[] SpannableRatio { get; set; }
        }
    }
}
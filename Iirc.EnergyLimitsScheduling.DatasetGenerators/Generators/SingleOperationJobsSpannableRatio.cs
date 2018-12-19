// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="SingleOperationJobsSpannableRatio.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.DatasetGenerators.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.EnergyLimitsScheduling.Shared.Input;
    using Iirc.Utils.Math;
    using Iirc.Utils.Random.Distributions;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Dataset generator.
    /// </summary>
    /// <remarks>
    /// Details
    /// <list type="bullet">
    /// <item><description>Each job has one operation.</description></item>
    /// <item><description>The processing times of the operations depend on the spannable ratio.</description></item>
    /// <item><description>The operations are assigned to machines randomly.</description></item>
    /// </list>
    /// </remarks>
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

        /// <summary>
        /// The specialized prescription.
        /// </summary>
        private class SpecializedPrescription
        {
            /// <summary>
            /// Gets or sets the number of jobs to generate.
            /// </summary>
            public int[] NumJobs { get; set; }
            
            /// <summary>
            /// Gets or sets the number of machines to generate.
            /// </summary>
            public int[] NumMachines { get; set; }
            
            /// <summary>
            /// Gets or sets the length of the metering interval.
            /// </summary>
            public int LengthMeteringInterval { get; set; }
            
            /// <summary>
            /// Gets or sets the minimum processing time of the operations.
            /// </summary>
            public int MinProcessingTime { get; set; }
            
            /// <summary>
            /// Gets or sets the maximum processing time of the operations.
            /// </summary>
            public int MaxProcessingTime { get; set; }
            
            /// <summary>
            /// Gets or sets the energy limit.
            /// </summary>
            public double EnergyLimit { get; set; }
            
            /// <summary>
            /// Gets or sets the power consumption lower bound multipliers.
            /// </summary>
            /// <remarks>
            /// Corresponds to beta from
            /// "Módos István et al., Scheduling on dedicated machines with energy consumption limit, ICORES 2019".
            /// </remarks>
            public double[] PowerConsumptionLbMultiplier { get; set; }
            
            /// <summary>
            /// Gets or sets the ratios of the spannable operations (value between 0.0 and 1.0).
            /// </summary>
            /// <remarks>
            /// Corresponds to alpha from
            /// "Módos István et al., Scheduling on dedicated machines with energy consumption limit, ICORES 2019".
            /// </remarks>
            public double[] SpannableRatio { get; set; }
        }
    }
}
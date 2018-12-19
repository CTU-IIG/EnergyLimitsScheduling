// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Prescription.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using Newtonsoft.Json;

    /// <summary>
    /// Prescription of the experimental setup.
    /// </summary>
    public class Prescription
    {
        /// <summary>
        /// Gets or sets the global config.
        /// </summary>
        public PrescriptionSolverConfig GlobalConfig { get; set; }

        /// <summary>
        /// Gets or sets the dataset names to evaluate.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string[] DatasetNames { get; set; }

        /// <summary>
        /// Gets or sets the prescription of the solvers.
        /// </summary>
        /// <remarks>Currently there is no dependency handling between the solvers (e.g., due to
        /// <see cref="SolverPrescription.InitStartTimesFrom"/>), the array must be a topological ordering of the
        /// solvers (e.g., solver on the index zero has no dependencies).</remarks>
        [JsonProperty(Required = Required.Always)]
        public SolverPrescription[] Solvers { get; set; }
    }
}
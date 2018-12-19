// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Prescription.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.DatasetGenerators
{
    using System.ComponentModel;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Prescription on how to generate the dataset.
    /// </summary>
    public class Prescription
    {
        /// <summary>
        /// Gets or sets the name of the dataset generator class.
        /// </summary>
        public string DatasetGeneratorName { get; set; }

        /// <summary>
        /// Gets or sets the number of instances to generate from fixed parameters.
        /// </summary>
        public int NumRepetitions { get; set; }

        /// <summary>
        /// Gets or sets the seed used for random generator. If not specified, a random value is used.
        /// </summary>
        [DefaultValue(null)]
        public int? RandomSeed { get; set; }

        /// <summary>
        /// Gets or sets the prescription that is specific for the dataset generator (see the specialized prescription
        /// class contained in the generators for more details).
        /// </summary>
        public JObject SpecializedPrescription { get; set; }
    }
}
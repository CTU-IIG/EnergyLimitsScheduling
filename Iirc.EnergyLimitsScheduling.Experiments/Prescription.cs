// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Prescription.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using Newtonsoft.Json;

    public class Prescription
    {
        public PrescriptionSolverConfig GlobalConfig { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string[] DatasetNames { get; set; }

        [JsonProperty(Required = Required.Always)]
        public SolverPrescription[] Solvers { get; set; }
    }
}
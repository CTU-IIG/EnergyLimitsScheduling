namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class Prescription
    {
        public PrescriptionSolverConfig GlobalConfig { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string[] DatasetNames { get; set; }

        [JsonProperty(Required = Required.Always)]
        public SolverPrescription[] Solvers { get; set; }
    }
}
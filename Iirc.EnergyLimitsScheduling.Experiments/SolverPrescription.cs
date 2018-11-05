namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Iirc.EnergyLimitsScheduling.Shared.Solvers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class SolverPrescription
    {
        [JsonProperty(Required=Required.Always)]
        public string Id { get; set; }     

        [JsonProperty(Required=Required.Always)]
        public string SolverName { get; set; }     

        public string InitStartTimesFrom { get; set; }     
        
        public PrescriptionSolverConfig Config { get; set; }

        public JObject SpecializedSolverConfig { get; set; }     
    }
}
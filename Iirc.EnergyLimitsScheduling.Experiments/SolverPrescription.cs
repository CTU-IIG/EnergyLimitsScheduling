// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="SolverPrescription.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Experiments
{
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
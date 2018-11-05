namespace Iirc.EnergyLimitsScheduling.DatasetGenerators
{
    using System.ComponentModel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class Prescription
    {
        public string DatasetGeneratorName { get; set; }

        public int NumRepetitions { get; set; }

        [DefaultValue(null)]
        public int? RandomSeed { get; set; }

        public JObject SpecializedPrescription { get; set; }
    }
}
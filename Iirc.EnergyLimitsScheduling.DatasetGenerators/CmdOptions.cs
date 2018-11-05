namespace Iirc.EnergyLimitsScheduling.DatasetGenerators
{
    using CommandLine;

    public class CmdOptions
    {
        [Value(0, Required = true, MetaValue = "DATASETS_PATH", HelpText = "Path to the datasets directory.")]
        public string DatasetsPath { get; set; }

        [Value(1, Required = true, MetaValue = "PRESCRIPTION_PATH", HelpText = "Path to the prescription file.")]
        public string PrescriptionPath { get; set; }
    }
}
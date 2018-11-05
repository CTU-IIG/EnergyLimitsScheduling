namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using CommandLine;

    public class CmdOptions
    {
        [Option("num-threads", Default = 1, HelpText = "Number of threads solving instances in parallel.")]
        public int NumThreads { get; set; }

        [Option("from-scratch", HelpText = "Whether to start the experiment from scratch.")]
        public bool FromScratch { get; set; }

        [Value(0, Required = true, MetaValue = "DATASETS_PATH", HelpText = "Path to the datasets directory.")]
        public string DatasetsPath { get; set; }

        [Value(1, Required = true, MetaValue = "PRESCRIPTION_PATH", HelpText = "Path to the prescription file.")]
        public string PrescriptionPath { get; set; }

        [Value(2, Required = true, MetaValue = "RESULTS_PATH", HelpText = "Path to the results directory.")]
        public string ResultsPath { get; set; }
    }
}
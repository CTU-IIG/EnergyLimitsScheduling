namespace Iirc.EnergyLimitsScheduling.SolverCli
{
    using CommandLine;

    public class CmdOptions
    {
        [Value(0, Required = true, MetaValue = "CONFIG_PATH", HelpText = "Path to the configuration file.")]
        public string ConfigPath { get; set; }

        [Value(1, Required = true, MetaValue = "INSTANCE_PATH", HelpText = "Path to the instance file.")]
        public string InstancePath { get; set; }

        [Value(1, Required = true, MetaValue = "RESULT_PATH", HelpText = "Path to the result file.")]
        public string ResultPath { get; set; }
    }
}
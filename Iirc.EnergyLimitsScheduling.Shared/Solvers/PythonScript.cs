namespace Iirc.EnergyLimitsScheduling.Shared.Solvers
{
    using Newtonsoft.Json;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Reflection;
    using Newtonsoft.Json.Linq;
    using Iirc.Utils.SolverFoundations;
    using System.Collections.Generic;
    using Iirc.EnergyLimitsScheduling.Shared.DataStructs;
    using System;

    public abstract class PythonScript<SpecializedSolverConfig> : BaseSolver<SpecializedSolverConfig>
    {
        private readonly string solverName;

        private string solverConfigPath;
        private string instancePath;
        private string solverResultPath;
        private SolverScriptResult solverScriptResult;

        protected PythonScript(string solverName)
        {
            this.solverName = solverName;
        }

        protected string PythonBinPath
        {
            get
            {
                return Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "python");
            }
        }

        protected string SolversPath
        {
            get
            {
                return Path.Combine(this.PythonBinPath, "solvers");
            }
        }

        protected string SolverPath
        {
            get
            {
                return Path.Combine(this.SolversPath, $"{this.solverName}.py");
            }
        }

        protected override Status Solve()
        {
            this.solverConfigPath = Path.GetTempFileName();
            this.instancePath = Path.GetTempFileName();
            this.solverResultPath = Path.GetTempFileName();
            
            File.WriteAllText(this.solverConfigPath, JsonConvert.SerializeObject(this.solverConfig));
            File.WriteAllText(this.instancePath, JsonConvert.SerializeObject(this.instance));

            var process = new Process();
            process.StartInfo.FileName = "python3";
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.WorkingDirectory = this.PythonBinPath;
            process.StartInfo.Arguments = $"{this.SolverPath} {this.solverConfigPath} {this.instancePath} {this.solverResultPath}";
            process.StartInfo.RedirectStandardInput = true;

            process.Start();
            process.WaitForExit();

            this.solverScriptResult =
                JsonConvert.DeserializeObject<SolverScriptResult>(File.ReadAllText(solverResultPath));

            return this.solverScriptResult.Status;
        }

        protected override void Cleanup()
        {
            File.Delete(this.solverConfigPath);
            File.Delete(this.instancePath);
            File.Delete(this.solverResultPath);
        }

        protected override double? GetLowerBound()
        {
            return this.solverScriptResult.LowerBound;
        }
        
        protected override StartTimes GetStartTimes()
        {
            return new StartTimes(this.instance, this.solverScriptResult.StartTimes);
        }

        protected override bool TimeLimitReached()
        {
            return this.solverScriptResult.TimeLimitReached;
        }

        private class SolverScriptResult
        {
            public Status Status { get; set; }

            public bool TimeLimitReached { get; set; }

            public List<StartTimes.IndexedStartTime> StartTimes { get; set; }
            
            public double? LowerBound { get; set; }
        }
    }
}

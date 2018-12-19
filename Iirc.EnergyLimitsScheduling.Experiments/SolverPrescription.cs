// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="SolverPrescription.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.EnergyLimitsScheduling.Experiments
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The solver prescription.
    /// </summary>
    public class SolverPrescription
    {
        /// <summary>
        /// Gets or sets the id of the solver.
        /// </summary>
        /// <remarks>Can be any string that can be converted to a file system name. However, two different solvers
        /// within the same experimental prescription cannot have the same id.</remarks>
        [JsonProperty(Required=Required.Always)]
        public string Id { get; set; }     

        /// <summary>
        /// Gets or sets the name of the solver class.
        /// </summary>
        [JsonProperty(Required=Required.Always)]
        public string SolverName { get; set; }     

        /// <summary>
        /// Gets or sets the id of the solver, whose start times will be used as initial start times. If not specified,
        /// the solver starts without the initial start times.
        /// </summary>
        public string InitStartTimesFrom { get; set; }     
        
        /// <summary>
        /// Gets or sets the solver configuration.
        /// </summary>
        /// <remarks>If some properties of the solver configuration are not provided, the global ones are used, see
        /// <see cref="Prescription.GlobalConfig"/>.
        /// </remarks>
        public PrescriptionSolverConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the solver configuration that is specific for the solver (see the specialized configuration
        /// class contained in the solvers for more details).
        /// </summary>
        public JObject SpecializedSolverConfig { get; set; }     
    }
}
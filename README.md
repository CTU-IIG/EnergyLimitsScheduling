# Energy Limits Scheduling

This repository contains the source code for the scheduling problem with energy consumption limits.
The benchmark instances and results are available [here](https://github.com/CTU-IIG/EnergyLimitsSchedulingDatasets) .

## Dependencies

You need the following

- .NET Core (>= 2.1)
- Python (>= 3.5)
- IBM CP Optimizer (>= 12.8)
- Gurobi (>= 8.0)
- docplex (>= 2.7.113)

The code is known to work on Fedora 28 and Debian Stretch operating systems.
Also make sure that environment variable `GUROBI_HOME` exists and it points to the installation directory, .e.g., on GNU/Linux
```bash
$ echo $GUROBI_HOME
/home/modosist/opt/gurobi800/linux64
```

## Projects

The repository contains one C# solution with four projects

- `DatasetGenerators` - generators of instance dataset from the given prescription.
- `Experiments` - runs the specified solvers on a dataset.
- `Shared` - library with common code shared among the rest of the projects, e.g., it contains the source codes of the solvers.
- `SolverCli` - command line interface for solving one instance with a specified solver.

The following sections explains the projects in detail.

### DatasetGenerators

To compile and run the project from the command line do the following (passing no arguments will print the help message)

```bash
$ dotnet run -c Release -p Iirc.EnergyLimitsScheduling.DatasetGenerators -- DATASETS_PATH PRESCRIPTION_PATH
```

where

- `DATASETS_PATH` is the path to directory with datasets.
- `PRESCRIPTION_PATH` is the path to the prescription file that describes how the instances are to be generated.
  The prescription files are in JSON format, see class `Prescription` in `Iirc.EnergyLimitsScheduling.DatasetGenerators/Prescription.cs` for the description of the files content (`Prescription` class is used for deserializing the JSON files).

The command will create a new directory with a name of the prescription file in `DATASETS_PATH` and fills it with the generated instances.

The generated instance files are in JSON format, see class `JsonInstance` in `Iirc.EnergyLimitsScheduling.Shared/Input/Writers/ExtendedEnergyLimits.cs` for the description of the files content (`JsonInstance` class is used for deserializing the JSON files).

### Experiments

To compile and run the project from the command line do the following (passing no arguments will print the help message)

```bash
$ dotnet run -c Release -p Iirc.EnergyLimitsScheduling.Experiments -- DATASETS_PATH PRESCRIPTION_PATH RESULTS_PATH
```

where

- `DATASETS_PATH` is the path to directory with datasets.
- `PRESCRIPTION_PATH` is the path to the prescription file that describes the experimental setup, e.g., the solvers to test.
  The prescription files are in JSON format, see class `Prescription` in `Iirc.EnergyLimitsScheduling.Experiments/Prescription.cs` for the description of the files content (`Prescription` class is used for deserializing the JSON files).
- `RESULTS_PATH` is the path to directory with results for each dataset.

The command will create a new directory with a name of the prescription file in `RESULTS_PATH` and fills it with the results.
The location of each result has the following format

```bash
{RESULTS_PATH}/{prescriptionFilename}/{datasetName}/{solverId}/{instanceFilename}.json
```

where

- `{prescriptionFilename}` is the file name of the experimental setup prescription.
- `{datasetName}` is the dataset name as specified in the prescription.
- `{solverId}` is the solver id as specified in the prescription.
- `{instanceFilename}` is the file name of the instance for which a result is generated.

The generated result files are in JSON format, see class `Result` in `Iirc.EnergyLimitsScheduling.Experiments/Result.cs` for the description of the files content (`Result` class is used for deserializing the JSON files).

The experiment is generating the results on the fly.
By default, if the experiment is interrupted before it is completed, restarting the experiment will resume from the previous point, i.e., it will keep the previously generated results.
If flag `--from-scratch` is passed, the previously generated results are deleted and the experiment starts from scratch.

By default, only one instance is being solved at a time.
The number of instances to solve in parallel can be specified using option `--num-threads`.

### SolverCli
To compile and run the project from the command line do the following (passing no arguments will print the help message)

```bash
$ dotnet run -c Release -p Iirc.EnergyLimitsScheduling.SolverCli -- CONFIG_PATH INSTANCE_PATH
```

where

- `CONFIG_PATH` is the path to configuration file.
  The configuration files are in JSON format, see class `Config` in `Iirc.EnergyLimitsScheduling.SolverCli/Config.cs` for the description of the files content (`Config` class is used for deserializing the JSON files).
- `INSTANCE_PATH` is the path to the instance file.

The command will run the given instance on the specified solver within the configuration and prints the status to standard output.

## License

[MIT license](LICENSE.txt)

## Authors

Please see file [AUTHORS.txt](AUTHORS.txt) for the list of authors.

## <a name="citing"></a>Citing

TODO
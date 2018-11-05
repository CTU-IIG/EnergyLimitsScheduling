#!/usr/bin/env python3

from pathlib import Path
import sys
import json
from datetime import timedelta
import time
from docplex.cp.parameters import VALUE_OFF, VALUE_AUTO
from docplex.cp.model import CpoModel
from docplex.cp.solution import CpoModelSolution

from datastructs.result import Result, Status
from datastructs.instance import Instance
import utils
import cp_utils

start_time_solver = time.time()

solver_config_path = Path(sys.argv[1]).resolve()
instance_path = Path(sys.argv[2]).resolve()
solver_result_path = Path(sys.argv[3]).resolve()

solver_config = json.loads(solver_config_path.read_text())
solver_config['TimeLimit'] = utils.parse_timedelta(solver_config['TimeLimit'])

instance = Instance.from_json(instance_path.read_text())

model = CpoModel()

# RestartPropagationLimitFactor and TemporalRelaxation suggested by Philippe Laborie.
model.set_parameters({
    "RestartPropagationLimitFactor": 1000,
    "TemporalRelaxation": VALUE_OFF,
    "Workers": solver_config['NumWorkers'] if solver_config['NumWorkers'] > 0 else VALUE_AUTO
})

# Variables (classic job shop).
operation_vars = dict()
for job in instance.jobs:
    for operation in job.operations:
        operation_vars[operation] = model.interval_var(
            length=operation.processing_time,
            name='var_' + str(operation.id))

# Can be changed by init start times.
num_metering_intervals = instance.num_metering_intervals

if solver_config['InitStartTimes'] is not None and solver_config['InitStartTimes']:
    init_start_times = {instance.jobs[d['JobIndex']].operations[d['OperationIndex']]: d['StartTime']
                        for d in solver_config['InitStartTimes']}
    init_vars = CpoModelSolution()
    for operation, start_time in init_start_times.items():
        init_vars.add_interval_var_solution(operation_vars[operation], presence=True, start=int(round(start_time)))
    model.set_starting_point(init_vars)

    makespan = int(round(max([start_time + operation.processing_time
                              for operation, start_time in init_start_times.items()])))
    num_metering_intervals = int((makespan - 1) / instance.length_metering_interval) + 1

machine_vars = dict()
for machine_index in range(instance.num_machines):
    machine_vars[machine_index] =\
        model.sequence_var([operation_vars[operation]
                            for job in instance.jobs
                            for operation in job.operations if operation.machine_index == machine_index])

# Constraints (classic job shop).
for job in instance.jobs:
    for operation, next_operation in zip(job.operations[:-1], job.operations[1:]):
        model.add(model.end_before_start(operation_vars[operation], operation_vars[next_operation]))

for machine_index in range(instance.num_machines):
    model.add(model.no_overlap(machine_vars[machine_index]))
    
# Constraints (energy limits).
if solver_config['WithEnergyLimits']:
    for operation_var in operation_vars.values():
        model.add(model.end_of(operation_var) <= num_metering_intervals * instance.length_metering_interval)

    for metering_interval_index in range(num_metering_intervals):
            model.add(model.sum(operation.power_consumption * model.overlap_length(
                                    operation_vars[operation],
                                    (metering_interval_index * instance.length_metering_interval,
                                     (metering_interval_index + 1) * instance.length_metering_interval))
                                for job in instance.jobs for operation in job.operations)
                      <= instance.energy_limit)

# Objective.
model.add(model.minimize(model.max([model.end_of(operation_var) for operation_var in operation_vars.values()])))

remaining_time = solver_config['TimeLimit'].total_seconds() - (time.time() - start_time_solver)
solution = model.solve(TimeLimit=remaining_time)

start_times = dict()
if cp_utils.get_result_status(solution) in {Status.Heuristic, Status.Optimal}:
    start_times = {operation: solution.get_var_solution(operation_vars[operation]).start
                   for job in instance.jobs for operation in job.operations}

solver_result = Result(
    cp_utils.get_result_status(solution),
    cp_utils.time_limit_reached(solution),
    timedelta(seconds=time.time() - start_time_solver),
    start_times
)

solver_result_path.write_text(solver_result.to_json())

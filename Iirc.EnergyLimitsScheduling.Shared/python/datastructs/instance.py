from typing import List, Generator, Dict, Optional
import json

__all__ = [
    'Operation',
    'Job',
    'Instance'
]

class Operation:

    __slots__ = [
        'id',
        'index',
        'job_index',
        'machine_index',
        'processing_time',
        'power_consumption'
    ]

    def __init__(
            self,
            id: int,
            index: int,
            job_index: int,
            machine_index: int,
            processing_time: int,
            power_consumption: float):
        self.id = id
        self.index = index
        self.job_index = job_index
        self.machine_index = machine_index
        self.processing_time = processing_time
        self.power_consumption = power_consumption

    def __hash__(self):
        return hash(self.id)

    def __eq__(self, other):
        return self.id == other.id


class Job:

    __slots__ = [
        'id',
        'index',
        'operations'
    ]

    def __init__(self, id: int, index: int, operations: List[Operation]):
        self.id = id
        self.index = index
        self.operations = operations

    def __hash__(self):
        return hash(self.id)

    def __eq__(self, other):
        return self.id == other.id

class Instance:

    __slots__ = [
        'num_machines',
        'jobs',
        'energy_limit',
        'horizon',
        'length_metering_interval',
        'num_metering_intervals',
        'metadata',
        'instance_filename'
    ]

    def __init__(
        self,
        num_machines: int,
        jobs: List[Job],
        energy_limit: float,
        horizon: int,
        length_metering_interval: int,
        metadata: Optional[Dict[str, object]] = None,
        instance_filename: str = None):
        self.num_machines = num_machines
        self.jobs = jobs
        self.energy_limit = energy_limit
        self.horizon = horizon
        self.length_metering_interval = length_metering_interval
        self.num_metering_intervals = int(self.horizon / self.length_metering_interval)
        self.metadata = metadata if metadata is not None else dict()
        self.instance_filename = instance_filename

    def get_operations(self) -> Generator[Operation, None, None]:
        for job in self.jobs:
            for operation in job.operations:
                yield operation

    @staticmethod
    def from_json(s: str, instance_filename: str = None):
        ins_raw = json.loads(s)

        jobs = []
        for job_index, job_raw in enumerate(ins_raw['Jobs']):
            operations = []
            for operation_index, operation_raw in enumerate(job_raw['Operations']):
                operations.append(Operation(
                    operation_raw['Id'],
                    operation_index,
                    job_index,
                    operation_raw['MachineIndex'],
                    operation_raw['ProcessingTime'],
                    operation_raw['PowerConsumption'] / 100.0
                ))
            jobs.append(Job(
                job_raw['Id'],
                job_index,
                operations
            ))

        return Instance(
            ins_raw['NumMachines'],
            jobs,
            ins_raw['EnergyLimit'] / 100.0,
            ins_raw['Horizon'],
            ins_raw['LengthMeteringInterval'],
            ins_raw['Metadata'],
            instance_filename
        )

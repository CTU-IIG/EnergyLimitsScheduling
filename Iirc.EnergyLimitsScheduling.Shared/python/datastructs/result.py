from typing import Dict, Optional
from enum import IntEnum
from datetime import timedelta
import json
import utils

from datastructs.instance import Instance, Operation

class Status(IntEnum):
    NoSolution = 0,
    Optimal = 1,
    Infeasible = 2,
    Heuristic = 3


class Result:

    __slots__ = [
        'status',
        'time_limit_reached',
        'running_time',
        'start_times'
    ]

    def __init__(
        self,
        status: Status,
        time_limit_reached: bool,
        running_time: timedelta,
        start_times: Optional[Dict[Operation, float]]):
        self.status = status
        self.time_limit_reached = time_limit_reached
        self.running_time = running_time
        self.start_times = start_times

    def to_json(self) -> str:
        d = dict()
        d['Status'] = self.status
        d['TimeLimitReached'] = self.time_limit_reached
        d['RunningTime'] = utils.timedelta_to_str(self.running_time)

        if self.start_times is None:
            d['StartTimes'] = None
        else:
            d['StartTimes'] = [
                {'JobIndex': operation.job_index, 'OperationIndex': operation.index, 'StartTime': start_time }
                for operation, start_time in self.start_times.items()
            ]

        return json.dumps(d)

    @staticmethod
    def from_json(s: str, ins: Instance):
        result_raw = json.loads(s)

        start_times = None
        if result_raw['StartTimes'] is not None:
            start_times = {
                ins.jobs[d['JobIndex']].operations[d['OperationIndex']]: d['StartTime']
                for d in result_raw['StartTimes']
            }

        return Result(
            Status(result_raw['Status']),
            result_raw['TimeLimitReached'],
            utils.parse_timedelta(result_raw['RunningTime']),
            start_times
        )

    def makespan(self) -> Optional[float]:
        if self.start_times is None:
            return None
        else:
            return max([
                operation.processing_time + start_time
                for operation, start_time in self.start_times.items()
            ])

from typing import Tuple, List, Dict, Optional

import matplotlib
matplotlib.rc('font',**{'family':'sans-serif','sans-serif':['Helvetica']})
matplotlib.rc('text', usetex=True)
matplotlib.rc('text.latex', preamble=r'\usepackage{amsmath}')
import numpy as np
import matplotlib.pyplot as plt
import struct
from matplotlib.patches import Rectangle

from datastructs.instance import Instance, Operation, Job

__all__ = [
    'draw'
]


def _generate_colors(n: int) -> List[Tuple[float, float, float]]:
    def scale_rgb_color(r: int, g: int, b: int) -> Tuple[float, float, float]:
        return (r / 255.0, g / 255.0, b / 255.0)

    # Backup old randomness state.
    rand_state = np.random.get_state()
    np.random.seed(0)

    # Some nice-looking default colors.
    colors = [
        scale_rgb_color(*struct.unpack('BBB', bytes.fromhex(color[1:])))
        for color in plt.rcParams['axes.prop_cycle'].by_key()['color']
    ]

    # If needed, add more random colors.
    colors.extend(np.random.rand(3) for _ in range(n - len(colors)))
    del colors[n:]

    # Restore old randomness state.
    np.random.set_state(rand_state)

    return colors

def _get_machine_bottom_y(machine_index: int, num_machines: int, machine_height: int) -> int:
    return (num_machines - 1 - machine_index) * machine_height


def _get_operation_bottom_y(
        operation: Operation,
        num_machines: int,
        machine_height: int,
        operation_margin: int) -> int:
    return _get_machine_bottom_y(operation.machine_index, num_machines, machine_height) + operation_margin

def _compute_intervals_overlap(
        left1: float,
        right1: float,
        left2: float,
        right2: float) -> float:
    return max(0, min(right1, right2) - max(left1, left2))

def draw(
        ins: Instance,
        start_times: Dict[Operation, float],
        title='',
        operation_height: int = 0.8,
        operation_margin: int = 0.1,
        time_units: Optional[str] = None,
        power_consumption_units: Optional[str] = None):
    job_colors = _generate_colors(len(ins.jobs))    # By job index.
    machine_height = operation_height + operation_margin
    last_metering_interval_index = int(round(max([start_time + operation.processing_time
        for operation, start_time in start_times.items()]) / ins.length_metering_interval))
    horizon = (last_metering_interval_index + 1) * ins.length_metering_interval

    gantt_ylim = machine_height * ins.num_machines
    gantt_xlim = horizon

    energy_ylim = ins.energy_limit * 1.1
    energy_xlim = gantt_xlim

    fig = plt.figure(figsize=(8, 4))
    fig.canvas.set_window_title(title)
    gs = matplotlib.gridspec.GridSpec(2, 1, height_ratios=[2, 3])

    # Gantt.
    gantt_ax = fig.add_subplot(gs[0])
    gantt_ax.set_title(title)

    gantt_ax.spines['top'].set_visible(False)
    gantt_ax.spines['right'].set_visible(False)
    gantt_ax.spines['bottom'].set_visible(True)
    gantt_ax.spines['left'].set_visible(False)

    plt.ylim(0, gantt_ylim)
    plt.xlim(0, gantt_xlim)

    if time_units is None:
        plt.xlabel("time")
    else:
        plt.xlabel(f"time [{time_units}]")
    gantt_ax.yaxis.set_visible(False)

    for metering_interval_index in range(1, last_metering_interval_index + 1):
        x = metering_interval_index * ins.length_metering_interval
        plt.plot([x, x], [0, gantt_ylim], "b:", linewidth=1)

    for operation, start_time in start_times.items():
        rect = Rectangle(
            (start_time, _get_operation_bottom_y(operation, ins.num_machines, machine_height, operation_margin)),
            operation.processing_time,
            operation_height,
            facecolor=job_colors[operation.job_index],
            edgecolor="black",
            linewidth=1
        )
        gantt_ax.add_patch(rect)

    # Energy consumption.
    energy_ax = fig.add_subplot(gs[1])

    plt.ylim(0, energy_ylim)
    plt.xlim(0, energy_xlim)

    plt.xlabel("metering intervals")
    plt.xticks(
        [(n + 0.5) * ins.length_metering_interval for n in range(int(horizon / ins.length_metering_interval))],
        np.array(range(int(horizon / ins.length_metering_interval))) + 1)

    if power_consumption_units is None:
        plt.ylabel(u"energy consumption\nin metering interval")
    else:
        plt.ylabel(u"energy consumption\nin metering interval [{units}]".format(units=power_consumption_units))

    energy_ax.xaxis.set_ticks_position('none')
    energy_ax.yaxis.set_ticks_position('left')

    energy_ax.spines['top'].set_visible(False)
    energy_ax.spines['right'].set_visible(False)
    energy_ax.spines['bottom'].set_visible(True)
    energy_ax.spines['left'].set_visible(True)

    for metering_interval_index in range(1, last_metering_interval_index + 1):
        x = metering_interval_index * ins.length_metering_interval
        plt.plot([x, x], [0, energy_ylim], "b:", linewidth=1)

    ordered_operations = sorted(
        start_times.keys(),
        key=lambda operation: (start_times[operation], operation.machine_index))
    for metering_interval_index in range(last_metering_interval_index + 1):
        metering_interval_energy_consumption = 0.0

        metering_interval_start = metering_interval_index * ins.length_metering_interval
        metering_interval_end = (metering_interval_index + 1) * ins.length_metering_interval

        plt.plot(
            [metering_interval_start, metering_interval_start + ins.length_metering_interval],
            [ins.energy_limit, ins.energy_limit],
            "r--", linewidth=2
        )

        for operation in ordered_operations:
            overlap = _compute_intervals_overlap(
                start_times[operation],
                start_times[operation] + operation.processing_time,
                metering_interval_start,
                metering_interval_end
            )

            if not np.isclose(overlap, 0.0):
                energy_consumption = overlap * operation.power_consumption
                stack_width_percent = 0.6
                stack_width = ins.length_metering_interval * stack_width_percent
                stack_space = ins.length_metering_interval * ((1.0 - stack_width_percent) / 2.0)
                rect = Rectangle(
                    (metering_interval_start + stack_space, metering_interval_energy_consumption),
                    stack_width,
                    energy_consumption,
                    facecolor=job_colors[operation.job_index])
                metering_interval_energy_consumption += energy_consumption
                energy_ax.add_patch(rect)


    plt.tight_layout()

if __name__ == '__main__':
    jobs = [
        Job(
            0, 0,
            [
                Operation(
                    0, 0, 0, 1, 5, 14.0
                ),
                Operation(
                    1, 1, 0, 0, 7, 23.0
                ),
                Operation(
                    2, 2, 0, 2, 3, 13.0
                ),
            ]
        ),
        Job(
            1, 1,
            [
                Operation(
                    3, 0, 1, 2, 2, 12.0
                ),
                Operation(
                    4, 1, 1, 1, 12, 28.0
                ),
                Operation(
                    5, 2, 1, 0, 35, 10.0
                ),
            ]
        ),
    ]

    ins = Instance(3, jobs, 600.0, 90, 15)
    start_times = {
        jobs[0].operations[0]: 0,
        jobs[0].operations[1]: 5,
        jobs[0].operations[2]: 12,
        jobs[1].operations[0]: 0,
        jobs[1].operations[1]: 5,
        jobs[1].operations[2]: 17,
    }

    draw(ins, start_times)
    plt.show()

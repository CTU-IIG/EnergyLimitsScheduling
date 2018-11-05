import argparse
from pathlib import Path

import matplotlib.pyplot as plt

from datastructs.instance import Instance
from datastructs.result import Result

import vizualization.gantt

def _parse_args():
    parser = argparse.ArgumentParser(description='Show a gantt chart for a energy limits scheduling result.')
    parser.add_argument(
        'instance_path',
        metavar='INSTANCE_PATH',
        type=str,
        help='Path to the instance file.')
    parser.add_argument(
        'result_path',
        metavar='RESULT_PATH',
        type=str,
        help='Path to the result file.')

    return parser.parse_args()

def main():
    args = _parse_args()

    args.instance_path = Path(args.instance_path).resolve()
    args.result_path = Path(args.result_path).resolve()
    
    instance = Instance.from_json(args.instance_path.read_text())
    result = Result.from_json(args.result_path.read_text(), instance)

    vizualization.gantt.draw(
        instance,
        result.start_times,
        time_units='min',
        power_consumption_units='MWh')

    plt.show()



if __name__ == '__main__':
    main()
import math
import argparse
import numpy as np
from typing import List, Dict, Tuple
from pathlib import Path
from datastructs.instance import Instance
from datastructs.result import Result, Status
from matplotlib import rc
rc('text', usetex=True)
import matplotlib.pyplot as plt
import pandas as pd
import webbrowser
import os

class GroupedInstances:

    __slots__ = [
        'params',
        'instances',
    ]

    def __init__(self, params: Dict[str, object], instances: List[Instance]):
        self.params = params
        self.instances = instances

def _load_instances(dataset_path: Path) -> List[Instance]:
    return [Instance.from_json(child_path.read_text(), child_path.name)
            for child_path in dataset_path.iterdir() if child_path.is_file()]

def _group_instances(group_params: List[str], instances: List[Instance]) -> List[GroupedInstances]:
    d: Dict[frozenset, List[Instance]] = dict()
    for instance in instances:
        params = {param: value for param, value in instance.metadata.items() if param in group_params}
        key = frozenset(params.items())
        if key not in d:
            d[key] = []
        d[key].append(instance)

    return [GroupedInstances(dict(key), instances) for key, instances in d.items()]

def _get_result_path(dataset_results_path: Path, solver_id: str, instance_filename: str):
    return dataset_results_path / solver_id / instance_filename

def _get_solver_ids(dataset_results_path: Path) -> List[str]:
    return [child_path.name for child_path in dataset_results_path.iterdir() if child_path.is_dir()]

def _results_table_to_latex(
        df: pd.DataFrame,
        solver_ids: List[str],
        solvers_display: Dict[str, str],
        group_params: List[str],
        group_params_display: Dict[str, str]) -> str:
    columns = group_params + solver_ids
    content_template = ' & '.join(['{' + column + '}' for column in columns]) + r'\\' + os.linesep

    tbl_content = ''
    tbl_content += ' & '.join(\
        [group_params_display[group_param] for group_param in group_params]
        + [solvers_display[solver_id] for solver_id in solver_ids]) + r'\\' + r'\midrule' + os.linesep
    for index, row in df.iterrows():
        max_value = row[solver_ids].max()
        row_dict = row.to_dict()
        for solver_id in solver_ids:
            if row_dict[solver_id] == max_value:
                row_dict[solver_id] = r'\textbf{{{value}}}'.format(value=row_dict[solver_id])
        tbl_content += content_template.format(**row_dict)

    table_spec = 'c'*len(group_params) + '|' + 'r'*len(solver_ids)
    tbl = \
r"""
\begin{{tabular}}{{{table_spec}}}
\toprule
{tbl_content}\bottomrule
\end{{tabular}}
""".format(tbl_content=tbl_content, table_spec=table_spec)

    return tbl

def _results_table_to_graph(
        df: pd.DataFrame,
        solver_ids: List[str],
        solvers_display: Dict[str, str],
        group_params: List[str],
        group_params_display: Dict[str, str],
        results_graph_path: Path) -> str:
    fig = plt.figure()
    for solver_id in solver_ids:
        plt.plot(df[group_params[-1]], df[solver_id])

    plt.legend([solvers_display[solver_id] for solver_id in solver_ids])
    plt.xlabel(group_params_display[group_params[-1]])
    plt.ylabel("Num. proved optimal solutions")
    plt.savefig(str(results_graph_path))

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument(
        'datasets_path',
        metavar='DATASETS_PATH',
        type=str,
        help='Path to the datasets directory.')
    parser.add_argument(
        'results_path',
        metavar='RESULTS_PATH',
        type=str,
        help='Path to the results directory.')
    parser.add_argument(
        'dataset',
        metavar='DATASET',
        type=str,
        help='The name of the dataset for which to perform analysis.')
    parser.add_argument(
        '--results-graph-path',
        dest='results_graph_path',
        metavar='RESULTS_GRAPH_PATH',
        type=str,
        nargs='?',
        default=None,
        help='The path where to store the results graph.')
    parser.add_argument(
        '--solvers',
        dest='solvers',
        metavar='SOLVERS',
        nargs='+',
        type=str,
        help='The ids of solvers for which to show the results and their order.')
    parser.add_argument(
        '--solvers-display',
        dest='solvers_display',
        metavar='SOLVERS_DISPLAY',
        nargs='+',
        type=str,
        help='The display names of the solvers.')
    parser.add_argument(
        '--group-params',
        dest='group_params',
        metavar='GROUP_PARAMS',
        nargs='+',
        type=str,
        help='Parameters in instance metadata according to which group instances.')
    parser.add_argument(
        '--group-params-display',
        dest='group_params_display',
        metavar='GROUP_PARAMS_DISPLAY',
        nargs='+',
        type=str,
        help='The display names for the group params.')

    args = parser.parse_args()

    args.datasets_path = Path(args.datasets_path).resolve()
    args.results_path = Path(args.results_path).resolve()

    dataset_path: Path = args.datasets_path / args.dataset
    instances = _load_instances(dataset_path)

    dataset_results_path = args.results_path / args.dataset

    groups = _group_instances(args.group_params, instances)

    if args.solvers is None or not args.solvers:
        solver_ids = _get_solver_ids(dataset_results_path)
    else:
        solver_ids = args.solvers

    if args.solvers_display is None or not args.solvers_display:
        solvers_display = {
            solver_id: solver_id
            for solver_id in solver_ids
        }
    else:
        solvers_display = {
            solver_id: solver_display
            for solver_id, solver_display in zip(solver_ids, args.solvers_display)
        }

    if args.group_params_display is None or not args.group_params_display:
        group_params_display = {
            group_param: group_param
            for group_param in args.group_params
        }
    else:
        group_params_display = {
            group_param: group_param_display
            for group_param, group_param_display in zip(args.group_params, args.group_params_display)
        }

    # Construct pandas dataframe containing the results data.
    series = []

    # Params series.
    for param in args.group_params:
        series.append(pd.Series([group.params[param] for group in groups], name=param))

    # Per solver series.
    for solver_id in solver_ids:
        solver_series = []
        solver_metering_interval_iterations = []
        for group in groups:
            num_optimals = 0
            for instance in group.instances:
                result_path = _get_result_path(dataset_results_path, solver_id, instance.instance_filename)
                if result_path.exists():
                    result = Result.from_json(result_path.read_text(), instance)
                    if result.status == Status.Optimal:
                        num_optimals += 1

                    if result.status == Status.Optimal:
                        makespan = int(result.makespan())
                        horizon = int(instance.horizon)
                        if horizon % instance.length_metering_interval == 0:
                            right_boundary = horizon
                        else:
                            right_boundary = horizon + instance.length_metering_interval - (horizon % instance.length_metering_interval)
                        if makespan % instance.length_metering_interval == 0:
                            left_boundary = makespan - instance.length_metering_interval
                        else:
                            left_boundary = makespan - (makespan % instance.length_metering_interval)
                        solver_metering_interval_iterations.append((right_boundary - left_boundary) / instance.length_metering_interval)
            solver_series.append(num_optimals)
        # num_bins = int(math.ceil(int(max(solver_metering_interval_iterations)) / 10))
        # plt.figure(figsize=(4,3))
        # plt.hist(
        #     solver_metering_interval_iterations,
        #     bins=[i * 10 for i in range(0, num_bins + 1)],
        #     rwidth=0.75)
        # plt.title(solvers_display[solver_id])
        # plt.xticks([5 + 10 * i for i in range(num_bins)])
        # plt.xticks(
        #     [5 + 10 * i for i in range(num_bins)],
        #     ["[{0}, {1})".format(10 * i, 10 * (i + 1)) for i in range(num_bins)],
        # )
        # plt.xlabel("num. iterations")
        # plt.ylabel("num. optimally solved instances")
        # plt.tight_layout()
        # plt.show()
        series.append(pd.Series(solver_series, name=solver_id, dtype=np.object))
        
    df = pd.concat(series, axis=1)
    df = df.sort_values(by=args.group_params)

    df_latex = _results_table_to_latex(df, solver_ids, solvers_display, args.group_params, group_params_display)
    print(df_latex)

    if args.results_graph_path is not None:
        _results_table_to_graph(
            df, solver_ids, solvers_display, args.group_params, group_params_display, Path(args.results_graph_path).resolve())

    html_file = Path('index.html').resolve()
    html_file.write_text(df.to_html())

    webbrowser.open(str(html_file), new=2)

if __name__ == '__main__':
    main()

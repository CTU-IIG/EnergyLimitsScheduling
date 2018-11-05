from docplex.cp.solution import CpoSolveResult
from docplex.cp.model import SOLVE_STATUS_FEASIBLE, SOLVE_STATUS_INFEASIBLE, SOLVE_STATUS_OPTIMAL, FAIL_STATUS_TIME_LIMIT

from datastructs.result import Status

def get_result_status(solve_result: CpoSolveResult) -> Status:
    solve_status = solve_result.get_solve_status()
    if solve_status == SOLVE_STATUS_FEASIBLE:
        return Status.Heuristic
    elif solve_status == SOLVE_STATUS_OPTIMAL:
        return Status.Optimal
    elif solve_status == SOLVE_STATUS_INFEASIBLE:
        return Status.Infeasible
    else:
        return Status.NoSolution

def time_limit_reached(solve_result: CpoSolveResult) -> bool:
    return solve_result.get_fail_status() == FAIL_STATUS_TIME_LIMIT

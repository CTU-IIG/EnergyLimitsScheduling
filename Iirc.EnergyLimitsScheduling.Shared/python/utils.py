from datetime import timedelta
import re
import math

def parse_timedelta(s):
    """Create timedelta object representing time delta
       expressed in a string

    Takes a string in the format produced by calling str() on
    a python timedelta object and returns a timedelta instance
    that would produce that string.

    Acceptable formats are: "X days, HH:MM:SS" or "HH:MM:SS".

    Source: https://kbyanc.blogspot.com/2007/08/python-reconstructing-timedeltas-from.html
    """
    if s is None:
        return None
    d = re.match(
        r'((?P<days>\d+) days, )?(?P<hours>\d+):'
        r'(?P<minutes>\d+):(?P<seconds>\d+)',
        str(s)).groupdict(0)
    return timedelta(**dict(((key, int(value))
                             for key, value in d.items())))

def timedelta_to_str(td: timedelta) -> str:
    s = math.ceil(td.total_seconds())
    hours = s // 3600 
    s = s - (hours * 3600)
    minutes = s // 60
    seconds = s - (minutes * 60)
    return '%d:%d:%d' % (hours, minutes, seconds)
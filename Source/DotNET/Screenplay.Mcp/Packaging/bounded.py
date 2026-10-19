# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Run a packaging tool with a deadline and clean up its whole process tree.

An archive the tool printed is not proof that it finished: only a zero exit within the
deadline counts as success. Any other outcome raises, so no later packaging step runs.
"""
import os
import signal
import subprocess
import sys

WINDOWS = sys.platform == "win32"


class PackagingToolFailed(RuntimeError):
    """The tool exited with a non-zero status."""


class PackagingToolTimedOut(RuntimeError):
    """The tool did not exit before its deadline and was stopped with its process tree."""


def _start(command):
    if WINDOWS:
        return subprocess.Popen(command, creationflags=subprocess.CREATE_NEW_PROCESS_GROUP)
    # A new session makes the tool the leader of a process group that holds every descendant.
    return subprocess.Popen(command, start_new_session=True)


def _kill_tree(process):
    if WINDOWS:
        subprocess.run(["taskkill", "/T", "/F", "/PID", str(process.pid)],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False)
        process.kill()
        return
    try:
        os.killpg(process.pid, signal.SIGKILL)
    except (ProcessLookupError, PermissionError):
        pass
    if process.returncode is None:
        process.kill()


def run_bounded(command, timeout, label):
    """Run command, waiting at most timeout seconds; raise unless it exits zero in time."""
    process = _start(command)
    try:
        code = process.wait(timeout=timeout)
    except subprocess.TimeoutExpired:
        _kill_tree(process)
        process.wait()
        raise PackagingToolTimedOut(
            f"{label} did not exit within {timeout} seconds and was stopped with its child processes; "
            "its output is not trusted and packaging stops here") from None
    except BaseException:
        _kill_tree(process)
        process.wait()
        raise
    # Descendants left behind by a tool that exited would otherwise outlive the build step.
    if not WINDOWS:
        _kill_tree(process)
    if code != 0:
        raise PackagingToolFailed(f"{label} exited with status {code}; packaging stops here")

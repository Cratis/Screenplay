# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
import os
import sys
import tempfile
import time
import unittest
from pathlib import Path
from unittest import mock
import build
from bounded import PackagingToolFailed, PackagingToolTimedOut, WINDOWS, run_bounded

PYTHON = sys.executable
# Starts a child that would outlive its parent, records the child's id, then hangs.
HANGING_PARENT = """
import subprocess, sys, time
child = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(120)"])
open(sys.argv[1], "w").write(str(child.pid))
print("archive written", flush=True)
time.sleep(120)
"""


def alive(pid):
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    return True


def wait_until_gone(pid, seconds=5):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        if not alive(pid):
            return True
        time.sleep(0.05)
    return False


class BoundedTool(unittest.TestCase):
    def test_a_zero_exit_succeeds(self):
        run_bounded([PYTHON, "-c", "pass"], 30, "tool")

    def test_a_non_zero_exit_fails(self):
        with self.assertRaisesRegex(PackagingToolFailed, "tool exited with status 3"):
            run_bounded([PYTHON, "-c", "raise SystemExit(3)"], 30, "tool")

    def test_a_tool_that_does_not_exit_is_stopped_after_its_deadline(self):
        with tempfile.TemporaryDirectory() as folder:
            pid_file = Path(folder) / "child.pid"
            started = time.monotonic()
            with self.assertRaisesRegex(PackagingToolTimedOut, "mcpb pack did not exit within 1 seconds"):
                run_bounded([PYTHON, "-c", HANGING_PARENT, str(pid_file)], 1, "mcpb pack")
            self.assertLess(time.monotonic() - started, 30)
            if not WINDOWS:
                self.assertTrue(wait_until_gone(int(pid_file.read_text())), "child process outlived the timeout")

    @unittest.skipIf(WINDOWS, "a Windows process tree cannot be found once its root has exited")
    def test_children_left_behind_by_a_finished_tool_are_stopped(self):
        with tempfile.TemporaryDirectory() as folder:
            pid_file = Path(folder) / "child.pid"
            script = ("import subprocess, sys; child = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(120)']);"
                      " open(sys.argv[1], 'w').write(str(child.pid))")
            run_bounded([PYTHON, "-c", script, str(pid_file)], 30, "tool")
            self.assertTrue(wait_until_gone(int(pid_file.read_text())), "child process outlived the tool")


class PackingStops(unittest.TestCase):
    def test_no_archive_or_checksum_follows_a_failed_or_stuck_mcpb(self):
        for failure in (PackagingToolTimedOut("stuck"), PackagingToolFailed("failed")):
            for failing_step in ("validate", "pack"):
                with self.subTest(failure=type(failure).__name__, step=failing_step), tempfile.TemporaryDirectory() as folder:
                    root = Path(folder)
                    publish = root / "publish"
                    publish.mkdir()
                    (publish / "Cratis.Screenplay.Tool").write_bytes(b"self-contained fixture")
                    calls = []

                    def run(command, timeout, label, failing_step=failing_step, failure=failure):
                        calls.append((command[1], timeout))
                        if command[1] == failing_step:
                            raise failure

                    with mock.patch.object(build.shutil, "which", return_value="mcpb"), mock.patch.object(build, "run_bounded", run):
                        with self.assertRaises(type(failure)):
                            build.pack(publish, root / "packages", "4.55.0", "osx-arm64", mcpb_timeout=7)
                    self.assertTrue(all(timeout == 7 for _, timeout in calls))
                    self.assertEqual(calls[-1][0], failing_step)
                    self.assertEqual(list((root / "packages").glob("*.zip")), [])
                    self.assertEqual(list((root / "packages").glob("*.sha256")), [])


if __name__ == "__main__":
    unittest.main()

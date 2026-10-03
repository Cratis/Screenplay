# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Unpack an artifact and exercise its real self-contained server over stdio."""
import argparse
import json
import os
import tempfile
import subprocess
import zipfile
from pathlib import Path


def smoke(artifact):
    with tempfile.TemporaryDirectory() as folder:
        root = Path(folder).resolve()
        with zipfile.ZipFile(artifact) as archive:
            archive.extractall(root)
        if artifact.suffix == ".mcpb":
            manifest = json.loads((root / "manifest.json").read_text())
            command = root / manifest["server"]["entry_point"]
            arguments = [value.replace("${user_config.model_root}", str(root / "model")) for value in manifest["server"]["mcp_config"]["args"]]
            (root / "model").mkdir()
        else:
            manifest = json.loads((root / "mcp.json").read_text())
            command = root / manifest["mcpServers"]["screenplay"]["command"]
            arguments = [value.replace("${PLUGIN_DATA}", str(root / "data")) for value in manifest["mcpServers"]["screenplay"]["args"]]
        if os.name != "nt":
            command.chmod(0o755)
        messages = [
            {"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {"protocolVersion": "2025-03-26", "capabilities": {"extensions": {"io.modelcontextprotocol/ui": {"mimeTypes": ["text/html;profile=mcp-app"]}}}, "clientInfo": {"name": "desktop-package-smoke", "version": "1.0.0"}}},
            {"jsonrpc": "2.0", "method": "notifications/initialized"},
            {"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}},
            {"jsonrpc": "2.0", "id": 3, "method": "resources/list", "params": {}},
            {"jsonrpc": "2.0", "id": 4, "method": "tools/call", "params": {"name": "describe-application", "arguments": {}}}
        ]
        model = Path(arguments[-1])
        if artifact.suffix != ".mcpb":
            initial = subprocess.run([str(command), *arguments], input=json.dumps(messages[0]) + "\n", text=True, capture_output=True, timeout=60)
            assert initial.returncode == 0, initial.stderr
            assert model.is_dir(), "Plugin did not create its default persistent model directory"
        (model / "desktop.play").write_text('module DesktopSmoke\n  description "A packaged desktop model"\n', encoding="utf-8")
        result = subprocess.run([str(command), *arguments], input="".join(json.dumps(m) + "\n" for m in messages), text=True, capture_output=True, timeout=60)
        assert result.returncode == 0, result.stderr
        replies = {m["id"]: m for m in map(json.loads, result.stdout.splitlines()) if "id" in m}
        assert set(replies) == {1, 2, 3, 4}, replies
        assert all("error" not in m for m in replies.values()), replies
        assert replies[1]["result"]["serverInfo"]["name"], replies[1]
        assert "describe-application" in {tool["name"] for tool in replies[2]["result"]["tools"]}, replies[2]
        assert "resources" in replies[3]["result"], replies[3]
        assert not replies[4]["result"].get("isError", False), replies[4]
        print(f"MCP initialization, tools, resources, and application read passed: {artifact.name}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifacts", nargs="+", type=Path)
    for artifact in parser.parse_args().artifacts:
        smoke(artifact)

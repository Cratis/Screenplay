# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Package a native publish directory. No server implementation lives here."""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
import zipfile
from pathlib import Path
import jsonschema

RIDS = {"osx-arm64": "darwin", "osx-x64": "darwin", "win-x64": "win32", "linux-x64": "linux", "linux-arm64": "linux"}
HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def prepare(publish, output, version, rid):
    if rid not in RIDS or not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", version):
        raise ValueError("Expected a supported RID and a semantic release version")
    executable = "Cratis.Screenplay.Tool" + (".exe" if rid == "win-x64" else "")
    if not (publish / executable).is_file():
        raise ValueError(f"Missing self-contained executable: {publish / executable}")
    for kind in ("mcpb", "plugin"):
        package = output / kind
        if package.exists():
            shutil.rmtree(package)
        (package / "server").mkdir(parents=True)
        for file in publish.iterdir():
            if file.is_dir():
                shutil.copytree(file, package / "server" / file.name)
            else:
                shutil.copy2(file, package / "server" / file.name)
        shutil.copy2(REPO / "LICENSE", package / "LICENSE")
        shutil.copy2(HERE / "icon.png", package / "icon.png")
    manifest = {
        "manifest_version": "0.3", "name": "cratis-screenplay", "display_name": "Screenplay",
        "version": version, "description": "Explore and author local Screenplay models with reviewed changes and an event model board.",
        "author": {"name": "Cratis", "url": "https://cratis.io"},
        "homepage": "https://github.com/Cratis/Screenplay", "license": "MIT", "icon": "icon.png",
        # No root argument and no user_config: the server binds its workspace dynamically from the
        # host's MCP roots, the working directory, or an explicit open-workspace call.
        "server": {"type": "binary", "entry_point": f"server/{executable}", "mcp_config": {
            "command": f"${{__dirname}}/server/{executable}", "args": ["mcp"]}},
        "compatibility": {"platforms": [RIDS[rid]]}
    }
    write_json(output / "mcpb" / "manifest.json", manifest)
    plugin = {
        "$schema": "https://agent-plugins.org/schemas/1.0.0/plugin.schema.json",
        "name": "cratis-screenplay", "version": version,
        "description": manifest["description"], "author": manifest["author"],
        "homepage": manifest["homepage"], "repository": manifest["homepage"], "license": "MIT",
        "keywords": ["screenplay", "event-modeling", "local-first"],
        "extensions": {"com.openai": {"interface": {"displayName": "Screenplay", "shortDescription": "Explore and author local business models",
            "composerIcon": "./icon.png", "logo": "./icon.png", "developerName": "Cratis", "category": "Productivity",
            "capabilities": ["Read", "Write"], "defaultPrompt": ["Explore my Screenplay model and explain its modules, features, and slices."]}}}
    }
    write_json(output / "plugin" / "plugin.json", plugin)
    write_json(output / "plugin" / "mcp.json", {
        "$schema": "https://agent-plugins.org/schemas/1.0.0/mcp.schema.json",
        "mcpServers": {"screenplay": {"type": "stdio", "command": f"./server/{executable}",
            "args": ["mcp", "--create-root", "${PLUGIN_DATA}/model"]}}})
    skill = output / "plugin" / "skills" / "screenplay"
    skill.mkdir(parents=True)
    shutil.copy2(HERE / "SKILL.md", skill / "SKILL.md")
    write_json(output / "marketplace.json", {"name": "cratis", "interface": {"displayName": "Cratis"}, "plugins": [{
        "name": "cratis-screenplay", "source": {"source": "local", "path": "./plugins/cratis-screenplay"},
        "policy": {"installation": "AVAILABLE", "authentication": "ON_INSTALL"}, "category": "Productivity"}]})
    return executable


def pack(publish, output, version, rid):
    prepare(publish, output, version, rid)
    for name in ("plugin", "mcp"):
        schema = json.loads((HERE / "Schemas" / f"{name}.schema.json").read_text(encoding="utf-8"))
        document = json.loads((output / "plugin" / f"{name}.json").read_text(encoding="utf-8"))
        jsonschema.Draft202012Validator(schema).validate(document)
    # Pinned by the workflow (and documented for local builds), never fetched implicitly here.
    mcpb = shutil.which("mcpb")
    if mcpb is None:
        raise ValueError("Install @anthropic-ai/mcpb@2.1.2 before packaging")
    subprocess.run([mcpb, "validate", str(output / "mcpb")], check=True)
    bundle = output / f"screenplay-{version}-{rid}.mcpb"
    subprocess.run([mcpb, "pack", str(output / "mcpb"), str(bundle)], check=True)
    plugin = output / f"screenplay-{version}-{rid}-plugin.zip"
    with zipfile.ZipFile(plugin, "w", zipfile.ZIP_DEFLATED) as archive:
        for file in sorted((output / "plugin").rglob("*")):
            if file.is_file():
                archive.write(file, file.relative_to(output / "plugin").as_posix())
    for artifact in (bundle, plugin):
        with artifact.open("rb") as stream:
            digest = hashlib.file_digest(stream, "sha256").hexdigest()
        (output / (artifact.name + ".sha256")).write_text(digest + "  " + artifact.name + "\n", encoding="utf-8", newline="\n")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--publish", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--rid", choices=RIDS, required=True)
    args = parser.parse_args()
    pack(args.publish.resolve(), args.output.resolve(), args.version, args.rid)

# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Assert the Release NuGet package stays slim and the compiler depends on it.

Run after `dotnet pack -c Release -o <directory> -p:Version=<version>`:
    python3 Source/DotNET/Screenplay.Contexts/verify-package.py <directory> <version>
"""

import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path


output, version = Path(sys.argv[1]), sys.argv[2]
frameworks = {"net8.0", "net9.0", "net10.0"}


def inspect(package):
    path = output / f"{package}.{version}.nupkg"
    with zipfile.ZipFile(path) as archive:
        files = set(archive.namelist())
        metadata = ET.fromstring(archive.read(f"{package}.nuspec")).find("{*}metadata")
        groups = metadata.find("{*}dependencies").findall("{*}group")
        dependencies = {
            group.attrib["targetFramework"]: {dep.attrib["id"] for dep in group.findall("{*}dependency")}
            for group in groups
        }
    assert set(dependencies) == frameworks, dependencies
    return files, dependencies


contexts_files, contexts_deps = inspect("Cratis.Screenplay.Contexts")
for framework in frameworks:
    assert f"lib/{framework}/Cratis.Screenplay.Contexts.dll" in contexts_files
    assert contexts_deps[framework] == set(), contexts_deps
assert not any("Cratis.Screenplay.dll" in name or "Specs.dll" in name for name in contexts_files)

compiler_files, compiler_deps = inspect("Cratis.Screenplay")
for framework in frameworks:
    assert f"lib/{framework}/Cratis.Screenplay.dll" in compiler_files
    assert "Cratis.Screenplay.Contexts" in compiler_deps[framework]
print("All context package content and dependency assertions passed")

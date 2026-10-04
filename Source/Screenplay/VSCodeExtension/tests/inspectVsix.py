# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Reject accidental test/dev/server payloads; the native host verifies runtime resolution."""
import json
import sys
import zipfile

with zipfile.ZipFile(sys.argv[1]) as package:
    names = package.namelist()
    assert 'extension/out/extension.js' in names
    assert 'extension/out/webview.js' in names
    for name in names:
        assert not name.endswith(('.ts', '.map', '.dll', '.exe', '.cs', '.node', '.so', '.dylib', '.pdb')), name
        assert '/tests/' not in name and '/for_' not in name and 'vscode.stub' not in name, name
        assert '/node_modules/' not in name, name
    runtime = package.read('extension/out/extension.js')
    assert b'WatchUnavailable' in runtime and b'WatchInvalidated' in runtime
    assert b'node:fs' in runtime
    manifest = json.loads(package.read('extension/package.json'))
    commands = {command['command'] for command in manifest['contributes']['commands']}
    assert {'screenplay.repair.apply', 'screenplay.repair.discard'} <= commands
    print(f'VSIX inspection passed: {len(names)} entries; bundled runtime, contributed review commands, no harness or .NET payload.')

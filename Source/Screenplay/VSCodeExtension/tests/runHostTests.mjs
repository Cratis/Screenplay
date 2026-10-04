// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { runTests } from '@vscode/test-electron';
import * as fs from 'node:fs';
import * as path from 'node:path';

// Never download/start a guessed browser runtime. Use an explicit native VS Code installation.
const executable = process.env.VSCODE_EXECUTABLE_PATH;
if (!executable || !path.isAbsolute(executable) || !fs.existsSync(executable)) throw new Error('Set VSCODE_EXECUTABLE_PATH to the installed native VS Code executable. Host coverage is unverified without it.');
const server = process.env.SCREENPLAY_REPAIR_SERVER ?? path.resolve('../../DotNET/Tool/bin/Debug/net10.0/Cratis.Screenplay.Tool' + (process.platform === 'win32' ? '.exe' : ''));
if (!fs.existsSync(server)) throw new Error('Build the native C# MCP tool first or set SCREENPLAY_REPAIR_SERVER.');
const tasks = path.resolve('../../..', '.ai-work');
fs.mkdirSync(tasks, { recursive: true });
const testRoot = fs.mkdtempSync(path.join(tasks, 'h'));
const model = path.join(testRoot, 'model');
const userData = path.join(testRoot, 'u');
fs.mkdirSync(model); fs.mkdirSync(path.join(userData, 'User'), { recursive: true });
fs.writeFileSync(path.join(userData, 'User/settings.json'), JSON.stringify({
    'screenplay.repairs.enabled': true, 'screenplay.repairs.executable': server, 'screenplay.repairs.arguments': ['mcp'], 'screenplay.repairs.modelRoot': model,
    'workbench.editorAssociations': { '*.play': 'default' }, 'files.autoSave': 'off', 'window.restoreWindows': 'none',
}));
await runTests({
    vscodeExecutablePath: executable,
    extensionDevelopmentPath: path.resolve('.'), extensionTestsPath: path.resolve('out/tests/extensionHost.cjs'),
    launchArgs: [model, '--user-data-dir', userData, '--extensions-dir', path.join(testRoot, 'extensions'), '--disable-workspace-trust', '--skip-welcome', '--skip-release-notes', '--disable-gpu'],
    extensionTestsEnv: { SCREENPLAY_REPAIR_SERVER: server, SCREENPLAY_REPAIR_HOST_ROOT: model },
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { runTests } from '@vscode/test-electron';
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as os from 'node:os';

// Never download/start a guessed browser runtime. Use an explicit native VS Code installation.
const executable = process.env.VSCODE_EXECUTABLE_PATH;
if (!executable || !path.isAbsolute(executable) || !fs.existsSync(executable)) throw new Error('Set VSCODE_EXECUTABLE_PATH to the installed native VS Code executable. Host coverage is unverified without it.');
const server = process.env.SCREENPLAY_REPAIR_SERVER ?? path.resolve('../../DotNET/Tool/bin/Debug/net10.0/Cratis.Screenplay.Tool' + (process.platform === 'win32' ? '.exe' : ''));
if (!fs.existsSync(server)) throw new Error('Build the native C# MCP tool first or set SCREENPLAY_REPAIR_SERVER.');
const tasks = path.resolve('../../..', '.ai-work');
fs.mkdirSync(tasks, { recursive: true });
const testRoot = fs.mkdtempSync(path.join(tasks, 'h'));
// Only these newly generated synthetic inputs live outside the checkout. On macOS
// /Volumes is refused by VS Code's native watcher; resolve /var's system symlink
// before authorizing the physical root. User data and all logs stay in .ai-work.
const model = fs.mkdtempSync(path.join(fs.realpathSync.native(os.tmpdir()), 'screenplay-repair-host-'));
fs.writeFileSync(path.join(testRoot, 'workspace-location.json'), JSON.stringify({ model, synthetic: true }));
console.log(`Native synthetic workspace: ${model}; retained host data: ${testRoot}`);
const userData = path.join(testRoot, 'u');
fs.mkdirSync(path.join(userData, 'User'), { recursive: true });
fs.writeFileSync(path.join(userData, 'User/settings.json'), JSON.stringify({
    'screenplay.repairs.enabled': true, 'screenplay.repairs.executable': server, 'screenplay.repairs.arguments': ['mcp'], 'screenplay.repairs.modelRoot': model,
    'workbench.editorAssociations': { '*.play': 'default' }, 'files.autoSave': 'off', 'window.restoreWindows': 'none',
    // Explicit provider commands drive these tests; background lightbulb probes
    // would race those calls for the intentionally single-flight C# session.
    'editor.lightbulb.enabled': 'off',
}));
await runTests({
    vscodeExecutablePath: executable,
    extensionDevelopmentPath: path.resolve('.'), extensionTestsPath: path.resolve('out/tests/extensionHost.cjs'),
    launchArgs: [model, '--user-data-dir', userData, '--extensions-dir', path.join(testRoot, 'extensions'), '--disable-workspace-trust', '--skip-welcome', '--skip-release-notes', '--disable-gpu', '--log', 'trace'],
    extensionTestsEnv: { SCREENPLAY_REPAIR_SERVER: server, SCREENPLAY_REPAIR_HOST_ROOT: model },
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { runTests, resolveCliArgsFromVSCodeExecutablePath } from '@vscode/test-electron';
import { spawnSync } from 'node:child_process';
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as os from 'node:os';
import { createRequire } from 'node:module';

// Never download/start a guessed browser runtime. Use an explicit native VS Code installation.
const executable = process.env.VSCODE_EXECUTABLE_PATH;
if (!executable || !path.isAbsolute(executable) || !fs.existsSync(executable)) throw new Error('Set VSCODE_EXECUTABLE_PATH to the installed native VS Code executable. Host coverage is unverified without it.');
const server = process.env.SCREENPLAY_REPAIR_SERVER ?? path.resolve('../../DotNET/Tool/bin/Debug/net10.0/Cratis.Screenplay.Tool' + (process.platform === 'win32' ? '.exe' : ''));
if (!fs.existsSync(server)) throw new Error('Build the native C# MCP tool first or set SCREENPLAY_REPAIR_SERVER.');
const tasks = path.resolve('../../..', '.ai-work');
fs.mkdirSync(tasks, { recursive: true });
const retained = process.env.AI_WORK_KEEP ?? tasks;
fs.mkdirSync(retained, { recursive: true });
const evidence = fs.mkdtempSync(path.join(retained, 'native-host-'));
// Native sockets have short path limits; avoid /var symlink aliases and
// VS Code's nonrecursive /Volumes watcher policy. Recursive watching is tested separately.
// Both synthetic inputs and host data use the physical temporary FS;
// preserve their locations and copy native logs to retained evidence on exit.
const temporary = fs.realpathSync.native(os.tmpdir());
const testRoot = fs.mkdtempSync(path.join(temporary, 'sp-host-'));
const model = fs.mkdtempSync(path.join(temporary, 'screenplay-repair-host-'));
createRequire(import.meta.url)('../out/tests/prepareHostFixtures.cjs').prepareHostFixtures(model);
fs.writeFileSync(path.join(evidence, 'workspace-location.json'), JSON.stringify({ model, testRoot, synthetic: true }));
console.log(`Native synthetic workspace: ${model}; native host data: ${testRoot}; retained evidence: ${evidence}`);
const userData = path.join(testRoot, 'u');
fs.mkdirSync(path.join(userData, 'User'), { recursive: true });
fs.writeFileSync(path.join(userData, 'User/settings.json'), JSON.stringify({
    'screenplay.repairs.enabled': true, 'screenplay.repairs.executable': server, 'screenplay.repairs.arguments': ['mcp'], 'screenplay.repairs.modelRoot': path.join(model, 'direct'),
    'workbench.editorAssociations': { '*.play': 'default' }, 'files.autoSave': 'off', 'window.restoreWindows': 'none',
    'extensions.autoUpdate': false, 'extensions.autoCheckUpdates': false, 'update.mode': 'none',
    'chat.disableAIFeatures': true,
}));
const extensions = path.join(testRoot, 'extensions');
let development = path.resolve('.');
const vsix = process.env.SCREENPLAY_REPAIR_VSIX;
if (vsix) {
    if (!path.isAbsolute(vsix) || !fs.existsSync(vsix)) throw new Error('SCREENPLAY_REPAIR_VSIX must identify the packaged VSIX.');
    const [cli, ...args] = resolveCliArgsFromVSCodeExecutablePath(executable, { reuseMachineInstall: true });
    const installed = spawnSync(cli, [...args, '--install-extension', vsix, '--extensions-dir', extensions, '--user-data-dir', userData, '--force'], { stdio: 'inherit', timeout: 60_000, shell: process.platform === 'win32' });
    if (installed.error || installed.status !== 0) throw installed.error ?? new Error(`VSIX installation failed: ${installed.status}`);
    // Only a tiny test driver is developed. cratis.screenplay MUST resolve from
    // the installed VSIX, not a development-path override of production sources.
    development = path.join(testRoot, 'driver');
    fs.mkdirSync(development);
    fs.writeFileSync(path.join(development, 'package.json'), JSON.stringify({ name: 'screenplay-repair-test-driver', publisher: 'cratis-tests', version: '0.0.0', engines: { vscode: '^1.85.0' } }));
}
let shutdownFailure;
try { await runTests({
    vscodeExecutablePath: executable,
    extensionDevelopmentPath: development, extensionTestsPath: path.resolve('out/tests/extensionHost.cjs'),
    launchArgs: [model, '--user-data-dir', userData, '--extensions-dir', extensions, '--disable-workspace-trust', '--skip-welcome', '--skip-release-notes', '--disable-gpu', '--disable-extension', 'github.copilot', '--disable-extension', 'github.copilot-chat', '--log', 'info'],
    extensionTestsEnv: { SCREENPLAY_REPAIR_SERVER: server, SCREENPLAY_REPAIR_HOST_ROOT: model, SCREENPLAY_REPAIR_INSTALLED_EXTENSIONS: vsix ? extensions : '' },
}); } finally {
    const logs = path.join(userData, 'logs');
    if (fs.existsSync(logs)) {
        fs.cpSync(logs, path.join(evidence, 'logs'), { recursive: true });
        const shutdownLogs = [];
        const inspectLogs = directory => {
            for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
                const file = path.join(directory, entry.name);
                if (entry.isDirectory()) inspectLogs(file);
                else if (entry.name === 'exthost.log') shutdownLogs.push(fs.readFileSync(file, 'utf8'));
            }
        };
        inspectLogs(logs);
        if (shutdownLogs.some(log => log.includes('illegal state - object is disposed'))) {
            shutdownFailure = `Native extension teardown touched disposed resources; inspect ${evidence}`;
            console.error(shutdownFailure);
        } else console.log(`NATIVE TEARDOWN LOG CHECK: no disposed-resource exception in ${shutdownLogs.length} extension-host logs; ${evidence}`);
    }
    // Keep synthetic paths for diagnosis; no blanket cleanup of unregistered outputs.
}
if (shutdownFailure) throw new Error(shutdownFailure);

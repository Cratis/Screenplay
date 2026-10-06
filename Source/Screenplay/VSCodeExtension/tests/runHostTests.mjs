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
// Independent installed-client lifetimes, never a root switch to escape an
// uncertain authority. Each case that ends in a retained unknown barrier (dirty,
// clean, and the three missed-notification cases) is classified/disposed before
// the next separately approved fixture/lifetime starts.
async function runHost(caseName) {
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
console.log(`Native ${caseName} synthetic workspace: ${model}; native host data: ${testRoot}; retained evidence: ${evidence}; independently approved NEW installed client lifetime`);
const userData = path.join(testRoot, 'u');
fs.mkdirSync(path.join(userData, 'User'), { recursive: true });
fs.writeFileSync(path.join(userData, 'User/settings.json'), JSON.stringify({
    'screenplay.repairs.enabled': true, 'screenplay.repairs.executable': server, 'screenplay.repairs.arguments': ['mcp'], 'screenplay.repairs.modelRoot': path.join(model, caseName === 'clean' ? 'clean-unknown' : caseName.startsWith('missed-') ? caseName : 'direct'),
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
    // CI packages this workspace manifest into the VSIX. An install can finish
    // before the CLI crashes, so verify the exact version in the same host dirs.
    const manifest = JSON.parse(fs.readFileSync(new URL('../package.json', import.meta.url), 'utf8'));
    const expected = `${manifest.publisher}.${manifest.name}@${manifest.version}`;
    let listed;
    for (let attempt = 0; attempt < 3; attempt++) {
        listed = spawnSync(cli, [...args, '--list-extensions', '--show-versions', '--extensions-dir', extensions, '--user-data-dir', userData], { encoding: 'utf8', timeout: 60_000, shell: process.platform === 'win32' });
        const output = `${listed.stdout ?? ''}\n${listed.stderr ?? ''}`;
        if (listed.status === 0 || !output.includes('FATAL ERROR: v8::ToLocalChecked Empty MaybeLocal') || attempt === 2) break;
        console.warn(`Extension listing crashed with exit ${listed.status}; retrying verification (${attempt + 1}/2).`);
    }
    const listing = `${listed.stdout ?? ''}\n${listed.stderr ?? ''}`;
    if (installed.error || listed.error || listed.status !== 0 || !(listed.stdout ?? '').split(/\r?\n/).some(line => line.trim() === expected)) {
        throw new Error(`VSIX installation failed: ${installed.status}; expected ${expected}; verification exit: ${listed.status}; install error: ${installed.error?.message ?? 'none'}; verification error: ${listed.error?.message ?? 'none'}; extension listing:\n${listing}`);
    }
    if (installed.status !== 0) console.warn(`VSIX installation CLI exited ${installed.status}, but verified ${expected} is installed.`);
    // Only a tiny test driver is developed. cratis.screenplay MUST resolve from
    // the installed VSIX, not a development-path override of production sources.
    development = path.join(testRoot, 'driver');
    fs.mkdirSync(development);
    fs.writeFileSync(path.join(development, 'package.json'), JSON.stringify({ name: 'screenplay-repair-test-driver', publisher: 'cratis-tests', version: '0.0.0', engines: { vscode: '^1.85.0' } }));
}
let shutdownFailure;
let suitesPassed = false;
const teardownEvidence = path.join(evidence, 'pending-inspection-teardown.json');
const { nativeObservationRoot } = createRequire(import.meta.url)('../out/tests/nativeObservationRoot.cjs');
try { await runTests({
    vscodeExecutablePath: executable,
    extensionDevelopmentPath: development, extensionTestsPath: path.resolve('out/tests/extensionHost.cjs'),
    launchArgs: [model, '--user-data-dir', userData, '--extensions-dir', extensions, '--disable-workspace-trust', '--skip-welcome', '--skip-release-notes', '--disable-gpu', '--disable-extension', 'github.copilot', '--disable-extension', 'github.copilot-chat', '--log', 'trace'],
    extensionTestsEnv: { SCREENPLAY_REPAIR_SERVER: server, SCREENPLAY_REPAIR_HOST_ROOT: model, SCREENPLAY_REPAIR_INSTALLED_EXTENSIONS: vsix ? extensions : '', SCREENPLAY_REPAIR_OBSERVE_SYNTHETIC_ROOT: nativeObservationRoot(model, caseName, process.env.SCREENPLAY_REPAIR_OBSERVE === '1'), SCREENPLAY_REPAIR_TEARDOWN_EVIDENCE: teardownEvidence, SCREENPLAY_REPAIR_NATIVE_LOGS: path.join(userData, 'logs'), SCREENPLAY_REPAIR_HOST_CASE: caseName },
}); suitesPassed = true; } finally {
    const logs = path.join(userData, 'logs');
    if (!fs.existsSync(logs)) {
        shutdownFailure = `Missing native logs; disposed-resource teardown cannot be verified: ${evidence}`;
    } else {
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
        if (!shutdownLogs.length) {
            shutdownFailure = `Missing native extension-host logs; disposed-resource teardown cannot be verified: ${evidence}`;
        } else if (shutdownLogs.some(log => log.includes('illegal state - object is disposed'))) {
            shutdownFailure = `Native extension teardown touched disposed resources; inspect ${evidence}`;
            console.error(shutdownFailure);
        } else console.log(`NATIVE TEARDOWN LOG CHECK: no disposed-resource exception in ${shutdownLogs.length} extension-host logs; ${evidence}`);
    }
    // A normal test exit alone cannot prove pending inspection completed after
    // actual installed disposal. Require test-owned evidence of the REAL reply's
    // late completion and zero installed UI calls; unsupported Windows roots skip.
    if (suitesPassed && fs.existsSync(teardownEvidence)) {
        const teardown = JSON.parse(fs.readFileSync(teardownEvidence, 'utf8'));
        if (teardown.pending || teardown.error || teardown.timedOut !== false || teardown.actualStateQueryDispatched !== true || teardown.genuineReadResponseHeld !== true || teardown.actualRootWatchClosed !== true || teardown.pendingAtClose !== true || teardown.actualInspectionSettled !== true || teardown.exactBuffersAndDiskPreserved !== true || teardown.lateUi?.length !== 0 || teardown.applyFrames !== (caseName === 'dirty' ? 2 : 1)) {
            shutdownFailure = `Pending native inspection teardown is incomplete or unsafe: ${JSON.stringify(teardown)}; inspect ${evidence}`;
        } else console.log(`NATIVE PENDING INSPECTION TEARDOWN VERIFIED: ${JSON.stringify(teardown)}`);
    } else if (suitesPassed && !(process.platform === 'win32' && fs.statSync(path.join(model, 'command-guards'), { bigint: true }).dev === 0n)) {
        shutdownFailure = `Missing pending native inspection teardown evidence; inspect ${evidence}`;
    }
    // Keep synthetic paths for diagnosis; no blanket cleanup of unregistered outputs.
}
if (shutdownFailure) throw new Error(shutdownFailure);
}

await runHost('dirty');
await runHost('clean');
for (const name of ['source', 'state', 'attachment']) await runHost(`missed-${name}`);

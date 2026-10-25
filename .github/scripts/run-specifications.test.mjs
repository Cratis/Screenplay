// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const root = fileURLToPath(new URL('../../', import.meta.url));
const launcher = path.join(root, '.github/scripts/run-specifications.sh');
const dotnet = spawnSync('which', ['dotnet'], { encoding: 'utf8' });
assert.equal(dotnet.status, 0, dotnet.stderr);
const scratch = path.join(root, '.ai-work');
mkdirSync(scratch, { recursive: true });

function trx({ total = 1, passed = 1, executed = passed, failed = 0, outcome = 'Completed', rowOutcome = 'Passed' } = {}) {
    return `<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results><UnitTestResult testId="one" outcome="${rowOutcome}" /></Results><ResultSummary outcome="${outcome}"><Counters total="${total}" executed="${executed}" passed="${passed}" failed="${failed}" notExecuted="0" /></ResultSummary></TestRun>`;
}

function fixture({ xml = trx(), mode = 'write', exitCode = 0, assembly = true, configuration = 'Debug', projectName = 'Fake', assemblyName = `Cratis.${projectName}` } = {}) {
    const directory = mkdtempSync(path.join(scratch, 'spec-runner-test.'));
    const projectDirectory = path.join(directory, 'project with spaces');
    const binaryDirectory = path.join(projectDirectory, 'bin', configuration, 'net10.0');
    mkdirSync(binaryDirectory, { recursive: true });
    if (assembly) writeFileSync(path.join(binaryDirectory, `${assemblyName}.dll`), 'fake native executable');
    const project = path.join(projectDirectory, `${projectName}.csproj`);
    writeFileSync(project, '<Project />');
    writeFileSync(path.join(directory, 'fixture.trx'), xml);
    const shim = path.join(directory, 'dotnet');
    writeFileSync(shim, `#!${process.execPath}
import { copyFileSync, writeFileSync, utimesSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
const args = process.argv.slice(2);
if (!args[0].endsWith('.dll')) {
    const result = spawnSync(process.env.REAL_DOTNET, args, { stdio: 'inherit' });
    process.exit(result.status ?? 99);
}
writeFileSync(process.env.ARGUMENT_LOG, JSON.stringify(args));
const output = args[args.indexOf('-result-trx') + 1];
if (process.env.MODE !== 'missing') {
    copyFileSync(process.env.FIXTURE, output);
    if (process.env.MODE === 'stale') utimesSync(output, new Date(0), new Date(0));
}
process.exit(Number(process.env.NATIVE_EXIT));
`, { mode: 0o755 });
    return { directory, project, configuration, environment: {
        ...process.env, PATH: `${directory}${path.delimiter}${process.env.PATH}`,
        REAL_DOTNET: dotnet.stdout.trim(), ARGUMENT_LOG: path.join(directory, 'arguments.json'),
        FIXTURE: path.join(directory, 'fixture.trx'), MODE: mode, NATIVE_EXIT: String(exitCode),
        SPEC_RESULTS_ROOT: path.join(directory, 'results')
    } };
}

function run(input, ...args) {
    const result = spawnSync('bash', [launcher, input.project, input.configuration, ...args], {
        env: input.environment, encoding: 'utf8', timeout: 120000
    });
    assert.equal(result.error, undefined);
    return result;
}

test('accepts a fresh positive native TRX and forwards quoted filters with the correct writer option', () => {
    const input = fixture();
    const result = run(input, '-filterVSTest', 'FullyQualifiedName~A&FullyQualifiedName!~B');
    assert.equal(result.status, 0, result.stdout + result.stderr);
    const args = JSON.parse(readFileSync(input.environment.ARGUMENT_LOG, 'utf8'));
    assert.equal(args[1], '-result-trx');
    assert.ok(args[2].startsWith(input.environment.SPEC_RESULTS_ROOT));
    assert.deepEqual(args.slice(3), ['-filterVSTest', 'FullyQualifiedName~A&FullyQualifiedName!~B']);
    assert.ok(!args.includes('-failEmpty') && !args.includes('-trx'));
});

test('supports the dedicated Release executable', () => {
    const input = fixture({ configuration: 'Release' });
    const result = run(input);
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.match(JSON.parse(readFileSync(input.environment.ARGUMENT_LOG, 'utf8'))[0], /bin\/Release\/net10\.0/);
});

test('uses the dedicated catalog specifications assembly name', () => {
    const input = fixture({ projectName: 'Screenplay.ContractCatalog.Specs' });
    const result = run(input);
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.ok(JSON.parse(readFileSync(input.environment.ARGUMENT_LOG, 'utf8'))[0].endsWith('/Cratis.Screenplay.ContractCatalog.Specs.dll'));
});

test('prepends the Tool Debug sentinel and forwards filters without changing TRX arguments', () => {
    const input = fixture({ projectName: 'Tool', assemblyName: 'Cratis.Screenplay.Tool' });
    const result = run(input, '-filterVSTest', 'FullyQualifiedName=Cratis.Screenplay.Tool.for_CommandLineInformation.when_describing_the_installed_tool');
    assert.equal(result.status, 0, result.stdout + result.stderr);
    const args = JSON.parse(readFileSync(input.environment.ARGUMENT_LOG, 'utf8'));
    assert.ok(args[0].endsWith('/Cratis.Screenplay.Tool.dll'));
    assert.equal(args[1], '--specifications');
    assert.equal(args[2], '-result-trx');
    assert.ok(args[3].startsWith(input.environment.SPEC_RESULTS_ROOT));
    assert.deepEqual(args.slice(4), ['-filterVSTest', 'FullyQualifiedName=Cratis.Screenplay.Tool.for_CommandLineInformation.when_describing_the_installed_tool']);
});

test('refuses Tool Release before invoking the ordinary CLI', () => {
    const input = fixture({ projectName: 'Tool', configuration: 'Release', assemblyName: 'Cratis.Screenplay.Tool' });
    const result = run(input);
    assert.equal(result.status, 2, result.stdout + result.stderr);
    assert.match(result.stderr, /Tool specifications require Debug/);
    assert.equal(existsSync(input.environment.ARGUMENT_LOG), false);
});

test('preserves the Tool Debug native nonzero exit', () => {
    const result = run(fixture({ projectName: 'Tool', exitCode: 23, assemblyName: 'Cratis.Screenplay.Tool' }));
    assert.equal(result.status, 23, result.stdout + result.stderr);
});

test('preserves native nonzero exit even when a positive TRX exists', () => {
    const result = run(fixture({ exitCode: 23 }));
    assert.equal(result.status, 23, result.stdout + result.stderr);
});

test('refuses a missing executable without building it', () => {
    const result = run(fixture({ assembly: false }));
    assert.equal(result.status, 2, result.stdout + result.stderr);
});

for (const [name, options] of [
    ['missing TRX', { mode: 'missing' }],
    ['stale TRX', { mode: 'stale' }],
    ['malformed XML', { xml: '<TestRun' }],
    ['non-TRX XML', { xml: '<NotATestRun />' }],
    ['zero specifications', { xml: '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results/><ResultSummary outcome="Completed"><Counters total="0" executed="0" passed="0" failed="0"/></ResultSummary></TestRun>' }],
    ['missing counters', { xml: '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results/><ResultSummary outcome="Completed"/></TestRun>' }],
    ['invalid counter', { xml: trx({ total: 'NaN' }) }],
    ['inconsistent counter', { xml: trx({ total: 2 }) }],
    ['failed specification', { xml: trx({ passed: 0, executed: 1, failed: 1, rowOutcome: 'Failed' }) }],
    ['failed counter despite a passing row', { xml: trx({ failed: 1 }) }],
    ['missing result identity', { xml: trx().replace('testId="one"', '') }],
    ['duplicate result identities', { xml: trx({ total: 2, passed: 2 }).replace('</Results>', '<UnitTestResult testId="one" outcome="Passed" /></Results>') }],
    ['failed summary', { xml: trx({ outcome: 'Failed' }) }],
    ['nonexecuted-only run', { xml: trx({ passed: 0, executed: 0, rowOutcome: 'NotExecuted' }) }],
    ['unexpected row outcome', { xml: trx({ rowOutcome: 'Error' }) }]
]) {
    test(`refuses native exit zero with ${name}`, () => {
        const result = run(fixture(options));
        assert.notEqual(result.status, 0, result.stdout + result.stderr);
    });
}

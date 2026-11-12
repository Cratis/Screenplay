// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { existsSync, mkdirSync, mkdtempSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import test from 'node:test';
import { expand } from './expand-spec-shards.mjs';

const root = fileURLToPath(new URL('../../', import.meta.url));
const launcher = path.join(root, '.github/scripts/run-specifications.sh');
const dotnet = spawnSync('which', ['dotnet'], { encoding: 'utf8' });
assert.equal(dotnet.status, 0, dotnet.stderr);
const scratch = path.join(root, '.ai-work');
mkdirSync(scratch, { recursive: true });

function trx({ total = 1, passed = 1, executed = passed, failed = 0, outcome = 'Completed', rowOutcome = 'Passed' } = {}) {
    return `<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results><UnitTestResult testId="one" outcome="${rowOutcome}" /></Results><ResultSummary outcome="${outcome}"><Counters total="${total}" executed="${executed}" passed="${passed}" failed="${failed}" notExecuted="0" /></ResultSummary></TestRun>`;
}

function fixture({ xml = trx(), mode = 'write', exitCode = 0, assembly = true, configuration = 'Debug', framework = 'net11.0', projectName = 'Fake', assemblyName = `Cratis.${projectName}` } = {}) {
    const directory = mkdtempSync(path.join(scratch, 'spec-runner-test.'));
    const projectDirectory = path.join(directory, 'project with spaces');
    const binaryDirectory = path.join(projectDirectory, 'bin', configuration, framework);
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
        SPEC_RESULTS_ROOT: path.join(directory, 'results'), SPEC_TARGET_FRAMEWORK: ''
    } };
}

function run(input, ...args) {
    const result = spawnSync('bash', [launcher, input.project, input.configuration, ...args], {
        env: input.environment, encoding: 'utf8', timeout: 120000
    });
    assert.equal(result.error, undefined);
    return result;
}

test('expands a legacy split into its original two complementary jobs', () => {
    assert.deepEqual(expand({ include: [{ name: 'compiler', projects: ['compiler.csproj'], split: 'Semantics*' }] }), {
        include: [
            { name: 'compiler, part 1', projects: ['compiler.csproj'], args: ['-class', 'Semantics*'] },
            { name: 'compiler, part 2', projects: ['compiler.csproj'], args: ['-class-', 'Semantics*'] }
        ]
    });
});

test('excludes earlier patterns from later groups and from the final remainder', () => {
    const jobs = expand({ include: [{ name: 'compiler', projects: ['compiler.csproj'], split: ['A*', 'A.B*', 'C*'] }] }).include;
    assert.deepEqual(jobs.map(job => job.args), [
        ['-class', 'A*'],
        ['-class', 'A.B*', '-class-', 'A*'],
        ['-class', 'C*', '-class-', 'A*', '-class-', 'A.B*'],
        ['-class-', 'A*', '-class-', 'A.B*', '-class-', 'C*']
    ]);
    for (const name of ['A.One', 'A.B.Two', 'C.Three', 'Future.NewNamespace']) {
        assert.equal(jobs.filter(job => selectsClass(job.args, name)).length, 1, name);
    }
});

test('keeps unsplit jobs unfiltered and refuses empty or malformed splits', () => {
    const shard = { name: 'all', projects: ['all.csproj'] };
    assert.deepEqual(expand({ include: [shard] }).include, [{ ...shard, args: [] }]);
    assert.deepEqual(expand({ include: [{ ...shard, split: null }] }).include, [{ ...shard, args: [] }]);
    assert.throws(() => expand({ include: [] }), /At least one/);
    for (const split of [[], '', ' ', [null], 17]) {
        assert.throws(() => expand({ include: [{ ...shard, split }] }), /nonempty class patterns/);
    }
});

function selectsClass(args, name) {
    for (let index = 0; index < args.length; index += 2) {
        const pattern = new RegExp(`^${args[index + 1].split('*').map(part => part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('.*')}$`, 'i');
        if (pattern.test(name) !== (args[index] === '-class')) return false;
    }
    return true;
}

function sourceClasses(directory) {
    return readdirSync(directory, { recursive: true, withFileTypes: true })
        .filter(entry => entry.isFile() && entry.name.endsWith('.cs') && !entry.parentPath.split(path.sep).some(part => part === 'bin' || part === 'obj'))
        .flatMap(entry => {
            const source = readFileSync(path.join(entry.parentPath, entry.name), 'utf8');
            const namespace = source.match(/^namespace ([\w.]+);/m)?.[1];
            return Array.from(source.matchAll(/\bclass\s+((?:when_|and_)[\w]+)/g), match => `${namespace}.${match[1]}`);
        });
}

const workflow = readFileSync(path.join(root, '.github/workflows/dotnet-build.yml'), 'utf8');
const configuredShards = JSON.parse(workflow.match(/SPEC_SHARDS: >-\n([\s\S]*?)\n\njobs:/)[1]);

for (const [name, sourceDirectory] of [['compiler', 'Screenplay'], ['mcp', 'Screenplay.Mcp']]) {
    test(`configured ${name} splits select every source specification class exactly once on both runtimes`, () => {
        const shard = configuredShards.include.find(shard => shard.name === name);
        const classes = sourceClasses(path.join(root, 'Source/DotNET', sourceDirectory));
        assert.ok(classes.length > 100, `Expected a populated ${name} specification inventory`);
        for (const split of [shard.split, shard['compatibility-split']]) {
            const jobs = expand({ include: [{ ...shard, split }] }).include;
            for (const className of classes) {
                assert.equal(jobs.filter(job => selectsClass(job.args, className)).length, 1, className);
            }
            for (const job of jobs) {
                assert.ok(classes.some(name => selectsClass(job.args, name)), `${job.name} selects no specifications`);
            }
        }
    });
}

test('the expansion CLI emits the same matrix used by the coverage guard', () => {
    const result = spawnSync(process.execPath, [path.join(root, '.github/scripts/expand-spec-shards.mjs'), JSON.stringify(configuredShards)], { encoding: 'utf8' });
    assert.equal(result.status, 0, result.stderr);
    assert.deepEqual(JSON.parse(result.stdout), expand(configuredShards));
});

test('package validation builds its own board and retains every Release validation step', () => {
    const job = workflow.match(/\n  package-validation:\n([\s\S]*?)\n  # Preserve the original check name/)[1];
    assert.doesNotMatch(job, /needs:|download-artifact|tar -xzf/);
    for (const step of ['Checkout code', 'Setup .Net', 'Cache NuGet packages', 'Setup node', 'Yarn install', 'Build event model board', 'Build Release', 'Run canonical vector specifications on net11.0', 'Pack', 'Verify context package dependencies']) {
        assert.ok(job.includes(`- name: ${step}\n`), step);
    }
    assert.ok(job.indexOf('- name: Build event model board') < job.indexOf('- name: Build Release'));
});

test('forwards ordered split filters to the native runner without changing them', () => {
    const input = fixture();
    const args = expand({ include: [{ name: 'compiler', projects: [], split: ['A*', 'B*'] }] }).include[1].args;
    const result = run(input, ...args);
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.deepEqual(JSON.parse(readFileSync(input.environment.ARGUMENT_LOG, 'utf8')).slice(3), args);
});

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
    assert.match(JSON.parse(readFileSync(input.environment.ARGUMENT_LOG, 'utf8'))[0], /bin\/Release\/net11\.0/);
});

test('defaults to net11.0 for Debug specifications', () => {
    const input = fixture();
    const result = run(input);
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.match(JSON.parse(readFileSync(input.environment.ARGUMENT_LOG, 'utf8'))[0], /bin\/Debug\/net11\.0/);
});

test('supports net10.0 library compatibility specifications', () => {
    const input = fixture({ framework: 'net10.0' });
    input.environment.SPEC_TARGET_FRAMEWORK = 'net10.0';
    const result = run(input, '-filterVSTest', 'FullyQualifiedName~Compatibility');
    assert.equal(result.status, 0, result.stdout + result.stderr);
    const args = JSON.parse(readFileSync(input.environment.ARGUMENT_LOG, 'utf8'));
    assert.match(args[0], /bin\/Debug\/net10\.0/);
    assert.deepEqual(args.slice(3), ['-filterVSTest', 'FullyQualifiedName~Compatibility']);
});

test('refuses unsupported specification frameworks without invoking dotnet', () => {
    const input = fixture();
    input.environment.SPEC_TARGET_FRAMEWORK = 'net9.0';
    const result = run(input);
    assert.equal(result.status, 2, result.stdout + result.stderr);
    assert.match(result.stderr, /Unsupported specification framework/);
    assert.equal(existsSync(input.environment.ARGUMENT_LOG), false);
});

test('does not fall back to net11.0 when the net10.0 executable is missing', () => {
    const input = fixture();
    input.environment.SPEC_TARGET_FRAMEWORK = 'net10.0';
    const result = run(input);
    assert.equal(result.status, 2, result.stdout + result.stderr);
    assert.match(result.stderr, /Build the Debug net10\.0 specification executable first/);
    assert.equal(existsSync(input.environment.ARGUMENT_LOG), false);
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

test('net10.0 compatibility shards run a shared build without rebuilding', () => {
    const build = workflow.match(/\n  dotnet-build-net10:\n([\s\S]*?)\n  compatibility-specs:/)[1];
    assert.match(build, /-p:SpecTargetFramework=net10\.0/);
    assert.match(build, /upload-artifact/);
    const shards = workflow.match(/\n  compatibility-specs:\n([\s\S]*?)\n  package-validation:/)[1];
    assert.match(shards, /needs: \[dotnet-build-net10, shards\]/);
    assert.match(shards, /download-artifact/);
    assert.doesNotMatch(shards, /dotnet build|yarn/);
});

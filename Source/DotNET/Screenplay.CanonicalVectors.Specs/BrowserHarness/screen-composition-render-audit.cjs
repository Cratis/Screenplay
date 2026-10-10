// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Audits what the installed CLI renders for the screens vector, without a browser:
//   determinism   two renders of the same source publish byte-identical artifacts (Screenplay#173.6)
//   artifacts     no timestamps, GUIDs that are not in the source, absolute paths or environment bytes (Screenplay#173.12)
//   profile       a changed profile input is refused with nothing published, and a stale managed artifact is
//                 refused without --force and left untouched (Screenplay#173.9)
//   recover       the rendered application recovers through `cratis screenplay generate` to the same commands,
//                 events, read models and queries the source declares (Screenplay#168.11)
//
// node screen-composition-render-audit.cjs              run against the installed `cratis`
// node screen-composition-render-audit.cjs --self-test  check the scanners against planted defects, no CLI needed
// Exit 0 when every check passes, 1 when a check ran and failed.

const { execFileSync, spawnSync } = require('node:child_process');
const { cpSync, existsSync, mkdtempSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } = require('node:fs');
const { hostname, tmpdir, userInfo } = require('node:os');
const { join, relative } = require('node:path');

const root = process.cwd();
const corpus = join(root, 'Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ScreenComposition/v1');
const source = join(corpus, 'source/folder');
const resultPath = process.env.SCREENPLAY_RENDER_AUDIT_RESULT ?? join(root, '.ai-work/render-audit-result.json');

const guidPattern = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;
const timestampPattern = /\b(?:19|20)\d\d-[01]\d-[0-3]\d(?:[T ][0-2]\d:[0-5]\d)?/g;

function filesUnder(folder) {
    if (!existsSync(folder)) return [];
    return readdirSync(folder, { recursive: true })
        .map(entry => join(folder, entry.toString()))
        .filter(path => statSync(path).isFile())
        .sort();
}

function guidsIn(text) {
    return new Set((text.match(guidPattern) ?? []).map(guid => guid.toLowerCase()));
}

// Every byte pattern that would make a published artifact depend on when, where or by whom it was rendered.
function environmentMarkers() {
    const markers = [['home folder', userInfo().homedir], ['working folder', root], ['temporary folder', tmpdir()], ['host name', hostname()], ['user name', userInfo().username]];
    return [
        ...markers.filter(([, value]) => value && value.length >= 4),
        ['absolute macOS path', '/Users/'],
        ['absolute Linux home path', '/home/'],
        ['absolute temporary path', '/var/folders/'],
        ['absolute Windows path', 'C:\\']
    ];
}

// Returns one finding per offending artifact and reason; an empty list means the artifacts are portable.
function auditArtifacts(files, sourceGuids, markers) {
    const findings = [];
    for (const { path, text } of files) {
        for (const timestamp of new Set(text.match(timestampPattern) ?? [])) findings.push(`${path}: timestamp ${timestamp}`);
        for (const guid of guidsIn(text)) if (!sourceGuids.has(guid)) findings.push(`${path}: GUID ${guid} is not in the source`);
        for (const [name, value] of markers) if (text.includes(value)) findings.push(`${path}: ${name}`);
    }
    return findings;
}

// The portable backend contract of a model: slices, and the members of every command, event, read model and query.
function declarationsIn(text) {
    const declarations = new Set();
    let owner = null;
    let ownerIndent = -1;
    for (const line of text.split('\n')) {
        const indent = line.length - line.trimStart().length;
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith('//')) continue;
        if (owner && indent <= ownerIndent) owner = null;

        const declaration = /^(slice\s+\w+|command|event|readmodel|query)\s+(\w+)/.exec(trimmed);
        if (declaration) {
            const kind = declaration[1].split(/\s+/)[0];
            declarations.add(`${kind} ${declaration[2]}`);
            owner = kind === 'slice' ? null : `${kind} ${declaration[2]}`;
            ownerIndent = indent;
            continue;
        }

        // A member is `name Type`, where the type token ends the word. A recovered model also carries source references
        // such as `file Workspaces/Tracking/X.cs`, which are provenance, not members.
        const member = /^(\w+)\s+([A-Z][\w[\]?]*)(?:\s|$)/.exec(trimmed);
        if (owner && indent === ownerIndent + 2 && member && !['produces', 'for', 'by', 'from', 'when', 'then', 'given', 'file'].includes(member[1])) {
            declarations.add(`${owner}.${member[1]}`);
        }
    }
    return declarations;
}

function compareDeclarations(expected, actual) {
    return {
        missing: [...expected].filter(declaration => !actual.has(declaration)).sort(),
        unexpected: [...actual].filter(declaration => !expected.has(declaration)).sort()
    };
}

function run(command, args, options = {}) {
    const result = spawnSync(command, args, { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024, ...options });
    return { status: result.status, output: `${result.stdout ?? ''}${result.stderr ?? ''}` };
}

function render(from, destination) {
    return run('cratis', ['render', from, '--target', 'cratis', '--destination', destination, '--name', 'Workspaces', '-y', '-o', 'json-compact']);
}

function diagnosticCodes(output) {
    return [...new Set([...output.matchAll(/"code":"([A-Z][A-Z-]*\d+)"/g)].map(match => match[1]))];
}

function record(result, path, status, value = undefined) {
    result.assertions.push({ path, status, value });
    if (status === 'failed' || status === 'blocked') result.remainingBlockers.push(`${path}: ${value}`);
}

function readArtifacts(folder) {
    return filesUnder(folder).map(path => ({ path: relative(folder, path), text: readFileSync(path, 'latin1') }));
}

function checkDeterminismAndArtifacts(result, work) {
    const first = render(source, join(work, 'first'));
    const second = render(source, join(work, 'second'));
    if (first.status !== 0 || second.status !== 0) {
        record(result, 'render.succeeds', 'failed', `exit ${first.status} and ${second.status}: ${diagnosticCodes(first.output).join(',')}`);
        return null;
    }

    const firstArtifacts = readArtifacts(join(work, 'first'));
    const secondArtifacts = readArtifacts(join(work, 'second'));
    record(result, 'render.artifactCount', 'info', String(firstArtifacts.length));

    const differing = firstArtifacts.filter((artifact, index) => artifact.path !== secondArtifacts[index]?.path || artifact.text !== secondArtifacts[index]?.text).map(artifact => artifact.path);
    if (firstArtifacts.length > 0 && firstArtifacts.length === secondArtifacts.length && differing.length === 0) record(result, 'render.deterministic', 'passed', `${firstArtifacts.length} artifacts byte-identical across two renders`);
    else record(result, 'render.deterministic', 'failed', `differing: ${differing.join(', ') || 'artifact sets differ'}`);

    const sourceGuids = new Set(filesUnder(corpus).flatMap(path => [...guidsIn(readFileSync(path, 'utf8'))]));
    const findings = auditArtifacts(firstArtifacts, sourceGuids, environmentMarkers());
    if (findings.length === 0) record(result, 'render.portableArtifacts', 'passed', `${firstArtifacts.length} artifacts scanned`);
    else record(result, 'render.portableArtifacts', 'failed', findings.slice(0, 20).join('; '));

    return join(work, 'first');
}

// A profile edit is a changed input; the render must refuse it rather than publish a fallback.
function checkChangedProfile(result, work, path, description, edit) {
    const copy = join(work, path);
    cpSync(source, copy, { recursive: true });
    const application = join(copy, 'application.play');
    const original = readFileSync(application, 'utf8');
    const changed = edit(original);
    if (changed === original) {
        record(result, `profile.${path}`, 'failed', `the edit did not change application.play: ${description}`);
        return;
    }

    writeFileSync(application, changed);
    const destination = join(work, `${path}-out`);
    const rendered = render(copy, destination);
    const published = filesUnder(destination).length;
    if (rendered.status !== 0 && published === 0) record(result, `profile.${path}`, 'passed', `refused with exit ${rendered.status}: ${diagnosticCodes(rendered.output).join(',')}`);
    else record(result, `profile.${path}`, 'failed', `${description}: exit ${rendered.status}, ${published} artifacts published, diagnostics ${diagnosticCodes(rendered.output).join(',')}`);
}

function checkStaleManagedArtifact(result, work) {
    const destination = join(work, 'stale');
    if (render(source, destination).status !== 0) {
        record(result, 'profile.staleManagedArtifact', 'failed', 'the first render failed');
        return;
    }

    const scene = join(destination, 'scene.json');
    writeFileSync(scene, `${readFileSync(scene, 'utf8')}\n`);
    const modified = readFileSync(scene, 'utf8');
    const rerendered = render(source, destination);
    const kept = readFileSync(scene, 'utf8') === modified;
    if (rerendered.status !== 0 && kept) record(result, 'profile.staleManagedArtifact', 'passed', `refused with exit ${rerendered.status} and left scene.json untouched`);
    else record(result, 'profile.staleManagedArtifact', 'failed', `exit ${rerendered.status}; scene.json ${kept ? 'kept' : 'replaced'}`);
}

function checkRenderThenRecover(result, work, rendered) {
    const project = join(work, 'recover');
    cpSync(rendered, project, { recursive: true });
    run('git', ['init', '-q'], { cwd: project });
    run('git', ['add', '-A'], { cwd: project });
    run('git', ['-c', 'user.email=audit@cratis.io', '-c', 'user.name=audit', 'commit', '-qm', 'render'], { cwd: project });

    const solution = readdirSync(project).find(name => name.endsWith('.slnx'));
    const restore = run('dotnet', ['restore', solution], { cwd: project });
    if (restore.status !== 0) {
        record(result, 'recover.restore', 'failed', `dotnet restore exited ${restore.status}`);
        return;
    }

    const recoveredPath = join(work, 'recovered.play');
    const generated = run('cratis', ['screenplay', 'generate', solution, '--file', recoveredPath, '-y', '-o', 'json-compact'], { cwd: project });
    if (generated.status !== 0 || !existsSync(recoveredPath)) {
        const message = /"message":"([^"]+)"/.exec(generated.output)?.[1] ?? generated.output.slice(0, 300);
        record(result, 'recover.generate', 'blocked', `exit ${generated.status} ${diagnosticCodes(generated.output).join(',')}: ${message}`);
        return;
    }

    const expected = declarationsIn(filesUnder(source).filter(path => path.endsWith('.play')).map(path => readFileSync(path, 'utf8')).join('\n'));
    const { missing, unexpected } = compareDeclarations(expected, declarationsIn(readFileSync(recoveredPath, 'utf8')));
    if (missing.length === 0 && unexpected.length === 0) record(result, 'recover.contracts', 'passed', `${expected.size} declarations recovered`);
    else record(result, 'recover.contracts', 'failed', `missing ${missing.join(', ') || 'none'}; unexpected ${unexpected.join(', ') || 'none'}`);
}

function audit() {
    const result = { name: 'screen-composition-render-audit', cliVersion: execFileSync('cratis', ['--version'], { encoding: 'utf8' }).trim(), status: 'unknown', remainingBlockers: [], assertions: [] };
    const work = mkdtempSync(join(tmpdir(), 'screen-render-audit-'));
    try {
        const rendered = checkDeterminismAndArtifacts(result, work);
        checkChangedProfile(result, work, 'unknownPackage', 'a profile naming an unknown package', text => text.replace(/^(\s+)Cratis\.Components$/m, '$1Cratis.NotAPackage'));
        checkChangedProfile(result, work, 'missingLayout', 'a profile naming a layout that does not exist', text => text.replace(/^(\s+)layout AppShell$/m, '$1layout MissingShell'));
        checkStaleManagedArtifact(result, work);
        if (rendered) checkRenderThenRecover(result, work, rendered);
    } finally {
        rmSync(work, { recursive: true, force: true });
    }

    result.status = result.remainingBlockers.length > 0 ? 'failed' : 'passed';
    writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
    console.log(`render audit on CLI ${result.cliVersion}: ${result.status}`);
    for (const assertion of result.assertions) console.log(`  ${assertion.status.padEnd(7)} ${assertion.path}: ${assertion.value}`);
    process.exit(result.status === 'passed' ? 0 : 1);
}

// Each scanner must find what was planted and stay silent on a clean artifact.
function selfTest() {
    const sourceGuid = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
    const markers = [['home folder', '/Users/someone'], ['host name', 'build-agent-7']];
    const clean = { path: 'scene.json', text: `{"id":"Workspaces.Tracking.contribution[0]","workItemId":"${sourceGuid}","version":"4.50.0"}` };
    const findingsFor = text => auditArtifacts([{ path: 'artifact', text }], new Set([sourceGuid]), markers);
    const sourceModel = 'module Workspaces\n  feature Tracking\n    slice StateChange AddComment\n      command AddComment\n        commentId CommentId identifier\n        text String\n        produces CommentAdded\n          for commentId\n      event CommentAdded\n        text String\n';
    const checks = [
        ['a clean artifact has no findings', auditArtifacts([clean], new Set([sourceGuid]), markers).length, 0],
        ['a timestamp is found', findingsFor('{"generatedAt":"2026-10-09T21:42:00Z"}').length, 1],
        ['a GUID not in the source is found', findingsFor('{"id":"0b5574f3-b45e-4c9a-9f1a-2c0d8e7a6b5c"}').length, 1],
        ['a GUID from the source is allowed', findingsFor(`{"id":"${sourceGuid.toUpperCase()}"}`).length, 0],
        ['a home folder path is found', findingsFor('"path":"/Users/someone/app"').length, 1],
        ['a host name is found', findingsFor('built on build-agent-7').length, 1],
        ['a version number is not a timestamp', findingsFor('"version":"2026.10.9"').length, 0],
        ['command members are declarations', [...declarationsIn(sourceModel)].sort(), ['command AddComment', 'command AddComment.commentId', 'command AddComment.text', 'event CommentAdded', 'event CommentAdded.text', 'slice AddComment']],
        ['produces clauses are not members', declarationsIn(sourceModel).has('command AddComment.produces'), false],
        ['a dropped event member is reported missing', compareDeclarations(declarationsIn(sourceModel), declarationsIn(sourceModel.replace('      event CommentAdded\n        text String\n', '      event CommentAdded\n'))).missing, ['event CommentAdded.text']],
        ['an identical recovery has no differences', compareDeclarations(declarationsIn(sourceModel), declarationsIn(sourceModel)), { missing: [], unexpected: [] }],
        ['a recovered source reference is not a member', compareDeclarations(declarationsIn(sourceModel), declarationsIn(sourceModel.replace('      event CommentAdded\n', '      event CommentAdded\n        file Workspaces/Tracking/AddComment/AddComment.cs\n'))), { missing: [], unexpected: [] }],
        ['a renamed member type is still compared', declarationsIn('      readmodel CommentView\n        texts String[]\n').has('readmodel CommentView.texts'), true],
        ['every diagnostic code is read', diagnosticCodes('{"code":"PLAY0263"},{"code":"STAGE-SCENE-ACTION-001"},{"code":"CLI0017"}'), ['PLAY0263', 'STAGE-SCENE-ACTION-001', 'CLI0017']]
    ];

    const failures = checks.filter(([, actual, expected]) => JSON.stringify(actual) !== JSON.stringify(expected));
    for (const [name, actual, expected] of failures) console.log(`FAIL ${name}: expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`);
    console.log(`render audit self-test: ${checks.length - failures.length} of ${checks.length} checks passed`);
    process.exit(failures.length === 0 ? 0 : 1);
}

if (process.argv.includes('--self-test')) selfTest();
else audit();

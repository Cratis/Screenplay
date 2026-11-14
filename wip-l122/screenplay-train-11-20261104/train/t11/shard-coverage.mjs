// Local mirror of dotnet-build.yml "Verify spec shard coverage" (the CI step is inline Python).
// Exit 0 = every Debug test project in Screenplay.slnx is assigned to exactly one shard; 1 = not; 2 = could not run.
import { readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';

try {
    const workflow = readFileSync('.github/workflows/dotnet-build.yml', 'utf8');
    const block = workflow.split('SPEC_SHARDS: >-')[1]?.split(/\njobs:/)[0];
    if (!block) { console.error('SPEC_SHARDS not found'); process.exit(2); }
    const matrix = JSON.parse(block.replace(/\s+/g, ' '));
    const assigned = new Map();
    for (const shard of matrix.include) for (const p of shard.projects) assigned.set(p, (assigned.get(p) ?? 0) + 1);
    const slnx = readFileSync('Screenplay.slnx', 'utf8');
    const paths = [...slnx.matchAll(/<Project\s+Path="([^"]+)"/g)].map(m => m[1]);
    if (paths.length === 0) { console.error('no projects found in Screenplay.slnx'); process.exit(2); }
    const expected = new Set();
    for (const path of paths) {
        const isTest = execFileSync('dotnet', ['msbuild', path, '-nologo', '-property:Configuration=Debug', '-getProperty:IsTestProject'], { encoding: 'utf8' }).trim().toLowerCase();
        if (isTest === 'true') expected.add(path);
    }
    const missing = [...expected].filter(p => !assigned.has(p)).sort();
    const unexpected = [...assigned.keys()].filter(p => !expected.has(p)).sort();
    const duplicates = [...assigned].filter(([, c]) => c !== 1).map(([p]) => p).sort();
    console.log(`Examined ${paths.length} solution projects; ${expected.size} Debug test projects; ${assigned.size} assigned.`);
    if (missing.length || unexpected.length || duplicates.length) {
        console.error(`Invalid spec shards: missing=${JSON.stringify(missing)}, unexpected=${JSON.stringify(unexpected)}, duplicates=${JSON.stringify(duplicates)}`);
        process.exit(1);
    }
    console.log([...expected].sort().join('\n'));
} catch (error) {
    console.error(error);
    process.exit(2);
}

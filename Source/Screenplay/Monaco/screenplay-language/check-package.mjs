// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { cp, mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { tmpdir } from 'node:os';
import { basename, delimiter, dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { build } from 'esbuild';

const packageRoot = dirname(fileURLToPath(import.meta.url));
const require = createRequire(import.meta.url);

function npmPack(destination) {
    const args = ['pack', '--json', '--ignore-scripts', '--pack-destination', destination];
    const options = { cwd: packageRoot, encoding: 'utf8' };
    if (process.platform !== 'win32') return execFileSync('npm', args, options);

    // npm.cmd cannot be executed directly on Windows. Invoke its CLI through Node instead of a shell,
    // preserving paths with spaces. Yarn sets npm_execpath to yarn.js, so only accept npm's own CLI.
    const cli = [
        ...(process.env.npm_execpath && basename(process.env.npm_execpath).toLowerCase() === 'npm-cli.js' ? [process.env.npm_execpath] : []),
        ...[dirname(process.execPath), ...(process.env.PATH ?? '').split(delimiter)]
            .map(directory => join(directory, 'node_modules/npm/bin/npm-cli.js')),
    ].find(candidate => existsSync(candidate));
    if (!cli) throw new Error('Cannot locate npm-cli.js for the Windows package check');
    return execFileSync(process.execPath, [cli, ...args], options);
}

// Exercise the tarball, not workspace symlinks. The consumer lives outside the checkout and contains
// only the packed package and declared peers, so workspace dependencies cannot hide declaration leaks.
// A resolver guard also prevents the browser bundle from hiding an undeclared runtime dependency.
export async function checkPackage() {
    const consumer = await mkdtemp(join(tmpdir(), 'monaco-pack-'));
    try {
        const result = JSON.parse(npmPack(consumer));
        const packed = Object.values(result)[0];
        if (!packed?.filename) throw new Error('npm pack did not report a package filename');
        const installed = join(consumer, 'node_modules/@cratis/screenplay-language');
        await mkdir(installed, { recursive: true });
        // Relative paths from the consumer directory: a drive-letter path (C:\...) makes GNU tar treat it as a remote host.
        execFileSync('tar', ['-xzf', packed.filename, '-C', 'node_modules/@cratis/screenplay-language', '--strip-components=1'], { cwd: consumer });
        const manifest = JSON.parse(await readFile(join(installed, 'package.json'), 'utf8'));
        const peers = new Set(Object.keys(manifest.peerDependencies ?? {}));
        const entries = Object.entries(manifest.exports);
        for (const file of packed.files.filter(file => file.path.endsWith('.d.ts'))) {
            const declaration = await readFile(join(installed, file.path), 'utf8');
            if (declaration.includes('@cratis/screenplay-compiler')) throw new Error(`Private compiler dependency in packed declaration: ${file.path}`);
        }
        for (const peer of peers) {
            // Copy the installed public peer, not a workspace node_modules tree or a symlink back to it.
            await cp(dirname(require.resolve(`${peer}/package.json`)), join(consumer, 'node_modules', peer), { recursive: true, dereference: true });
        }
        await writeFile(join(consumer, 'package.json'), JSON.stringify({ private: true, type: 'module' }));
        await writeFile(join(consumer, 'consumer.ts'), entries.map(([subpath], index) => {
            const specifier = manifest.name + (subpath === '.' ? '' : subpath.slice(1));
            return `import * as entry${index} from ${JSON.stringify(specifier)};\nvoid entry${index};`;
        }).join('\n'));
        await writeFile(join(consumer, 'tsconfig.json'), JSON.stringify({
            compilerOptions: {
                target: 'ES2022', module: 'ESNext', moduleResolution: 'Bundler',
                lib: ['ES2022', 'DOM'], types: [], strict: true, skipLibCheck: false, noEmit: true,
            },
            files: ['consumer.ts'],
        }));
        execFileSync(process.execPath, [require.resolve('typescript/bin/tsc'), '-p', join(consumer, 'tsconfig.json')], { cwd: consumer, stdio: 'inherit' });
        for (const [subpath, target] of entries) {
            await readFile(join(installed, target.types));
            const specifier = manifest.name + (subpath === '.' ? '' : subpath.slice(1));
            await build({
                stdin: { contents: `import * as entry from ${JSON.stringify(specifier)}; console.log(entry);`, resolveDir: consumer },
                absWorkingDir: consumer,
                bundle: true,
                write: false,
                platform: 'browser',
                format: 'esm',
                plugins: [{ name: 'consumer-dependencies', setup(builder) {
                    builder.onResolve({ filter: /^[^./]/ }, args => {
                        if (args.path === specifier) return undefined;
                        if (peers.has(args.path)) return { path: args.path, external: true };
                        throw new Error(`Undeclared packed runtime dependency: ${args.path}`);
                    });
                } }],
            });
            // Monaco is a browser peer (and imports CSS), not a Node-loadable runtime. The browser
            // bundler above resolves the complete packed module graph, leaving only that peer external.
        }
        const runtimeBytes = packed.files.filter(file => file.path.endsWith('.js')).reduce((sum, file) => sum + file.size, 0);
        // Guards against accidentally bundling dependencies, not against language growth: the packed
        // The combined routes, dependency checks, typed examples, guarded actions, policy negation
        // and refusal authoring runtime is ~503 KB; 512 KB keeps a bounded margin for this surface.
        if (runtimeBytes > 512_000) throw new Error(`Monaco runtime bundle budget exceeded: ${runtimeBytes} bytes`);
        console.log(`Monaco pack: ${entries.length} exports type-checked and bundled; ${runtimeBytes} runtime bytes; ${packed.size} packed bytes`);
    } finally {
        // This directory was created exclusively by this check; never clean another build's output.
        await rm(consumer, { recursive: true, force: true });
    }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) await checkPackage();

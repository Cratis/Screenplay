// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { mkdir, mkdtemp, readFile, rm } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { build } from 'esbuild';

const packageRoot = dirname(fileURLToPath(import.meta.url));

// Exercise the tarball, not workspace symlinks. A resolver guard prevents ancestor node_modules from
// hiding an undeclared/private dependency; consumers may resolve only this package and declared peers.
export async function checkPackage() {
    const work = resolve(packageRoot, '../../../../.ai-work');
    await mkdir(work, { recursive: true });
    const consumer = await mkdtemp(join(work, 'monaco-pack-'));
    try {
        const result = JSON.parse(execFileSync('npm', ['pack', '--json', '--ignore-scripts', '--pack-destination', consumer], { cwd: packageRoot, encoding: 'utf8' }));
        const packed = Object.values(result)[0];
        if (!packed?.filename) throw new Error('npm pack did not report a package filename');
        const installed = join(consumer, 'node_modules/@cratis/screenplay-language');
        await mkdir(installed, { recursive: true });
        execFileSync('tar', ['-xzf', join(consumer, packed.filename), '-C', installed, '--strip-components=1']);
        const manifest = JSON.parse(await readFile(join(installed, 'package.json'), 'utf8'));
        const peers = new Set(Object.keys(manifest.peerDependencies ?? {}));
        const entries = Object.entries(manifest.exports);
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
        if (runtimeBytes > 400_000) throw new Error(`Monaco runtime bundle budget exceeded: ${runtimeBytes} bytes`);
        console.log(`Monaco pack: ${entries.length} exports resolved and bundled; ${runtimeBytes} runtime bytes; ${packed.size} packed bytes`);
    } finally {
        // This directory was created exclusively by this check; never clean another build's output.
        await rm(consumer, { recursive: true, force: true });
    }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) await checkPackage();

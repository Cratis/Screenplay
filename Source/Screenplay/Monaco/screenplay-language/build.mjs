// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawnSync } from 'node:child_process';
import { rm } from 'node:fs/promises';
import { build } from 'esbuild';
import { checkPackage } from './check-package.mjs';

// This package is also built on its own for npm publication. The compiler stays private: emit the
// declarations against its build, then bundle its browser-safe parser rather than publishing a runtime
// dependency that consumers cannot install. Every public entry shares one set of bundled modules.
for (const [command, args] of [
    ['yarn', ['workspace', '@cratis/screenplay-compiler', 'build']],
    ['yarn', ['exec', 'tsc', '-p', 'tsconfig.build.json', '--emitDeclarationOnly']],
]) {
    const result = spawnSync(command, args, { stdio: 'inherit', shell: process.platform === 'win32' });
    if (result.error) throw result.error;
    if (result.status !== 0) process.exit(result.status ?? 1);
}

// Only this build's owned runtime output is replaced; declaration/source output is not published as JS.
await rm('dist/bundles', { recursive: true, force: true });
await build({
    entryPoints: ['index.ts', 'sub-languages/capture/index.ts', 'sub-languages/projection/index.ts'],
    outdir: 'dist/bundles',
    outbase: '.',
    splitting: true,
    bundle: true,
    format: 'esm',
    platform: 'browser',
    target: 'es2022',
    external: ['monaco-editor'],
    sourcemap: true,
    // Minify local identifiers as the authoring surface grows, but preserve callable names
    // and source maps for readable stack traces. Public export names remain unchanged.
    minifyWhitespace: true,
    minifySyntax: true,
    minifyIdentifiers: true,
    keepNames: true,
});

await checkPackage();

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawnSync } from 'node:child_process';
import { build } from 'esbuild';

// This package is also built on its own for npm publication. The compiler stays private: emit the
// declarations against its build, then bundle its browser-safe parser rather than publishing a runtime
// dependency that consumers cannot install. Sub-language entry points retain their existing output.
for (const [command, args] of [
    ['yarn', ['workspace', '@cratis/screenplay-compiler', 'build']],
    ['yarn', ['exec', 'tsc', '-p', 'tsconfig.build.json']],
]) {
    const result = spawnSync(command, args, { stdio: 'inherit', shell: process.platform === 'win32' });
    if (result.error) throw result.error;
    if (result.status !== 0) process.exit(result.status ?? 1);
}

await build({
    entryPoints: ['index.ts'],
    outfile: 'dist/index.js',
    bundle: true,
    format: 'esm',
    platform: 'browser',
    target: 'es2022',
    external: ['monaco-editor'],
    sourcemap: true,
});

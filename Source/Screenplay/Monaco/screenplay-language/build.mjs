// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spawnSync } from 'node:child_process';
import { rm } from 'node:fs/promises';
import { dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { rolldown } from 'rolldown';
import { checkPackage } from './check-package.mjs';

// This package is also built on its own for npm publication. The compiler stays private: emit the
// declarations against its build, then include its browser-safe modules rather than publishing a runtime
// dependency that consumers cannot install. Rolldown is already installed by the workspace's Vite build.
// Preserve the module graph so consumers can tree-shake and split it with their own chunk budgets.
const packageRoot = dirname(fileURLToPath(import.meta.url));
const compilerRoot = resolve(packageRoot, '../../Compiler');
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
const graph = await rolldown({
    input: ['index.ts', 'sub-languages/capture/index.ts', 'sub-languages/projection/index.ts'],
    external: ['monaco-editor'],
    platform: 'browser',
    transform: { target: 'es2022' },
    resolve: { alias: { '@cratis/screenplay-compiler': resolve(compilerRoot, 'index.ts') } },
});
try {
    await graph.write({
        dir: 'dist/bundles',
        format: 'esm',
        preserveModules: true,
        // Keep every public entry at its existing exports-map path. Compiler files are private,
        // relative modules inside the tarball, never workspace paths or bare compiler imports.
        entryFileNames(chunk) {
            if (!chunk.facadeModuleId || chunk.facadeModuleId.startsWith('\0')) return '_virtual/[name].js';
            const compilerPath = relative(compilerRoot, chunk.facadeModuleId);
            const modulePath = compilerPath.startsWith('..')
                ? relative(packageRoot, chunk.facadeModuleId)
                : `compiler/${compilerPath}`;
            return modulePath.replaceAll('\\', '/').replace(/\.ts$/, '.js');
        },
        sourcemap: true,
        minify: true,
        keepNames: true,
    });
} finally {
    await graph.close();
}

await checkPackage();

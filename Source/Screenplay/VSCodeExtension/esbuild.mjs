// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as esbuild from 'esbuild';
import { fileURLToPath } from 'node:url';

const production = process.argv.includes('--production');
const watch = process.argv.includes('--watch');

/** @type {import('esbuild').BuildOptions} */
const extension = {
    entryPoints: ['extension.ts'],
    bundle: true,
    format: 'cjs',
    minify: production,
    sourcemap: !production,
    sourcesContent: false,
    platform: 'node',
    outfile: 'out/extension.js',
    external: ['vscode'],
    // Use workspace sources in this bundle so the extension and language service share one compiler.
    alias: { '@cratis/screenplay-language': fileURLToPath(new URL('../Monaco/screenplay-language/index.ts', import.meta.url)) },
    logLevel: 'info',
};

// The event model board runs in a webview: a browser page with everything it needs bundled beside it -
// React, the board and its stylesheets, and the fonts the stylesheets load.
/** @type {import('esbuild').BuildOptions} */
const webview = {
    entryPoints: ['Webview/main.tsx'],
    bundle: true,
    format: 'iife',
    minify: production,
    sourcemap: !production,
    sourcesContent: false,
    platform: 'browser',
    jsx: 'automatic',
    outfile: 'out/webview.js',
    loader: { '.woff': 'file', '.woff2': 'file', '.ttf': 'file', '.eot': 'file', '.svg': 'file' },
    define: { 'process.env.NODE_ENV': production ? '"production"' : '"development"' },
    logLevel: 'info',
};

if (watch) {
    const contexts = await Promise.all([esbuild.context(extension), esbuild.context(webview)]);
    await Promise.all(contexts.map(context => context.watch()));
    console.log('[esbuild] watching...');
} else {
    await Promise.all([esbuild.build(extension), esbuild.build(webview)]);
}

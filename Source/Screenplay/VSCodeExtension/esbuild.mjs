// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as esbuild from 'esbuild';
import { statSync } from 'node:fs';
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

// What the webview may weigh, in bytes. Everything the board draws with is bundled into one file that the
// editor parses before anything appears, and Scene's renderer - which draws the screens - is the largest
// thing in it. The ceiling is headroom over what it weighs today, not a target: a change that needs more
// than this is a change worth looking at, so raise it deliberately and say why rather than quietly.
const webviewCeiling = 8 * 1024 * 1024;

function holdTheWebviewToItsBudget() {
    const weight = statSync(webview.outfile).size;
    const megabytes = size => `${(size / (1024 * 1024)).toFixed(2)} MB`;
    if (weight > webviewCeiling) {
        console.error(`The webview bundle is ${megabytes(weight)}, over its ${megabytes(webviewCeiling)} ceiling.`);
        console.error('Either take weight out of it, draw the heavy part on demand, or raise the ceiling in esbuild.mjs deliberately.');
        process.exit(1);
    }
    console.log(`[esbuild] webview ${megabytes(weight)} of ${megabytes(webviewCeiling)}`);
}

if (watch) {
    const contexts = await Promise.all([esbuild.context(extension), esbuild.context(webview)]);
    await Promise.all(contexts.map(context => context.watch()));
    console.log('[esbuild] watching...');
} else {
    await Promise.all([esbuild.build(extension), esbuild.build(webview)]);
    if (production) {
        holdTheWebviewToItsBudget();
    }
}

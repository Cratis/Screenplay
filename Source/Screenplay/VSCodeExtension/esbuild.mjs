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

// What the webview may weigh in the bundle that ships. It grew from 2.16 MB to 7.08 MB when the board started
// drawing the real screens instead of a placeholder window: Scene's renderer, its blueprint packages and
// PrimeReact travel with it (Cratis/StudioIssues#1420). The script is read from disk inside the editor rather
// than downloaded, so the cost is install size and the first parse, not a page load - but it must not grow
// again without anyone noticing, which is what this is. Raise it deliberately, with the measurement in the
// pull request. Only the production bundle is held to it; a development build keeps its names and is larger.
const webviewBudgetBytes = 8_000_000;

function guardWebviewSize() {
    const bytes = statSync(webview.outfile).size;
    const megabytes = (bytes / 1_000_000).toFixed(2);
    if (!production) {
        console.log(`[esbuild] ${webview.outfile} is ${megabytes} MB; the ${(webviewBudgetBytes / 1_000_000).toFixed(2)} MB budget applies to the production bundle.`);
        return;
    }
    if (bytes > webviewBudgetBytes) {
        console.error(`[esbuild] ${webview.outfile} is ${megabytes} MB, over the ${(webviewBudgetBytes / 1_000_000).toFixed(2)} MB budget. Load what grew it on demand, or raise the budget in esbuild.mjs with the measurement in the pull request.`);
        process.exitCode = 1;
        return;
    }
    console.log(`[esbuild] ${webview.outfile} is ${megabytes} MB of the ${(webviewBudgetBytes / 1_000_000).toFixed(2)} MB budget.`);
}

if (watch) {
    const contexts = await Promise.all([esbuild.context(extension), esbuild.context(webview)]);
    await Promise.all(contexts.map(context => context.watch()));
    console.log('[esbuild] watching...');
} else {
    await Promise.all([esbuild.build(extension), esbuild.build(webview)]);
    guardWebviewSize();
}

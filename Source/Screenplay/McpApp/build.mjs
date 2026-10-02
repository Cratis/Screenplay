// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as esbuild from 'esbuild';
import { existsSync, readFileSync } from 'node:fs';
import { mkdir, writeFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { dirname, join, relative } from 'node:path';

// An MCP Apps view is one HTML page the server returns as a resource; the host renders it in a sandbox
// that loads nothing from the server, so the script and stylesheet are inlined into it. Fonts are the
// exception - the sandbox blocks fonts from data URLs - so they load from the npm CDN, whose origin the
// server declares, at the version of the package that ships them.
const cdn = 'https://cdn.jsdelivr.net/npm';
const iconFont = `${cdn}/primeicons@${packageOf(createRequire(import.meta.url).resolve('primeicons/primeicons.css'), 'primeicons').version}/primeicons.css`;

function packageOf(file, request) {
    for (let folder = dirname(file); folder !== dirname(folder); folder = dirname(folder)) {
        const manifest = join(folder, 'package.json');
        if (existsSync(manifest)) {
            const { name, version } = JSON.parse(readFileSync(manifest, 'utf8'));
            if (name && version) {
                return { name, version, folder };
            }
        }
    }
    throw new Error(`No package ships '${request}'.`);
}

const fontsFromCdn = {
    name: 'fonts-from-cdn',
    setup(build) {
        build.onResolve({ filter: /\.(woff2?|ttf|eot)$/ }, async args => {
            if (args.pluginData?.resolving) {
                return undefined;
            }
            const resolved = await build.resolve(args.path, { kind: args.kind, resolveDir: args.resolveDir, pluginData: { resolving: true } });
            if (resolved.errors.length > 0) {
                return { errors: resolved.errors };
            }
            const shipping = packageOf(resolved.path, args.path);
            return { path: `${cdn}/${shipping.name}@${shipping.version}/${relative(shipping.folder, resolved.path).split('\\').join('/')}`, external: true };
        });
    },
};

const result = await esbuild.build({
    entryPoints: ['main.tsx'],
    bundle: true,
    format: 'iife',
    minify: true,
    write: false,
    outdir: 'dist',
    platform: 'browser',
    jsx: 'automatic',
    loader: { '.svg': 'dataurl' },
    plugins: [fontsFromCdn],
    define: { 'process.env.NODE_ENV': '"production"' },
    logLevel: 'info',
});

const output = extension => result.outputFiles.filter(file => file.path.endsWith(extension)).map(file => file.text).join('\n');

// Text inside a script or style element ends at the first closing tag, so none may appear in it.
const script = output('.js').replace(/<\/script/gi, '<\\/script').replace(/<!--/g, '<\\!--');
const style = output('.css').replace(/<\/style/gi, '<\\/style');

const page = `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Event model board</title>
    <link rel="stylesheet" href="${iconFont}">
    <style>${style}</style>
</head>
<body>
    <div id="root"></div>
    <script>${script}</script>
</body>
</html>
`;

await mkdir('dist', { recursive: true });
await writeFile('dist/event-model-board.html', page);
console.log(`dist/event-model-board.html: ${page.length} characters`);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { randomBytes } from 'node:crypto';

// What the page needs from wherever it is hosted: where its script and stylesheet are, the source its
// content security policy allows them from, and a nonce for the one script it runs.
export interface BoardPage {
    readonly scriptUri: string;
    readonly styleUri: string;
    readonly cspSource: string;
    readonly nonce: string;
}

// The board's page. Scripts run only with the nonce. The board draws on a WebGL canvas, which creates its
// textures from blob and data URLs; styles are allowed inline because the component library sets them.
export function boardHtml(page: BoardPage): string {
    const policy = [
        'default-src \'none\'',
        `img-src ${page.cspSource} data: blob:`,
        `style-src ${page.cspSource} 'unsafe-inline'`,
        `font-src ${page.cspSource} data:`,
        `script-src 'nonce-${page.nonce}'`,
        'worker-src blob:',
    ].join('; ');
    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta http-equiv="Content-Security-Policy" content="${policy}">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <link rel="stylesheet" href="${page.styleUri}">
    <title>Event Model Board</title>
</head>
<body>
    <div id="root"></div>
    <script nonce="${page.nonce}" src="${page.scriptUri}"></script>
</body>
</html>`;
}

// A nonce for one page load.
export function createNonce(): string {
    return randomBytes(24).toString('base64url');
}

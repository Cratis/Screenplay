// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

// Specs live beside the source in for_/when_ folders and are named for the case they describe rather
// than carrying a .spec suffix, so the runner is pointed at the folders instead of a file suffix. A
// given/ folder holds shared setup rather than specs.
export default defineConfig({
    test: {
        include: ['**/for_*/**/*.ts'],
        exclude: ['**/node_modules/**', '**/dist/**', '**/out/**', '**/given/**'],
        setupFiles: ['./vitest.setup.ts'],
    },
    resolve: {
        alias: {
            // `vscode` ships as types only — the editor supplies the module at runtime, so there is
            // nothing to import outside an extension host. Aliasing it to a stub is what lets the files
            // that talk to the editor be specified here rather than only by pressing F5.
            vscode: fileURLToPath(new URL('./vscode.stub.ts', import.meta.url)),
        },
    },
});

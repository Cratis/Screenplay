// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';

// Specs live beside the source in for_/when_ folders and are named for the case they describe rather
// than carrying a .spec suffix. A given/ folder holds shared setup rather than specs.
export default defineConfig({
    test: {
        include: ['**/for_*/**/*.ts'],
        exclude: ['**/node_modules/**', '**/dist/**', '**/given/**'],
        setupFiles: ['./vitest.setup.ts'],
    },
});

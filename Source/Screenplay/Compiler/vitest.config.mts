// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';

// Specs live beside the source in for_/when_ folders and are named for the case they describe rather
// than carrying a .spec suffix, so the runner is pointed at the folders instead of a file suffix. A
// given/ folder holds shared setup rather than specs.
//
// Coverage is enforced: a change that adds code without specs for it fails the test run, and the
// thresholds only move up.
export default defineConfig({
    test: {
        include: ['**/for_*/**/*.ts'],
        exclude: ['**/node_modules/**', '**/dist/**', '**/out/**', '**/given/**'],
        setupFiles: ['./vitest.setup.ts'],
        coverage: {
            provider: 'v8',
            include: ['**/*.ts'],
            exclude: ['**/for_*/**', '**/dist/**', '**/index.ts', 'vitest.setup.ts'],
            reporter: ['text-summary'],
            thresholds: { lines: 99, statements: 99, functions: 98, branches: 96 },
        },
    },
});

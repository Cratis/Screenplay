// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vitest/config';

export default defineConfig({
    test: {
        include: ['**/for_*/**/*.ts'],
        exclude: ['**/node_modules/**', '**/dist/**', '**/given/**'],
        setupFiles: ['./vitest.setup.ts'],
    },
});

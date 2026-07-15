// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    build: {
        target: 'esnext',
        // Monaco is a single large dependency by design — silence the size hint.
        chunkSizeWarningLimit: 5000,
    },
    server: {
        port: 9200,
        open: false,
    },
});

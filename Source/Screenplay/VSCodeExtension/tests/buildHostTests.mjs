// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { build } from 'esbuild';
await build({ entryPoints: ['tests/extensionHost.ts'], outfile: 'out/tests/extensionHost.cjs', bundle: true, platform: 'node', format: 'cjs', external: ['vscode'], sourcemap: true });

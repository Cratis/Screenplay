// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { SpawnOptions } from 'node:child_process';
import * as path from 'node:path';

/** Release only the Windows child-CWD lock for the adversarial root-replacement fixture. */
export function rootReplacementLaunchOptions(executable: string, options: SpawnOptions | undefined, server: string | undefined, fixtureRoot: string, platform = process.platform): SpawnOptions | undefined {
    if (platform !== 'win32' || executable !== server || options?.cwd !== path.win32.join(fixtureRoot, 'root-replacement')) return options;
    // The absolute approved root argument and every RPC remain unchanged. The actual
    // watcher stays live; this permits a real inode replacement rather than skipping it
    // because Windows locks a running process's current working directory.
    return { ...options, cwd: fixtureRoot };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'node:fs';

/** Verified unsupported-host classification: Windows volumes reporting zero identity cannot register a watcher. */
export function zeroVolumeIdentityWindowsHost(model: string): boolean {
    return process.platform === 'win32' && fs.statSync(model, { bigint: true }).dev === 0n;
}

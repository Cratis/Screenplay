// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';

/** Create a unique physical fixture directory outside the repository. The caller owns cleanup. */
export function specFixtureDirectory(prefix: string): string {
    const parent = path.join(os.tmpdir(), 'screenplay-specs');
    fs.mkdirSync(parent, { recursive: true });
    return fs.realpathSync.native(fs.mkdtempSync(path.join(parent, prefix)));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, it } from 'vitest';
import * as fs from 'node:fs';
import * as os from 'node:os';
import * as path from 'node:path';
import { specFixtureDirectory } from '../tests/specFixtureDirectory';

let directories: string[] = [];
beforeEach(() => {
    directories = [];
    directories.push(specFixtureDirectory('fixture-directory-'));
    directories.push(specFixtureDirectory('fixture-directory-'));
});
afterEach(() => {
    for (const directory of directories) fs.rmSync(directory, { recursive: true, force: true });
});
it('should create distinct existing physical directories under the system temporary fixture parent', () => {
    const parent = fs.realpathSync.native(path.join(os.tmpdir(), 'screenplay-specs'));
    directories[0].should.not.equal(directories[1]);
    for (const directory of directories) {
        path.dirname(directory).should.equal(parent);
        fs.realpathSync.native(directory).should.equal(directory);
        fs.statSync(directory).isDirectory().should.be.true;
    }
});

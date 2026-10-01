// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const repository = resolve(__dirname, '../../../..');

function playFiles(folder: string): string[] {
    return readdirSync(folder).flatMap(entry => {
        const path = join(folder, entry);
        if (entry === 'node_modules' || entry === 'bin' || entry === 'obj') {
            return [];
        }
        return statSync(path).isDirectory() ? playFiles(path) : path.endsWith('.play') ? [path] : [];
    });
}

// Every .play document the repository ships compiles with the C# compiler, so the TypeScript compiler must
// read each of them without an error too.
describe('when parsing the repository samples', () => {
    let files: string[];
    let errors: string[];

    beforeAll(() => {
        files = playFiles(join(repository, 'Source'));
        errors = files.flatMap(file => parse(readFileSync(file, 'utf8'), file).diagnostics
            .filter(diagnostic => diagnostic.severity === 'error')
            .map(diagnostic => `${file}:${diagnostic.location.line} ${diagnostic.code} ${diagnostic.message}`));
    });

    it('should find the samples', () => {
        files.length.should.be.greaterThan(20);
    });

    it('should read every sample without an error', () => {
        errors.should.deep.equal([]);
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';

const source = resolve(__dirname, '../../../..');

function playFiles(folder: string): string[] {
    return readdirSync(folder).flatMap(entry => {
        const path = join(folder, entry);
        if (entry === 'node_modules' || entry === 'bin' || entry === 'obj') {
            return [];
        }
        return statSync(path).isDirectory() ? playFiles(path) : path.endsWith('.play') ? [path] : [];
    });
}

describe('when mapping the repository samples', () => {
    let files: string[];
    let problems: string[];

    beforeAll(() => {
        files = playFiles(source);
        problems = files.flatMap(file => problems_the_board_finds_in(toEventModelDocument(parse(readFileSync(file, 'utf8')).value, file))
            .map(problem => `${file}: ${problem}`));
    });

    it('should find the samples', () => {
        files.length.should.be.greaterThan(20);
    });

    it('should map every sample into a document the board can read', () => {
        problems.should.deep.equal([]);
    });
});

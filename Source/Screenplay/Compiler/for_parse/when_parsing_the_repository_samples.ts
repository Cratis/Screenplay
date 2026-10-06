// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { describe, beforeAll, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const repository = resolve(__dirname, '../../../..');
const generatedInputSource = join(repository, 'Source/DotNET/Screenplay.CanonicalCorpus/Corpus/RegisterProject/v7/rejected/generated-input.play');

function playFiles(folder: string): string[] {
    return readdirSync(folder).flatMap(entry => {
        const path = join(folder, entry);
        if (entry === 'node_modules' || entry === 'bin' || entry === 'obj') {
            return [];
        }
        return statSync(path).isDirectory() ? playFiles(path) : path.endsWith('.play') ? [path] : [];
    });
}

// Semantic rejection vectors such as UnsupportedSequence still parse without errors. The generated-input
// source is deliberately invalid at the parser boundary, so require its exact diagnostic rather than skip it.
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

    it('should report only the expected generated-input rejection', () => {
        errors.should.deep.equal([
            `${generatedInputSource}:10 PLAY0485 Generated property 'projectId' cannot be supplied as request or form input.`
        ]);
    });
});

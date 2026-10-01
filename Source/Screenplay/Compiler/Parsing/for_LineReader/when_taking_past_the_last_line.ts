// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { LineReader } from '../LineReader';
import { splitLines } from '../SourceLineSplitter';

describe('when taking past the last line', () => {
    let reader: LineReader;

    beforeEach(() => {
        reader = new LineReader(splitLines('module A\n\n'));
        reader.takeSignificant();
    });

    it('should have no significant line left', () => {
        (reader.peekSignificant() === undefined).should.be.true;
    });

    it('should refuse to take one', () => {
        (() => reader.takeSignificant()).should.throw('There is no significant line left to take');
    });

    // A raw read takes the blank lines a significant read steps over - a fenced block keeps its blank lines.
    it('should still take the blank lines raw, then nothing', () => {
        [reader.takeRaw()?.content, reader.takeRaw()?.content, reader.takeRaw()].should.deep.equal(['', '', undefined]);
    });
});

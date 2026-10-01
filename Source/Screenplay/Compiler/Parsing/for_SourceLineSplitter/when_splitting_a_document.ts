// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { splitLines } from '../SourceLineSplitter';
import { SourceLine } from '../SourceLine';

describe('when splitting a document', () => {
    let lines: SourceLine[];

    beforeEach(() => {
        lines = splitLines([
            'module Projects',
            '  feature Registration   // the only feature',
            '',
            '    description "a // is not a comment in a string"\r',
        ].join('\n'), false, 'Projects.play');
    });

    it('should number every line from one', () => {
        lines.map(line => line.number).should.deep.equal([1, 2, 3, 4]);
    });

    it('should measure the indent', () => {
        lines.map(line => line.indent).should.deep.equal([0, 2, 0, 4]);
    });

    it('should strip a trailing comment and whitespace', () => {
        lines[1].content.should.equal('feature Registration');
    });

    it('should keep comment markers inside a string', () => {
        lines[3].content.should.equal('description "a // is not a comment in a string"');
    });

    it('should drop the carriage return of a windows line ending', () => {
        lines[3].raw.endsWith('\r').should.be.false;
    });

    it('should leave a blank line without content', () => {
        lines[2].content.should.equal('');
    });

    it('should carry the path', () => {
        lines[0].path!.should.equal('Projects.play');
    });

    it('should record where each line starts in the source', () => {
        lines.map(line => line.startOffset).should.deep.equal([0, 16, 61, 62]);
    });
});

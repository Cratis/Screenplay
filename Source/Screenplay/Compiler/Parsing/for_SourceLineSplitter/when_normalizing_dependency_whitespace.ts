// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { splitLines } from '../SourceLineSplitter';
import { SourceLine } from '../SourceLine';

describe('when normalizing dependency whitespace', () => {
    let lines: SourceLine[];
    beforeEach(() => {
        lines = splitLines('  depends\ton B\u0085\n  depends on B\ufeff\n  dependsOn String\ufeff');
    });
    it('should normalize native trailing whitespace after dependency targets', () => {
        lines[0].content.should.equal('depends\ton B');
    });
    it('should retain a trailing byte order mark for the dependency parser to reject', () => {
        lines[1].content.should.equal('depends on B\ufeff');
    });
    it('should leave similarly named properties on their existing normalization path', () => {
        lines[2].content.should.equal('dependsOn String');
    });
});

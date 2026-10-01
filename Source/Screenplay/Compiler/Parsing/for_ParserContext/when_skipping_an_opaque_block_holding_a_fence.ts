// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { LineReader } from '../LineReader';
import { ParserContext } from '../ParserContext';
import { splitLines } from '../SourceLineSplitter';

// A fenced line may sit further left than the block it belongs to. Read as significant it would end the
// skip and the code would be parsed as Screenplay.
describe('when skipping an opaque block holding a fence', () => {
    let context: ParserContext;

    beforeEach(() => {
        context = new ParserContext(new LineReader(splitLines([
            '      screen Register',
            '        ```tsx',
            'export const Register = () => <div/>;',
            '',
            '        ```',
            '      event Registered',
        ].join('\n'))));
        const header = context.reader.takeSignificant();
        context.skipOpaqueBlock(header.indent);
    });

    it('should stop at the next sibling', () => {
        context.reader.peekSignificant()!.content.should.equal('event Registered');
    });
});

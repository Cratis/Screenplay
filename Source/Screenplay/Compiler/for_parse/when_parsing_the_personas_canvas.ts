// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing the personas canvas', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'persona Shopper',
            '  description "Browses and buys"',
            '  goal "Find the right product quickly"',
            '  goal "Check out in \\"under\\" a minute"',
            '  position 40 -20',
            '  size 320 180',
            'persona Buyer',
            'sticky note on personas',
            '  description "Pain point: slow checkout"',
            '  colour pink',
            '  position 400 40',
            '  size 220 140',
            'sticky note on personas',
            '  description "Context"',
            '  position 400 220',
        );
    });

    it('should succeed without diagnostics', () => result.diagnostics.length.should.equal(0));
    it('should keep the goals in order', () => result.value!.personas[0].goals!.should.deep.equal(['Find the right product quickly', 'Check out in "under" a minute']));
    it('should keep the position', () => [result.value!.personas[0].x, result.value!.personas[0].y].should.deep.equal([40, -20]));
    it('should keep the size', () => [result.value!.personas[0].width, result.value!.personas[0].height].should.deep.equal([320, 180]));
    it('should leave an unplaced persona without a position', () => (result.value!.personas[1].x === undefined).should.be.true);
    it('should keep both notes', () => result.value!.stickyNotes!.length.should.equal(2));
    it('should keep the text, colour and size of a note', () => {
        const note = result.value!.stickyNotes![0];
        [note.text, note.colour, note.x, note.y, note.width, note.height].should.deep.equal(['Pain point: slow checkout', 'pink', 400, 40, 220, 140]);
    });
    it('should leave a bare note uncoloured and unsized', () => {
        const note = result.value!.stickyNotes![1];
        [note.colour, note.width, note.height].should.deep.equal([null, null, null]);
    });
});

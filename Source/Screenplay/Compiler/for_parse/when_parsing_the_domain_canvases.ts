// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing the domain canvases', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'concept ProductId : Uuid',
            'concept CustomerId : Uuid',
            'eventsource Product',
            '  identifier ProductId',
            '  position 110 -150',
            'eventsource Customer',
            '  identifier CustomerId',
            'seed "Starter catalogue"',
            '  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e1001"',
            '    ProductDrafted',
            '      sku = "ACM-1000"',
            '  position 20 400',
            'sticky note on eventsources',
            '  description "Open question"',
            '  colour pink',
            '  position 480 150',
            '  size 240 140',
            'sticky note on givencontexts',
            '  description "A paid order"',
            '  position -40 80',
            'sticky note on seeddata',
            '  description "Seed it"',
            '  position 20 400',
            'sticky note on personas',
            '  description "Persona question"',
            '  position 0 0',
        );
    });

    it('should succeed without diagnostics', () => result.diagnostics.map(diagnostic => diagnostic.message).should.deep.equal([]));
    it('should keep where an event source sits', () => [result.value!.eventSources![0].x, result.value!.eventSources![0].y].should.deep.equal([110, -150]));
    it('should leave an unplaced event source without a position', () => (result.value!.eventSources![1].x === undefined).should.be.true);
    it('should keep the name of the seed data', () => result.value!.seeds![0].name!.should.equal('Starter catalogue'));
    it('should keep where the seed data sits', () => [result.value!.seeds![0].x, result.value!.seeds![0].y].should.deep.equal([20, 400]));
    it('should keep the canvas of every note', () => result.value!.stickyNotes!.map(note => note.canvas).should.deep.equal(['eventsources', 'givencontexts', 'seeddata', 'personas']));
    it('should keep the colour and size of a note', () => {
        const note = result.value!.stickyNotes![0];
        [note.colour, note.width, note.height].should.deep.equal(['pink', 240, 140]);
    });
});

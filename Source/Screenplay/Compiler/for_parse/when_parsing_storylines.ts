// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing storylines', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'module Returns',
            'storyline ReturnAnItem "A customer returns an item"',
            '  description "From the request to the refund"',
            '  module Returns',
            '  feature Returns.Requests "Where it starts"',
            '  readmodel PendingReturn',
            'storyline Bare',
        );
    });

    it('should succeed without diagnostics', () => result.diagnostics.length.should.equal(0));
    it('should keep every storyline in order', () => result.value!.storylines!.map(storyline => storyline.name).should.deep.equal(['ReturnAnItem', 'Bare']));
    it('should keep the title', () => (result.value!.storylines![0].title === 'A customer returns an item').should.be.true);
    it('should keep the description', () => (result.value!.storylines![0].description === 'From the request to the refund').should.be.true);
    it('should keep the steps in order with their kinds', () => result.value!.storylines![0].steps.map(step => `${step.elementKind} ${step.target}`).should.deep.equal(['Module Returns', 'Feature Returns.Requests', 'ReadModel PendingReturn']));
    it('should keep a step note', () => (result.value!.storylines![0].steps[1].note === 'Where it starts').should.be.true);
    it('should leave a missing title null', () => (result.value!.storylines![1].title === null).should.be.true);
});

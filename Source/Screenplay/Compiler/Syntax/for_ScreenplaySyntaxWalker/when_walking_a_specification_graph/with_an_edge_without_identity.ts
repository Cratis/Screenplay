// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { SpecificationGraphEdgeSyntax, SpecificationGraphSyntax } from '../../Graphs';
import { a_recording_walker } from '../given/a_recording_walker';
import { given } from '../given/given';
import { literal, location, mapping } from '../given/syntax';

describe('when walking a specification graph with an edge without identity', given(a_recording_walker, context => {
    const property = mapping('name');
    const edge: SpecificationGraphEdgeSyntax = { kind: 'SpecificationGraphEdgeSyntax', type: 'Owns', source: literal('source'),
        target: literal('target'), identity: null, absent: false, properties: [property], location };
    const syntax: SpecificationGraphSyntax = { kind: 'SpecificationGraphSyntax', graph: 'Accounts', elements: [edge], location };

    beforeEach(() => context.visitSpecificationGraph(syntax));

    it('should not dispatch a null identity', () => {
        context.hooks.should.deep.equal(['node', 'node', 'expression', 'node', 'expression', 'node', 'mapping', 'node', 'expression', 'node']);
    });

    it('should visit property mappings immediately after the endpoints', () => {
        context.nodes.should.have.ordered.members([
            syntax, edge, edge.source, edge.source, edge.target, edge.target, property, property, property.source, property.source,
        ]);
    });
}));

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { SpecificationGraphNodeSyntax, SpecificationGraphSyntax } from '../../Graphs';
import { a_recording_walker } from '../given/a_recording_walker';
import { given } from '../given/given';
import { literal, location, mapping } from '../given/syntax';

describe('when walking a specification graph with a node', given(a_recording_walker, context => {
    const firstProperty = mapping('name');
    const secondProperty = mapping('status');
    const node: SpecificationGraphNodeSyntax = { kind: 'SpecificationGraphNodeSyntax', label: 'Account', key: literal('first'),
        absent: false, properties: [firstProperty, secondProperty], location };
    const sibling: SpecificationGraphNodeSyntax = { kind: 'SpecificationGraphNodeSyntax', label: 'Account', key: literal('second'),
        absent: false, properties: [], location };
    const syntax: SpecificationGraphSyntax = { kind: 'SpecificationGraphSyntax', graph: 'Accounts', elements: [node, sibling], location };

    beforeEach(() => context.visitSpecificationGraph(syntax));

    it('should dispatch the key and mappings through public hooks', () => {
        context.hooks.should.deep.equal([
            'node', 'node', 'expression', 'node', 'mapping', 'node', 'expression', 'node',
            'mapping', 'node', 'expression', 'node', 'node', 'expression', 'node',
        ]);
    });

    it('should visit the node key then all property mappings before the next element', () => {
        context.nodes.should.have.ordered.members([
            syntax, node, node.key, node.key, firstProperty, firstProperty, firstProperty.source, firstProperty.source,
            secondProperty, secondProperty, secondProperty.source, secondProperty.source, sibling, sibling.key, sibling.key,
        ]);
    });
}));

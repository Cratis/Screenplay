// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ExpressionSyntax } from '../Expressions';
import { SpecificationGraphNodeSyntax, SpecificationGraphSyntax } from '../Graphs';
import { a_recording_walker } from './given/a_recording_walker';
import { given } from './given/given';
import { literal, location, mapping } from './given/syntax';

class a_throwing_walker extends a_recording_walker {
    readonly failure = new Error('child hook failed');
    readonly trigger = literal('throw');

    override visitExpression(syntax: ExpressionSyntax): void {
        super.visitExpression(syntax);
        if (syntax === this.trigger) throw this.failure;
    }
}

describe('when a child hook throws', given(a_throwing_walker, context => {
    const first = { ...mapping('first'), source: context.trigger };
    const second = mapping('second');
    const node: SpecificationGraphNodeSyntax = { kind: 'SpecificationGraphNodeSyntax', label: 'Account', key: literal('key'),
        absent: false, properties: [first, second], location };
    const sibling: SpecificationGraphNodeSyntax = { ...node, key: literal('sibling') };
    const syntax: SpecificationGraphSyntax = { kind: 'SpecificationGraphSyntax', graph: 'Accounts', elements: [node, sibling], location };
    let caught: unknown;

    beforeEach(() => {
        caught = undefined;
        try {
            context.visitSpecificationGraph(syntax);
        } catch (error) {
            caught = error;
        }
    });

    it('should propagate the same exception object', () => {
        (caught === context.failure).should.be.true;
    });

    it('should stop before later properties and graph elements', () => {
        context.hooks.should.deep.equal(['node', 'node', 'expression', 'node', 'mapping', 'node', 'expression', 'node']);
        context.nodes.should.have.ordered.members([syntax, node, node.key, node.key, first, first, context.trigger, context.trigger]);
    });
}));

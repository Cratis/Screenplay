// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../ScreenplayCompiler';
import { InvalidSyntaxJson } from '../Syntax/InvalidSyntaxJson';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SpecificationEventSyntax } from '../Syntax/Specifications';
import { toSyntaxJson as writeSyntaxJson } from '../Syntax/SyntaxJson';
import { SyntaxNode } from '../Syntax/SyntaxNode';

const toSyntaxJson = <Node extends SyntaxNode>(node: Node) => writeSyntaxJson(node);

const specification = parseSpecificationSource(`specification X
  when append E
    for "other"
    stream Account.Profile
      streamId = "key"
  then E
    no stream`).value[0];
const occurrence = specification.whenAppended!;
const route = occurrence.stream!;
const noStream = specification.thenEvents[0].noStream!;

class route_nodes extends ScreenplaySyntaxWalker {
    readonly kinds: string[] = [];
    visitNode(node: SyntaxNode): void { this.kinds.push(node.kind); }
}

describe('when serializing specification stream invariants', () => {
    it('should refuse conflicting routing markers', () => (() => toSyntaxJson({ ...occurrence, noStream })).should.throw(InvalidSyntaxJson));
    it('should refuse a stream metadata node of the wrong kind', () => (() => toSyntaxJson({ ...occurrence, stream: noStream } as unknown as SpecificationEventSyntax)).should.throw(InvalidSyntaxJson));
    it('should refuse an unrouted metadata node of the wrong kind', () => (() => toSyntaxJson({ ...occurrence, stream: undefined, noStream: route } as unknown as SpecificationEventSyntax)).should.throw(InvalidSyntaxJson));
    it('should refuse route mappings that target payload', () => (() => toSyntaxJson({ ...route, streamId: { ...route.streamId!, property: 'payload' } })).should.throw(InvalidSyntaxJson));
    it('should preserve the stream id and omit source spans', () => [toSyntaxJson(route)].should.deep.equal([{ kind: 'SpecificationStreamSyntax', eventSource: 'Account', stream: 'Profile', streamId: { kind: 'PropertyMappingSyntax', property: 'streamId', source: { kind: 'LiteralExpressionSyntax', value: 'key' } } }]));
});

describe('when walking specification streams', () => {
    let nodes: route_nodes;
    beforeEach(() => {
        nodes = new route_nodes();
        nodes.visitSpecification(specification);
    });
    it('should visit the route and unrouted marker', () => nodes.kinds.should.include.members(['SpecificationStreamSyntax', 'SpecificationNoStreamSyntax']));
    it('should visit the stream id mapping and literal', () => nodes.kinds.filter(kind => kind === 'PropertyMappingSyntax' || kind === 'LiteralExpressionSyntax').should.deep.equal(['PropertyMappingSyntax', 'LiteralExpressionSyntax', 'LiteralExpressionSyntax']));
});

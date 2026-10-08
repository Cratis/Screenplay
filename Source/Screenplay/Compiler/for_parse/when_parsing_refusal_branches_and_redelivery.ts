// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { decodeExactSyntaxJson } from '../Syntax/StrictSyntaxJson';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { ApplicationSyntax } from '../Syntax/Structure';

const source = readFileSync(resolve(__dirname, '../Conformance/reaction-refusals-redelivery.play'), 'utf8');
const application = () => parse(source).value;
const slice = (value: ApplicationSyntax) => value.modules[0].features[0].slices[0];

class visited_nodes extends ScreenplaySyntaxWalker {
    readonly kinds: string[] = [];
    visitNode(node: SyntaxNode): void { this.kinds.push(node.kind); }
}

describe('when parsing refusal branches and redelivery', () => {
    it.each(['on', '@on'])('should retain the invocation mapping named %s', property => {
        const result = parse(`module Billing\n  feature Claims\n    slice Automation Claiming\n      reaction Claimer\n        when Approved\n          invokes Claim\n            ${property} = "value"`);
        result.diagnostics.should.be.empty;
        const invocation = slice(result.value).reactions[0].triggers[0].invokes[0];
        expect(invocation.onRefused).toEqual([]);
        expect(invocation.mappings![0]).toMatchObject({ property: 'on', source: { kind: 'LiteralExpressionSyntax', value: 'value' } });
    });

    it('should retain the ordered selectors and branch effects', () => {
        parse(source).diagnostics.should.be.empty;
        const branches = slice(application()).reactions[0].triggers[0].invokes[0].onRefused!;
        branches.map(branch => branch.selector).should.deep.equal(['constraint', 'validation', 'constraint', 'authorization', 'any']);
        branches[0].constraint!.should.equal('Handling.UniqueClaim');
        branches[0].produces[0].mappings.map(mapping => mapping.source.kind).should.deep.equal(['RefusalExpressionSyntax', 'RefusalExpressionSyntax', 'RefusalExpressionSyntax']);
        branches[1].acknowledge.should.be.true;
    });

    it('should retain the event reaction and occurrence locator', () => {
        const redelivery = slice(application()).specifications[0].whenRedelivered!;
        redelivery.eventType.should.equal('Approved');
        redelivery.reaction.should.equal('Claimer');
        expect(redelivery.for).toMatchObject({ kind: 'LiteralExpressionSyntax', value: 'invoice-1' });
        redelivery.values[0].property.should.equal('invoice');
    });

    it('should round trip the new source members through exact syntax JSON', () => {
        const exact = parse(`numbers exact\n${source}`).value;
        const json = toSyntaxJson(exact);
        const restored = slice(decodeExactSyntaxJson(JSON.stringify(json)) as ApplicationSyntax);
        expect(toSyntaxJson(restored.reactions[0].triggers[0].invokes[0])).toEqual(toSyntaxJson(slice(exact).reactions[0].triggers[0].invokes[0]));
        expect(toSyntaxJson(restored.specifications[0].whenRedelivered!)).toEqual(toSyntaxJson(slice(exact).specifications[0].whenRedelivered!));
    });

    it('should walk the branches values productions and redelivery', () => {
        const walker = new visited_nodes();
        walker.visitApplication(application());
        walker.kinds.filter(kind => kind === 'InvocationRefusalSyntax').length.should.equal(5);
        walker.kinds.filter(kind => kind === 'RefusalExpressionSyntax').length.should.equal(3);
        walker.kinds.filter(kind => kind === 'SpecificationRedeliverySyntax').length.should.equal(1);
    });

    it('should preserve legacy bytes for absent branches and redelivery', () => {
        const value = parse(source.replace('numbers exact\n', '').replace(/ {12}on refused[^]*? {6}specification/, '      specification').replace(/ {8}when redelivered[^]*? {8}then no events/, '        when Claim')).value;
        expect(toSyntaxJson(slice(value).reactions[0].triggers[0].invokes[0])).not.toHaveProperty('onRefused');
        expect(toSyntaxJson(slice(value).specifications[0])).not.toHaveProperty('whenRedelivered');
    });
});

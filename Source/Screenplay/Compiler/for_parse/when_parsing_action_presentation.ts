// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { ScreenActionSyntax } from '../Syntax/Screens';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';

const source = (body: string) => `concept ItemId : Uuid
module Work
  feature Items
    slice StateView List
      readmodel Row
        rowId ItemId key
      command Retry
        itemId ItemId identifier
      query Rows => Row[]
      screen List
        data Row[] via query Rows
        action Retry for item label "Retry"
          with itemId from item.rowId
          navigate to List
${body}
        action Retry for screen`;
const hint = '          presentation style "quiet"\n          presentation placement "row-menu"';
const result = parse(source(hint));
const actions = result.value.modules[0].features[0].slices[0].screens[0].directives.filter(directive => directive.kind === 'ScreenActionSyntax') as ScreenActionSyntax[];

class PresentationWalker extends ScreenplaySyntaxWalker {
    readonly values: SyntaxNode[] = [];
    override visitNode(node: SyntaxNode): void { if (node.kind === 'PresentationValueSyntax') this.values.push(node); }
}

describe('when parsing action presentation', () => {
    it('should accept the hint with ordinary action children', () => expect(result.diagnostics).toEqual([]));
    it('should retain authored order and values', () => expect(actions[0].presentation?.map(value => [value.name, value.value])).toEqual([['style', 'quiet'], ['placement', 'row-menu']]));
    it('should leave action scope and bindings unchanged', () => {
        expect(actions[0].subject).toBe('Item');
        expect(actions[0].inputs?.[0].binding).toBe('item.rowId');
        expect(actions[0].label).toBe('Retry');
        expect(actions[0].navigate?.screen).toBe('List');
    });
    it('should omit unhinted presentation', () => expect(actions[1].presentation).toBeUndefined());
    for (const scope of ['for screen', '']) {
        it(`should reject presentation without an explicit item subject: ${scope}`, () => expect(parse(source(hint).replace('for item', scope)).diagnostics.some(diagnostic => diagnostic.code === 'PLAY1166')).toBe(true));
    }
    it('should walk the presentation with its source location', () => {
        const walker = new PresentationWalker();
        walker.visitApplication(result.value);
        expect(walker.values.map(value => value.location.line)).toEqual([15, 16]);
    });
    for (const body of ['presentation placement "future-menu"', 'presentation extension "custom"']) {
        it(`should preserve an unknown advisory value: ${body}`, () => expect(parse(source(`          ${body}`)).diagnostics).toEqual([]));
    }
    for (const body of ['presentation placement ""', 'presentation placement " "', 'presentation placement "row-menu"\n          presentation placement "other"']) {
        it(`should reject invalid values: ${body}`, () => expect(parse(source(`          ${body}`)).diagnostics.some(diagnostic => diagnostic.code === 'PLAY1166')).toBe(true));
    }
    for (const body of ['presentation', 'presentation placement', 'placement "row-menu"']) {
        it(`should reject malformed children: ${body}`, () => expect(parse(source(`          ${body}`)).diagnostics.some(diagnostic => diagnostic.severity === 'error')).toBe(true));
    }
});

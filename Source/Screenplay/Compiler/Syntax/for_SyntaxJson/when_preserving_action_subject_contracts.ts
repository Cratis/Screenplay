// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../Structure';
import { ScreenActionSyntax } from '../Screens';
import { decodeExactSyntaxJson, InvalidSyntaxJson } from '../StrictSyntaxJson';
import { toSyntaxJson } from '../SyntaxJson';
import { ScreenplaySyntaxWalker } from '../ScreenplaySyntaxWalker';
import { SyntaxNode } from '../SyntaxNode';

const source = `module Work
  feature Items
    slice StateView Details
      screen Details
        action Retry for item label "Retry for screen"
          with id from item.foreignKey
          with note from $context.note
          navigate to Details
        action "Choose" for screen
          when item.ready == true execute Retry
            with id from item.foreignKey
          otherwise execute Release
            with id from item.foreignKey
        toolbar
          item retry action Retry for screen label "Try"
            parameter id from item.foreignKey
`;
const raw = parse(source).value;
const action = (application: ApplicationSyntax) => application.modules[0].features[0].slices[0].screens[0].directives[0] as ScreenActionSyntax;
const wire = JSON.stringify(toSyntaxJson(parse(`numbers exact\n${source}`).value));

class CountingWalker extends ScreenplaySyntaxWalker {
    readonly nodes: SyntaxNode[] = [];
    override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
}

describe('when preserving action subject contracts', () => {
    it('should strictly restore new metadata and ordered inputs', () => {
        const restored = decodeExactSyntaxJson(wire) as ApplicationSyntax;
        expect(toSyntaxJson(action(restored))).toMatchObject(toSyntaxJson(action(raw)) as object);
        expect(action(restored).inputs!.map(input => input.name)).toEqual(['id', 'note']);
    });
    it('should omit legacy default additions', () => {
        const legacy = parse(source.replace(' for item', '').replace('          with id from item.foreignKey\n          with note from $context.note\n', '')).value;
        const encoded = toSyntaxJson(action(legacy));
        expect(encoded).not.toHaveProperty('subject');
        expect(encoded).not.toHaveProperty('inputs');
    });
    it('should refuse unknown enum values', () => expect(() => decodeExactSyntaxJson(wire.replace('"Item"', '"Row"'))).toThrow(InvalidSyntaxJson));
    it('should refuse subject metadata on non-action toolbar items', () => expect(() => decodeExactSyntaxJson(wire.replace('"syntaxKind":"Action"', '"syntaxKind":"Navigate"'))).toThrow(InvalidSyntaxJson));
    it('should walk plain inputs and preserve authored locations', () => {
        const walker = new CountingWalker();
        walker.visitApplication(raw);
        expect(walker.nodes.filter(node => node.kind === 'InteractionArgumentSyntax')).toHaveLength(4);
        expect(action(raw).inputs!.map(input => input.location.line)).toEqual([6, 7]);
    });
    for (const header of ['action Retry for', 'action Retry for planet', 'action Retry for item for screen', 'toolbar\n          item go navigate to Details for item']) {
        it(`should reject malformed or inapplicable scope: ${header}`, () => {
            expect(parse(source.replace('action Retry for item label "Retry for screen"', header)).diagnostics.some(diagnostic => diagnostic.code === 'PLAY0817')).toBe(true);
        });
    }
});

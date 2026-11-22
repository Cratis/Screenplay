// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ScreenActionSyntax } from '../Screens';
import { ApplicationSyntax } from '../Structure';
import { decodeExactSyntaxJson, InvalidSyntaxJson } from '../StrictSyntaxJson';
import { toSyntaxJson } from '../SyntaxJson';

const application = parse(`numbers exact
screen List
  action Retry for item
    presentation placement "row-menu"
    presentation extension "future"`).value;
const actionOf = (syntax: ApplicationSyntax) => syntax.screens![0].directives[0] as ScreenActionSyntax;
const action = actionOf(application);
const wire = () => JSON.parse(JSON.stringify(toSyntaxJson(application))) as { screens: { directives: { presentation?: unknown }[] }[] };
const value = (name: unknown, value: unknown) => ({ kind: 'PresentationValueSyntax', name, value });
const withAction = (replacement: ScreenActionSyntax): ApplicationSyntax => ({ ...application, screens: [{ ...application.screens![0], directives: [replacement] }] });
const duplicateAction: ScreenActionSyntax = { ...action, presentation: [action.presentation![0], action.presentation![0]] };

describe('when preserving action presentation', () => {
    it('should round trip values through AST JSON', () => expect(toSyntaxJson(actionOf(decodeExactSyntaxJson(JSON.stringify(toSyntaxJson(application))) as ApplicationSyntax))).toEqual(toSyntaxJson(action)));
    it('should omit an empty presentation', () => expect(toSyntaxJson({ ...action, presentation: [] } as ScreenActionSyntax)).not.toHaveProperty('presentation'));
    it('should accept an older action without presentation', () => {
        const old = wire();
        delete old.screens[0].directives[0].presentation;
        expect(actionOf(decodeExactSyntaxJson(JSON.stringify(old)) as ApplicationSyntax).presentation ?? []).toEqual([]);
    });
    it('should keep empty presentation structurally equivalent to omission', () => {
        const { presentation: omitted, ...old } = action;
        expect(omitted).toBeDefined();
        expect(toSyntaxJson(old)).toEqual(toSyntaxJson({ ...old, presentation: [] } as ScreenActionSyntax));
    });
    for (const presentation of [null, {}, 'row-menu', [null], [value('', 'row-menu')], [value('placement', '')], [value('placement', ' ')], [value('placement', null)], [value('placement', 42)], [value('placement', 'row-menu'), value('placement', 'other')]]) {
        it(`should reject invalid wire shapes: ${JSON.stringify(presentation)}`, () => {
            const invalid = wire();
            invalid.screens[0].directives[0].presentation = presentation;
            expect(() => decodeExactSyntaxJson(JSON.stringify(invalid))).toThrow(InvalidSyntaxJson);
        });
    }
    for (const subject of [null, 'Screen']) {
        it(`should serialize but reject reading typed presentation without item scope: ${subject}`, () => {
            const serialized = toSyntaxJson(withAction({ ...action, subject } as ScreenActionSyntax));
            expect(() => decodeExactSyntaxJson(JSON.stringify(serialized))).toThrow(InvalidSyntaxJson);
        });
    }
    it('should serialize duplicate typed keys without throwing', () => expect(() => toSyntaxJson(duplicateAction)).not.toThrow());
    it('should reject duplicate typed keys when reading', () => {
        const serialized = toSyntaxJson(withAction(duplicateAction));
        expect(() => decodeExactSyntaxJson(JSON.stringify(serialized))).toThrow(InvalidSyntaxJson);
    });
    it('should serialize an erroneous partial document alongside its diagnostic', () => {
        const partial = parse('numbers exact\nscreen Broken\n  action Retry for screen\n    presentation placement "row-menu"');
        expect(partial.diagnostics.some(diagnostic => diagnostic.code === 'PLAY1166')).toBe(true);
        expect(() => toSyntaxJson(partial.value)).not.toThrow();
    });
});

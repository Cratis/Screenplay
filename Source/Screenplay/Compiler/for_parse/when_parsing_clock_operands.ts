// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const prefix = 'module M\n  feature F\n    slice StateChange S\n      command Submit\n        value String\n';
const source = 'time zone configurable default "Europe/Oslo"\n' + prefix + '        calendar from $context.zone\n        produces when date of occurred >= date of now - 7 days\n          Recorded\n      specification Case\n        given time zone "Europe/Oslo"\n';

describe('when parsing clock operands', () => {
    it('should preserve every clock preview kind without authoring diagnostics', () => {
        const preview = readFileSync(new URL('../../../../Documentation/screenplay/fixtures/clock-preview.play', import.meta.url), 'utf8');
        const result = parse(preview);
        expect(result.diagnostics).toEqual([]);
        class Walker extends ScreenplaySyntaxWalker {
            kinds = new Set<string>();
            override visitNode(node: SyntaxNode): void { this.kinds.add(node.kind); }
        }
        const walker = new Walker();
        walker.visitApplication(result.value);
        for (const kind of ['TimeZoneSyntax', 'CommandCalendarSyntax', 'SpecificationTimeZoneSyntax', 'ClockOffsetExpressionSyntax', 'DateOfExpressionSyntax']) {
            expect(walker.kinds.has(kind)).toBe(true);
        }
    });

    it('should parse and walk every syntax-only node', () => {
        const result = parse(source);
        expect(result.diagnostics).toEqual([]);
        expect(result.value.timeZone).toMatchObject({ id: 'Europe/Oslo', configurable: true });
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(command.calendar).toMatchObject({ contextMember: 'zone' });
        expect(command.produces[0].when).toMatchObject({
            kind: 'ComparisonConditionSyntax',
            left: 'date of occurred',
            leftExpression: { kind: 'DateOfExpressionSyntax' },
            right: { kind: 'DateOfExpressionSyntax' }
        });
        class Walker extends ScreenplaySyntaxWalker {
            nodes: SyntaxNode[] = [];
            override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
        }
        const walker = new Walker();
        walker.visitApplication(result.value);
        for (const kind of ['TimeZoneSyntax', 'CommandCalendarSyntax', 'SpecificationTimeZoneSyntax', 'ClockOffsetExpressionSyntax']) {
            expect(walker.nodes.filter(node => node.kind === kind)).toHaveLength(1);
        }
        expect(walker.nodes.filter(node => node.kind === 'DateOfExpressionSyntax')).toHaveLength(2);
    });

    it.each([
        ['time zone "A"\ntime zone "B"', 'PLAY0812'],
        ['time zone Europe/Oslo', 'PLAY0812'],
        [prefix + '        calendar "A"\n        calendar "B"', 'PLAY0813'],
        [prefix + '        calendar from $context.', 'PLAY0813'],
        [prefix + '      specification Case\n        given time zone "A"\n        given time zone "B"', 'PLAY0814'],
        [prefix + '      specification Case\n        given time zone A', 'PLAY0814'],
        [prefix + '        produces when dueDate < today - 7 weeks\n          Recorded', 'PLAY0158'],
    ])('should diagnose invalid clock syntax %s', (text, code) => {
        expect(parse(text).diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
    });

    it('should preserve contextual properties and legacy JSON', () => {
        const result = parse(prefix + '        calendar String\n        produces when dueDate >= today\n          Recorded\n');
        expect(result.diagnostics).toEqual([]);
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(command.calendar).toBeUndefined();
        expect(command.properties.map(property => property.name)).toContain('calendar');
        const json = JSON.stringify(toSyntaxJson(result.value));
        for (const member of ['leftExpression', 'givenTimeZone', 'contextMember', 'configurable']) expect(json).not.toContain(member);
    });
});

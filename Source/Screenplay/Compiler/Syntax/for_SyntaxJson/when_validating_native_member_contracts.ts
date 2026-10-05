// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { decodeExactSyntaxJson } from '../StrictSyntaxJson';
import { InvalidSyntaxJson } from '../InvalidSyntaxJson';
import { SyntaxNode } from '../SyntaxNode';
import { syntaxMemberNames } from '../SyntaxMemberContracts';
import { toSyntaxJson } from '../SyntaxJson';
import { syntaxDefinitions } from '../SyntaxDefinitions';

const source = readFileSync(resolve(__dirname, '../../Conformance/exact-numbers.play'), 'utf8');
const wire = JSON.stringify(toSyntaxJson(parse(source).value));
const root = JSON.parse(wire) as Record<string, unknown>;
const template = decodeExactSyntaxJson(wire) as unknown as Record<string, unknown>;
const definitions = syntaxDefinitions as unknown as Record<string, { required?: string[]; properties: Record<string, unknown> }>;
const cases: { path: (string | number)[]; member: string; description: string }[] = [];
function visit(value: unknown, path: (string | number)[]): void {
    if (Array.isArray(value)) { value.forEach((item, index) => visit(item, [...path, index])); return; }
    if (typeof value !== 'object' || value === null) return;
    const node = value as Record<string, unknown>;
    if (typeof node.kind === 'string') {
        for (const member of definitions[node.kind].required ?? []) cases.push({ path, member, description: `${path.join('.')}.${node.kind}.${member}` });
    }
    for (const [name, member] of Object.entries(node)) visit(member, [...path, name]);
}
visit(root, []);
function target(value: Record<string, unknown>, path: (string | number)[]): Record<string, unknown> {
    let result: unknown = value;
    for (const key of path) result = (result as Record<string | number, unknown>)[key];
    return result as Record<string, unknown>;
}

describe('when validating native member contracts', () => {
    it('should refuse unknown native contract lookups rather than default to an untyped shape', () => {
        expect(() => syntaxMemberNames('UnknownSyntax')).toThrow(InvalidSyntaxJson);
    });
    it.each(cases)('should reject omission of required $description on both public transport boundaries', ({ path, member }) => {
        const json = structuredClone(root);
        const tree = structuredClone(template);
        delete target(json, path)[member];
        delete target(tree, path)[member];
        expect(() => decodeExactSyntaxJson(JSON.stringify(json))).toThrow(InvalidSyntaxJson);
        expect(() => toSyntaxJson(tree as unknown as SyntaxNode)).toThrow(InvalidSyntaxJson);
    });
    it.each([
        ['generation', -1], ['generation', -0], ['generation', 4294967296], ['generation', 1.5],
        ['generation', '1'], ['name', null], ['name', false],
    ])('should retain primitive widths rather than serialize %s=%s as a different valid value', (member, value) => {
        const text = JSON.stringify(toSyntaxJson(parse('numbers exact\nmodule M\n  feature F\n    slice StateChange S\n      event Added\n').value));
        const tree = decodeExactSyntaxJson(text) as unknown as { modules: { features: { slices: { events: Record<string, unknown>[] }[] }[] }[] };
        tree.modules[0].features[0].slices[0].events[0][member] = value;
        expect(() => toSyntaxJson(tree as unknown as SyntaxNode)).toThrow(InvalidSyntaxJson);
    });
    it('should retain nullable/default omissions and business signed zero in both modes', () => {
        const exact = decodeExactSyntaxJson('{"kind":"ApplicationSyntax","sourceOptions":{"numericMode":"exact"},"seeds":null}');
        const value = exact as unknown as Record<string, unknown>;
        delete value.modules;
        value.seeds = null;
        expect(() => decodeExactSyntaxJson(JSON.stringify(toSyntaxJson(exact)))).not.toThrow();
        const body = 'seed\n  for "global"\n    Added\n      amount = -0\n';
        const legacy = parse(body).value;
        const literal = legacy.seeds![0].groups[0].events[0].properties[0].source;
        expect(literal.kind === 'LiteralExpressionSyntax' && Object.is(literal.value, -0)).toBe(true);
        expect(JSON.stringify(toSyntaxJson(parse(`numbers exact\n${body}`).value))).toContain('"value":"0"');
        expect(JSON.stringify(toSyntaxJson(legacy))).not.toContain('ExactNumber');
    });
    it('should preserve partial Legacy writer data without a new structural rejection', () => {
        expect(() => toSyntaxJson({ kind: 'TypeRefSyntax', location: { line: 1, column: 1 } } as SyntaxNode)).not.toThrow();
    });
});

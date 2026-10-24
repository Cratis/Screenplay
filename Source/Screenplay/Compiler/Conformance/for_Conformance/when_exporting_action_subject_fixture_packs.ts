// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { resolveActionSubjects } from '../../ScreenActionSubjects';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

const directory = fileURLToPath(new URL('../action-subjects/', import.meta.url));
const compiled = parse(readFileSync(`${directory}source.play`, 'utf8'));
const raw = toSyntaxJson(compiled.value);
const effective = resolveActionSubjects(compiled.value).value;
const relevant = new Set(['PLAY0319', 'PLAY0345', 'PLAY0346', 'PLAY0815', 'PLAY0816', 'PLAY0817', 'PLAY0818', 'PLAY0819']);

interface LocatedNode {
    path: string;
    kind: string;
    location: { line: number; column: number };
}

function locations(value: unknown, path: string): LocatedNode[] {
    if (Array.isArray(value)) return value.flatMap((child, index) => locations(child, `${path}[${index}]`));
    if (value === null || typeof value !== 'object') return [];
    const node = value as Record<string, unknown>;
    const location = node.location as LocatedNode['location'] | undefined;
    const own = typeof node.kind === 'string' && location !== undefined ? [{ path, kind: node.kind, location: { line: location.line, column: location.column } }] : [];
    return [...own, ...Object.entries(node).filter(([name]) => name !== 'location').flatMap(([name, child]) => locations(child, `${path}.${name}`))];
}

const exports = {
    'raw.syntax': raw,
    'effective.syntax': toSyntaxJson(effective),
    diagnostics: compiled.diagnostics.filter(diagnostic => relevant.has(diagnostic.code)).map(diagnostic => ({ code: diagnostic.code, severity: diagnostic.severity, message: diagnostic.message, location: { line: diagnostic.location.line, column: diagnostic.location.column } })),
    context: { source: 'source.play', application: raw, locations: locations(compiled.value, '$').sort((left, right) => left.path < right.path ? -1 : left.path > right.path ? 1 : 0) },
};

const regenerate = process.env.SCREENPLAY_REGENERATE_ACTION_SUBJECT_FIXTURES === '1';
if (regenerate) {
    for (const [name, value] of Object.entries(exports)) writeFileSync(`${directory}typescript.${name}.json`, `${JSON.stringify(value, null, 2)}\n`);
}

describe('when exporting action subject fixture packs', () => {
    it('should preserve authored syntax', () => expect(toSyntaxJson(compiled.value)).toEqual(raw));
    it('should be idempotent', () => expect(toSyntaxJson(resolveActionSubjects(effective).value)).toEqual(toSyntaxJson(effective)));
    for (const [name, value] of Object.entries(exports)) {
        it(`should retain the typescript ${name} fixture`, () => {
            expect(regenerate, 'Fixture pack regenerated; review and rerun without SCREENPLAY_REGENERATE_ACTION_SUBJECT_FIXTURES').toBe(false);
            expect(value).toEqual(JSON.parse(readFileSync(`${directory}typescript.${name}.json`, 'utf8')));
        });
    }
    it('should agree with native diagnostics', () => {
        const native = JSON.parse(readFileSync(`${directory}native.diagnostics.json`, 'utf8')) as typeof exports.diagnostics;
        const order = (value: typeof exports.diagnostics) => [...value].sort((left, right) => left.location.line - right.location.line || left.code.localeCompare(right.code));
        expect(order(exports.diagnostics)).toEqual(order(native));
    });
});

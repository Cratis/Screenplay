// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { resolveActionSubjects } from '../../ScreenActionSubjects';
import { ScreenActionSyntax } from '../../Syntax/Screens';
import { ScreenplaySyntaxWalker } from '../../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../../Syntax/SyntaxNode';
import { toSyntaxJson } from '../../Syntax/SyntaxJson';

const directory = fileURLToPath(new URL('../action-subjects/', import.meta.url));
const compiled = parse(readFileSync(`${directory}source.play`, 'utf8'));
const raw = toSyntaxJson(compiled.value);
const effective = resolveActionSubjects(compiled.value).value;
const relevant = new Set(['PLAY0319', 'PLAY0345', 'PLAY0346', 'PLAY0817', 'PLAY0818', 'PLAY0819', 'PLAY0820', 'PLAY0821']);

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

interface SubjectScope { screen: string | null; slice: string | null; data: readonly string[] }

function subjects(value: unknown, path: string, inherited: SubjectScope): unknown[] {
    if (Array.isArray(value)) return value.flatMap((child, index) => subjects(child, `${path}[${index}]`, inherited));
    if (value === null || typeof value !== 'object') return [];
    const node = value as Record<string, unknown>;
    let scope = subjectScope(node, path, inherited);
    if (Array.isArray(node.directives)) {
        const local = node.directives.flatMap((child: { kind: string }, index) => child.kind === 'ScreenDataSyntax' ? [`${path}.directives[${index}]`] : []);
        if (local.length > 0) scope = { ...scope, data: local };
    }
    const own = ['ScreenActionSyntax', 'ScreenGuardedActionSyntax', 'ToolbarItemSyntax'].includes(String(node.kind)) ? [{ path, ...scope }] : [];
    return [...own, ...Object.entries(node).flatMap(([name, child]) => subjects(child, `${path}.${name}`, scope))];
}

function subjectScope(node: Record<string, unknown>, path: string, inherited: SubjectScope): SubjectScope {
    switch (node.kind) {
        case 'SliceSyntax': return { ...inherited, slice: path };
        case 'ScreenSyntax': return { ...inherited, screen: path, data: [] };
        case 'ScreenContributionSyntax': return { ...inherited, data: [] };
        default: return inherited;
    }
}

const exports = {
    'raw.syntax': raw,
    'effective.syntax': toSyntaxJson(effective),
    manifest: {
        protocol: 'Cratis.ActionSubjects/1', authored: 'source.play', rawSyntaxJson: 'typescript.raw.syntax.json', effectiveSyntaxJson: 'typescript.effective.syntax.json', diagnostics: 'typescript.diagnostics.json', context: 'typescript.context.json',
        cases: compiled.value.modules[0].features[0].slices[0].screens.map((screen, index) => ({ name: screen.name, focusPath: `$.modules[0].features[0].slices[0].screens[${index}]` })),
    },
    diagnostics: compiled.diagnostics.filter(diagnostic => relevant.has(diagnostic.code)).map(diagnostic => ({ code: diagnostic.code, severity: diagnostic.severity, message: diagnostic.message, location: { line: diagnostic.location.line, column: diagnostic.location.column } })),
    context: { source: 'source.play', application: raw, subjects: subjects(raw, '$', { screen: null, slice: null, data: [] }), locations: locations(compiled.value, '$').sort((left, right) => left.path < right.path ? -1 : left.path > right.path ? 1 : 0) },
};

const regenerate = process.env.SCREENPLAY_REGENERATE_ACTION_SUBJECT_FIXTURES === '1';
if (regenerate) {
    for (const [name, value] of Object.entries(exports)) writeFileSync(`${directory}typescript.${name}.json`, `${JSON.stringify(value, null, 2)}\n`);
}

class ActionCollector extends ScreenplaySyntaxWalker {
    readonly actions: ScreenActionSyntax[] = [];
    override visitNode(node: SyntaxNode): void { if (node.kind === 'ScreenActionSyntax') this.actions.push(node as ScreenActionSyntax); }
}

function action(name: string): ScreenActionSyntax {
    const screen = effective.modules[0].features[0].slices[0].screens.find(screen => screen.name === name)!;
    const walker = new ActionCollector();
    walker.visitScreen(screen);
    expect(walker.actions).toHaveLength(1);
    return walker.actions[0];
}

describe('when exporting action subject fixture packs', () => {
    for (const [screen, binding] of [['InferredItem', 'item.rowId'], ['SameName', 'item.itemId'], ['CaseMismatch', 'item.rowId'], ['QualifiedDeclarations', 'item.rowId'], ['SlotScope', 'item.rowId'], ['OutletScope', 'item.itemId']]) {
        it(`should resolve nominal identity in ${screen}`, () => {
            expect(action(screen).subject).toBe('Item');
            expect(action(screen).inputs!.find(input => input.name === 'itemId')!.binding).toBe(binding);
        });
    }
    for (const screen of ['ForeignKey', 'OptionalIdentifier', 'GeneratedIdentifier', 'PrimitiveIdentifier', 'AmbiguousInferred', 'MissingKey', 'UnresolvedDeclaration']) {
        it(`should not invent identity in ${screen}`, () => {
            expect(action(screen).subject ?? 'Screen').toBe('Screen');
            expect(action(screen).inputs ?? []).toEqual([]);
        });
    }
    it('should preserve authored syntax', () => expect(toSyntaxJson(compiled.value)).toEqual(raw));
    it('should retain authored source locations', () => {
        const resolved = new Map(locations(effective, '$').map(entry => [entry.path, entry.location]));
        for (const entry of locations(compiled.value, '$')) expect(resolved.get(entry.path)).toEqual(entry.location);
    });
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

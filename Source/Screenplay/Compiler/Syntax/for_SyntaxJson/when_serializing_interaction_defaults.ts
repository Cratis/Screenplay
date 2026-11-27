// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { sourceLocation } from '../../Diagnostics/SourceLocation';
import { parse } from '../../ScreenplayCompiler';
import { SyntaxJsonValue, toCompleteSyntaxJson, toSyntaxJson } from '../SyntaxJson';
import { SyntaxNode } from '../SyntaxNode';

interface OmissionCase {
    readonly kind: string;
    readonly defaults: Record<string, unknown>;
    readonly authored: Record<string, unknown>;
    readonly retained: Record<string, unknown>;
}

const binding = parse('behavior Choice\n  on click\n    when item.enabled == true\n      navigate back\n    otherwise\n      close dialog').value.behaviors![0].bindings[0];
const offeredOutlet = node('OutletSyntax', { name: 'tools', syntaxKind: 'Actions', mode: 'Unspecified', items: [] });
const slotContent = node('TemplateSlotContentSyntax', { slot: 'content', directives: [] });
const templateDefaults = { displayName: null, description: null, restrictsScopes: false, scopes: [], sliceKinds: [], content: [], outlets: [] };
const templateAuthored = { displayName: 'Editor', description: 'Edit items', restrictsScopes: true, scopes: ['module'], sliceKinds: ['StateView'], content: [slotContent], outlets: [offeredOutlet] };
const cases: readonly OmissionCase[] = [
    { kind: 'InteractionBindingSyntax', defaults: { alternatives: [], otherwise: null }, authored: { alternatives: binding.alternatives, otherwise: binding.otherwise }, retained: { condition: null } },
    { kind: 'LayoutSyntax', defaults: { restrictsScopes: false, scopes: [], sliceKinds: [], outlets: [] }, authored: { restrictsScopes: true, scopes: ['module'], sliceKinds: ['StateChange'], outlets: [offeredOutlet] }, retained: { category: null } },
    { kind: 'ScreenTemplateSyntax', defaults: templateDefaults, authored: templateAuthored, retained: { category: null, fitsSlot: null } },
    { kind: 'DialogTemplateSyntax', defaults: templateDefaults, authored: templateAuthored, retained: { category: null } },
    { kind: 'SlotSyntax', defaults: { role: null, routed: false, routedBy: null, primary: false, optional: false }, authored: { role: 'Page', routed: true, routedBy: 'selected', primary: true, optional: true }, retained: { name: 'content' } },
];
const writers = [{ name: 'ordinary transport', write: toSyntaxJson }, { name: 'complete transport', write: toCompleteSyntaxJson }];

function node(kind: string, members: Record<string, unknown>): SyntaxNode {
    return { kind, location: sourceLocation(1, 1), ...members };
}

function canonicalValue(value: unknown, write: (syntax: SyntaxNode) => SyntaxJsonValue): unknown {
    if (Array.isArray(value)) return value.map(item => canonicalValue(item, write));
    return value !== null && typeof value === 'object' ? write(value as SyntaxNode) : value;
}

for (const writer of writers) {
    describe(`when serializing interaction defaults in ${writer.name}`, () => {
        for (const testCase of cases) {
            it(`should omit only native default members of ${testCase.kind}`, () => {
                const result = writer.write(node(testCase.kind, { ...testCase.defaults, ...testCase.retained })) as Record<string, SyntaxJsonValue>;
                expect(Object.keys(result).sort()).toEqual(['kind', ...Object.keys(testCase.retained)].sort());
                for (const [name, value] of Object.entries(testCase.retained)) expect(result[name]).toEqual(value);
            });

            it(`should preserve every non-default payload of ${testCase.kind}`, () => {
                const result = writer.write(node(testCase.kind, testCase.authored)) as Record<string, SyntaxJsonValue>;
                expect(Object.keys(result).sort()).toEqual(['kind', ...Object.keys(testCase.authored)].sort());
                for (const [name, value] of Object.entries(testCase.authored)) {
                    // Nested syntax loses locations on wire, but no authored payload may disappear.
                    expect(result[name]).toEqual(canonicalValue(value, writer.write));
                }
            });
        }

        for (const kind of ['LayoutSyntax', 'ScreenTemplateSyntax', 'DialogTemplateSyntax']) {
            it(`should match native empty-collection omission for null collections on ${kind}`, () => {
                const result = writer.write(node(kind, { scopes: null, sliceKinds: null, outlets: null })) as Record<string, SyntaxJsonValue>;
                expect(Object.keys(result)).toEqual(['kind']);
            });
        }
    });
}

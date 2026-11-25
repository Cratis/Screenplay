// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'vitest';
import { syntaxDefinitions } from '../../../Syntax/SyntaxDefinitions';
import { SyntaxJsonValue, toSyntaxJson } from '../../../Syntax/SyntaxJson';
import { decodeExactSyntaxJson } from '../../../Syntax/StrictSyntaxJson';

interface MemberContract { readonly default?: unknown; readonly [member: string]: unknown }
const definitions: Readonly<Record<string, { readonly properties?: Readonly<Record<string, MemberContract>> }>> = syntaxDefinitions;

/** Every authored member survives; newly materialized members must be explicit native defaults. */
export function assertExactSyntaxRoundTrip(source: SyntaxJsonValue): void {
    const restored = toSyntaxJson(decodeExactSyntaxJson(JSON.stringify(source)));
    assertDefaultExpansion(source, restored);
    const canonical = JSON.stringify(restored);
    JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(canonical))).should.equal(canonical);
}

function assertDefaultExpansion(source: SyntaxJsonValue, restored: SyntaxJsonValue): void {
    if (Array.isArray(source)) {
        expect(Array.isArray(restored)).toBe(true);
        expect(restored).toHaveLength(source.length);
        source.forEach((member, index) => assertDefaultExpansion(member, (restored as SyntaxJsonValue[])[index]));
    } else if (source !== null && typeof source === 'object') {
        expect(restored).not.toBeNull();
        expect(typeof restored).toBe('object');
        expect(Array.isArray(restored)).toBe(false);
        assertObjectExpansion(source as Record<string, SyntaxJsonValue>, restored as Record<string, SyntaxJsonValue>);
    } else expect(restored).toEqual(source);
}

function assertObjectExpansion(source: Record<string, SyntaxJsonValue>, restored: Record<string, SyntaxJsonValue>): void {
    for (const [name, value] of Object.entries(source)) {
        expect(Object.hasOwn(restored, name)).toBe(true);
        assertDefaultExpansion(value, restored[name]);
    }
    const properties = typeof source.kind === 'string' ? definitions[source.kind]?.properties : undefined;
    for (const name of Object.keys(restored).filter(name => !Object.hasOwn(source, name))) {
        const contract = properties?.[name];
        expect(contract).toHaveProperty('default');
        expect(restored[name]).toEqual(contract?.default);
    }
}

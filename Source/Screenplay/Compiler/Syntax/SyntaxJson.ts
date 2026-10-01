// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

export type SyntaxJsonValue = string | number | boolean | null | SyntaxJsonValue[] | { [member: string]: SyntaxJsonValue };

const isNode = (value: unknown): value is SyntaxNode =>
    typeof value === 'object' && value !== null && typeof (value as { kind?: unknown }).kind === 'string';

// The canonical JSON form of a syntax tree, the same form the C# SyntaxJson writes: 'kind' first, then the
// members in ordinal order, with source locations left out. Because a node only carries the members this
// compiler models, the result is the C# form narrowed to those members.
export function toSyntaxJson(node: SyntaxNode): SyntaxJsonValue {
    return write(node);
}

function write(value: unknown): SyntaxJsonValue {
    if (Array.isArray(value)) {
        return value.map(write);
    }
    if (isNode(value)) {
        const result: { [member: string]: SyntaxJsonValue } = { kind: value.kind };
        const members = Object.keys(value).filter(member => member !== 'kind' && member !== 'location').sort(ordinal);
        for (const member of members) {
            result[member] = write((value as unknown as Record<string, unknown>)[member]);
        }
        return result;
    }
    if (value === undefined) {
        return null;
    }
    return value as SyntaxJsonValue;
}

function ordinal(left: string, right: string): number {
    return left < right ? -1 : left > right ? 1 : 0;
}

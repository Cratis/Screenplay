// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { decodeExactSyntaxJson, InvalidSyntaxJson } from '../StrictSyntaxJson';
import { toSyntaxJson } from '../SyntaxJson';

const source = [
    'numbers exact',
    'seed',
    '    for "g"',
    '        Added',
    '            amount = [1, 2]',
    'module M',
    '    feature F',
    '        slice StateChange S',
    '            event Added',
    '                amount Decimal',
    ''
].join('\n');
const parsed = (): Record<string, unknown> => parse(source).value as unknown as Record<string, unknown>;
const sparse = (items: unknown[]): unknown[] => { const result = new Array(items.length); return result; };
const find = (value: unknown, kind: string): Record<string, unknown> | undefined => {
    if (typeof value !== 'object' || value === null) return undefined;
    if ((value as { kind?: unknown }).kind === kind) return value as Record<string, unknown>;
    for (const member of Object.values(value)) { const found = find(member, kind); if (found !== undefined) return found; }
    return undefined;
};

describe('when writing sparse exact collections', () => {
    it('should accept the dense tree', () => {
        expect(() => decodeExactSyntaxJson(JSON.stringify(toSyntaxJson(parsed() as never)))).not.toThrow();
    });

    it('should refuse a hole in a declaration collection', () => {
        const root = parsed();
        root.modules = sparse(root.modules as unknown[]);
        expect(() => toSyntaxJson(root as never)).toThrow(InvalidSyntaxJson);
    });

    it('should refuse a hole in an expression collection', () => {
        const root = parsed();
        const list = find(root, 'ListExpressionSyntax')!;
        list.items = sparse(list.items as unknown[]);
        expect(() => toSyntaxJson(root as never)).toThrow(InvalidSyntaxJson);
    });

    it('should refuse an invalid element in a collection', () => {
        const root = parsed();
        root.modules = [null];
        expect(() => toSyntaxJson(root as never)).toThrow(InvalidSyntaxJson);
    });
});

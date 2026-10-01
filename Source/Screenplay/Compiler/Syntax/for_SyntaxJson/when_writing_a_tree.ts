// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { SyntaxJsonValue, toSyntaxJson } from '../SyntaxJson';
import { SyntaxNode } from '../SyntaxNode';

interface Example extends SyntaxNode {
    readonly zeta: string;
    readonly alpha: readonly SyntaxNode[];
    readonly missing?: string;
}

describe('when writing a tree', () => {
    let json: Record<string, SyntaxJsonValue>;

    beforeEach(() => {
        const node: Example = {
            kind: 'ExampleSyntax',
            zeta: 'last',
            alpha: [{ kind: 'ChildSyntax', location: { line: 2, column: 3 } }],
            missing: undefined,
            location: { line: 1, column: 1, path: 'a.play' },
        };
        json = toSyntaxJson(node) as Record<string, SyntaxJsonValue>;
    });

    it('should put the kind first and the members in ordinal order', () => {
        Object.keys(json).should.deep.equal(['kind', 'alpha', 'missing', 'zeta']);
    });

    it('should leave source locations out, nested ones too', () => {
        JSON.stringify(json).should.not.contain('line');
    });

    it('should write an absent value as null', () => {
        (json.missing === null).should.be.true;
    });
});

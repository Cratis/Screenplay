// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';
import { SyntaxJsonValue, toSyntaxJson } from '../../Syntax/SyntaxJson';

describe('when serializing a specification document with examples', () => {
    let wire: Record<string, SyntaxJsonValue>[];

    beforeEach(() => {
        const result = parseSpecificationSource('numbers exact\nspecification First\nexample Sample : C\n  amount = 1\nspecification Second', 'wire.play');
        wire = result.value.map(root => toSyntaxJson(root) as Record<string, SyntaxJsonValue>);
    });

    it('should preserve root kinds, names and order', () => wire.map(({ kind, name }) => ({ kind, name })).should.deep.equal([
        { kind: 'SpecificationSyntax', name: 'First' }, { kind: 'SpecificationSyntax', name: 'Second' },
    ]));
    it('should preserve exact numeric mode on every wire root', () => wire.map(root => root.sourceOptions).should.deep.equal([{ numericMode: 'exact' }, { numericMode: 'exact' }]));
    it('should omit physical locations from every wire root', () => wire.forEach(root => Object.hasOwn(root, 'location').should.equal(false)));
    it('should retain the examples key and its full syntax contents on every root', () => {
        for (const root of wire) {
            Object.hasOwn(root, 'examples').should.equal(true);
            root.examples!.should.deep.equal([{
                kind: 'SpecificationExampleSyntax', name: 'Sample', type: 'C', for: null, generatedValues: [], description: null,
                values: [{ kind: 'PropertyMappingSyntax', property: 'amount', source: { kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: '1' } } }],
            }]);
        }
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { expressionText } from '../expressionText';

const source = readFileSync(new URL('../../../Compiler/Conformance/specification-tables.play', import.meta.url), 'utf8');

describe('when mapping specification cases', () => {
    it('retains authored case references in source descriptions', () => {
        expressionText({ kind: 'CaseValueExpressionSyntax', parameter: 'amount', location: { line: 1, column: 1 } }).should.equal('case.amount');
    });
    it('should draw independent cards named from case provenance in case order', () => {
        const document = toEventModelDocument(parse(source).value, 'Cases');
        const specifications = document.collections[0].modules[0].features[0].slices[0].specifications;
        specifications.map(specification => specification.name).should.deep.equal(['Recording — Small', 'Recording — amountTooLow', 'Recording — Large', 'Rejecting — Empty']);
        specifications[0].when!.values.should.deep.equal({ amount: 10 });
        specifications[1].when!.values.should.deep.equal({ amount: 0 });
        specifications[2].when!.values.should.deep.equal({ amount: 100 });
        new Set(specifications.map(specification => specification.id)).size.should.equal(4);
    });
});

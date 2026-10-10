// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { SpecificationErrorSyntax } from '../Specifications';
import { toCompleteSyntaxJson, toSyntaxJson } from '../SyntaxJson';

describe('when omitting an absent error case value', () => {
    it('should preserve the named error without serializing a null case binding', () => {
        const error: SpecificationErrorSyntax = { kind: 'SpecificationErrorSyntax', name: 'Rejected', caseValue: null, location: { line: 1, column: 1 } };
        expect(toSyntaxJson(error)).toEqual({ kind: 'SpecificationErrorSyntax', name: 'Rejected' });
        expect(toCompleteSyntaxJson(error)).toEqual({ kind: 'SpecificationErrorSyntax', name: 'Rejected' });
    });
});

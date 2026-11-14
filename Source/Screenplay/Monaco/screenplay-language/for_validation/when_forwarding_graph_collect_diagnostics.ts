// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import vectors from '../../../Compiler/Conformance/diagnostics.json';
import { diagnosticCodes } from '../diagnostic-codes';
import { validateLines } from '../validation';

const collectCodes = new Set([diagnosticCodes.invalidGraphCollect, diagnosticCodes.graphCollectTargetMismatch, diagnosticCodes.nestedGraphCollect, diagnosticCodes.graphCollectQueryShape] as string[]);
const cases = vectors.cases.filter(vector => vector.diagnostics.some(diagnostic => collectCodes.has(diagnostic.split('@')[0])));

describe('when forwarding graph collect diagnostics', () => {
    it('should find a vector for every collect code', () => new Set(cases.flatMap(vector => vector.diagnostics.map(diagnostic => diagnostic.split('@')[0]).filter(code => collectCodes.has(code)))).size.should.equal(4));
    it.each(cases)('should report the collect diagnostics of $name', vector => {
        const expected = vector.diagnostics.filter(diagnostic => collectCodes.has(diagnostic.split('@')[0]));
        const reported = validateLines(vector.source).filter(issue => collectCodes.has(issue.code ?? '')).map(issue => `${issue.code}@${issue.line + 1}`);
        reported.should.deep.equal(expected);
    });
});

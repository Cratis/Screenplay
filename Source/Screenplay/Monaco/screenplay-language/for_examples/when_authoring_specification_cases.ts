// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { exampleCompletions } from '../example-authoring';
import { caseHover } from '../case-authoring';
import { responseAnalysis } from '../response-analysis';

const lines = ['specification Recording', '  parameter amount Int', '  case Small amount = 10', '  case Large amount = 100', '  when Record', '    amount = case.amount', '  then Recorded amount = case.amount'];

describe('when authoring specification cases', () => {
    it('completes parameter references in step values', () => {
        expect(exampleCompletions(lines, 5, '    amount = case.')?.map(entry => entry.label)).toEqual(['case.amount']);
    });
    it('shows case provenance and concrete values in hover', () => {
        expect(caseHover(lines, 5, 13, 24)).toContain('Case origin in **Recording**');
        expect(caseHover(lines, 5, 13, 24)).toContain('Small: `10`');
        expect(caseHover(lines, 5, 13, 24)).toContain('Large: `100`');
    });
    it('reports undeclared case parameters', () => {
        expect(responseAnalysis(lines.map(line => line.replace('case.amount', 'case.other'))).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0577');
    });
    it('does not offer references outside specifications or in case values', () => {
        expect(exampleCompletions([...lines, 'command Other', '  amount = case.'], 8, '  amount = case.')).toBeNull();
        expect(exampleCompletions(lines, 2, '  case Small amount = case.')).toBeNull();
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';
import { validateMatchPattern } from '../PortableMatchPattern';

interface BudgetCase { pattern: string; expandedSize: number; accepted: boolean; offset?: number }
const shared = JSON.parse(readFileSync(join(__dirname, '../../Conformance/portable-match-differential.json'), 'utf8')) as { budgetCases: BudgetCase[]; patterns: { pattern: string }[] };

describe('when enforcing expanded nodes including nullable choices', () => {
    it('should cover boundaries nesting assertions and surrogate ranges', () => {
        shared.budgetCases.length.should.be.greaterThan(40);
    });
    for (const vector of shared.budgetCases) {
        it(`should classify the ${vector.expandedSize} node shape ${JSON.stringify(vector.pattern)}`, () => {
            const issue = validateMatchPattern(vector.pattern);
            (issue === undefined).should.equal(vector.accepted);
            if (issue !== undefined) {
                issue.invalid.should.be.false;
                issue.construct.should.equal('expanded pattern size');
                issue.offset.should.equal(vector.offset);
                const result = parse(`concept Code : String\n  validate\n    matches ${JSON.stringify(vector.pattern)}`);
                result.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.PatternOutsidePortableMatchDialect).should.be.true;
            }
        });
    }
    it('should admit every generated and corpus oracle pattern', () => {
        shared.patterns.length.should.be.greaterThan(1500);
        for (const { pattern } of shared.patterns) (validateMatchPattern(pattern) === undefined).should.be.true;
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ValidationRuleSyntax } from '../Syntax/Commands';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';

interface ChecksumRuleVectors {
    syntax: { directive: string; algorithm: string; message: string | null; severity: string }[];
    invalid: string[];
}
const vectors = JSON.parse(readFileSync(join(__dirname, '../Conformance/generation-checksum-rules.json'), 'utf8')) as ChecksumRuleVectors;

function source(directive: string): string {
    return `concept InvoiceRef : String\n  ${directive}\n`;
}

function rule(result: CompilationResult<ApplicationSyntax>): ValidationRuleSyntax {
    const validation = result.value.concepts[0].validations?.[0];
    if (validation?.kind !== 'DeclarativeValidateSyntax') throw new Error('Expected declarative checksum validation.');
    return validation.rules[0];
}

describe('when parsing checksum validation', () => {
    let parsed: CompilationResult<ApplicationSyntax>[];
    let refused: CompilationResult<ApplicationSyntax>[];
    let rules: ValidationRuleSyntax[];
    beforeEach(() => {
        parsed = vectors.syntax.map(vector => parse(source(vector.directive)));
        refused = vectors.invalid.map(directive => parse(source(directive)));
        rules = parsed.map(rule);
    });
    it('should check a nonempty syntax population', () => vectors.syntax.should.not.be.empty);
    it('should check a nonempty refusal population', () => vectors.invalid.should.not.be.empty);
    it('should parse both forms without diagnostics', () => parsed.flatMap(result => result.diagnostics).should.deep.equal([]));
    it('should use the appended checksum kind', () => rules.map(value => value.rule).should.deep.equal(vectors.syntax.map(() => 'Checksum')));
    it('should use the implied concept subject', () => rules.map(value => value.property).should.deep.equal(vectors.syntax.map(() => 'value')));
    it('should use a text algorithm operand', () => rules.map(value => value.value?.kind === 'LiteralExpressionSyntax' ? value.value.value : null).should.deep.equal(vectors.syntax.map(vector => vector.algorithm)));
    it('should preserve messages', () => rules.map(value => value.message).should.deep.equal(vectors.syntax.map(vector => vector.message)));
    it('should preserve severities', () => rules.map(value => value.severity).should.deep.equal(vectors.syntax.map(vector => vector.severity)));
    it('should refuse every invalid algorithm', () => refused.every(result => result.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.InvalidGenerationArgument)).should.be.true);
});

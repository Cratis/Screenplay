// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, it } from 'vitest';
import { checkPortableMatchPattern, parse, PortableMatchReplacement, PortableMatchStatus } from '../../index';
import { escapeLegacyTextRule } from '../LegacyTextRulePatterns';

interface Literal { value: string; v1: string; v2: string; codeUnits?: number[] }
const shared = JSON.parse(readFileSync(join(__dirname, '../../Conformance/legacy-text-rule-shapes.json'), 'utf8')) as { literals: Literal[]; refused: string[]; unchanged: string[] };

function text(literal: Literal, key: 'value' | 'v1' | 'v2'): string { return literal.codeUnits === undefined ? literal[key] : String.fromCharCode(...literal.codeUnits); }
function quote(value: string): string { return '"' + value.replace(/["\\\n\r\t]/g, character => JSON.stringify(character).slice(1, -1)) + '"'; }

describe('when recognizing Studio text rule shapes', () => {
    it('should inspect the complete shared population', () => { shared.literals.length.should.equal(16); shared.refused.length.should.equal(8); shared.unchanged.length.should.equal(6); });
    for (const vector of shared.literals) for (const v1 of [true, false]) {
        const value = text(vector, 'value');
        const literal = text(vector, v1 ? 'v1' : 'v2');
        const end = v1 ? '$' : '(?![\\s\\S])';
        const cases: [string, PortableMatchReplacement['kind']][] = [
            ['^(?![\\s\\S]*' + literal + ')', 'NotContains'], ['^(?!' + literal + end + ')', 'NotEqual'],
            ...v1 ? [] : [['^' + literal + end, 'Equal'], [literal + end, 'EndsWith']] as [string, PortableMatchReplacement['kind']][],
        ];
        it(`should reproduce the v${v1 ? 1 : 2} escaping of ${JSON.stringify(value)}`, () => { escapeLegacyTextRule(value, v1).should.equal(literal); });
        for (const [pattern, kind] of cases) it(`should deprecate ${JSON.stringify(pattern)} as ${kind}`, () => {
            const result = checkPortableMatchPattern(pattern);
            result.status.should.equal(PortableMatchStatus.Deprecated);
            result.code!.should.equal('PLAY0787');
            result.replacement!.should.deep.equal({ kind, value });
            const parsed = parse(`concept Ref : String\n  validate\n    matches ${quote(pattern)} severity warning message "Invalid reference"\n`);
            parsed.success.should.equal(true);
            parsed.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0787').length.should.equal(1);
            const validation = parsed.value.concepts[0].validations![0];
            if (validation.kind !== 'DeclarativeValidateSyntax') throw new Error('Expected declarative validation');
            validation.rules[0].rule.should.equal('Matches');
            validation.rules[0].severity.should.equal('Warning');
        });
    }
    for (const pattern of ['^(?![\\s\\S]*\\)', '^(?!\\q$)', '\\#(?![\\s\\S])']) it(`should leave a non-round-tripping literal refused: ${JSON.stringify(pattern)}`, () => {
        checkPortableMatchPattern(pattern).code!.should.equal('PLAY0699');
    });
    for (const pattern of shared.refused) it(`should refuse ${JSON.stringify(pattern)}`, () => { checkPortableMatchPattern(pattern).code!.should.equal('PLAY0699'); });
    for (const pattern of shared.unchanged) it(`should leave ${JSON.stringify(pattern)} unchanged`, () => { checkPortableMatchPattern(pattern).status.should.equal(PortableMatchStatus.Admitted); });
    for (const rule of ['contains', 'not contains', 'starts with', 'ends with']) it(`should parse first-class ${rule} on commands and concepts`, () => {
        const result = parse(`concept Ref : String\n  validate\n    ${rule} "A"\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        validate\n          value ${rule} "A"\n`);
        result.success.should.equal(true);
        const concept = result.value.concepts[0].validations![0];
        const command = result.value.modules[0].features[0].slices[0].commands[0].validations[0];
        if (concept.kind !== 'DeclarativeValidateSyntax' || command.kind !== 'DeclarativeValidateSyntax') throw new Error('Expected declarative validation');
        command.rules[0].rule.should.equal(concept.rules[0].rule);
        command.rules[0].rule.should.not.equal('Matches');
    });
});

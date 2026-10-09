// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse, parseSpecificationSource } from '../../ScreenplayCompiler';
import { expandEffectiveSpecificationExamples } from '../SpecificationCommandExamples';

const source = `module M
  feature F
    slice StateChange S
      command Record
        amount Int
      event Recorded
        amount Int
      specification Recording
        parameter amount Int
        case Small amount = 10
        case Large amount = 100
        when Record amount = case.amount
        then Recorded amount = case.amount`;

describe('when parsing specification cases', () => {
    it('should expand named cases in order', () => {
        const result = parse(source);
        result.diagnostics.should.deep.equal([]);
        const expanded = expandEffectiveSpecificationExamples(result.value);
        expanded.specifications.map(specification => specification.effective.name).should.deep.equal(['Recording_Small', 'Recording_Large']);
        expanded.specifications.map(specification => specification.case).should.deep.equal(['Small', 'Large']);
        expanded.specifications[0].steps[0].values[0].origin.should.equal('case');
        expanded.specifications[0].steps[0].values[0].caseParameter!.should.equal('amount');
    });
    it('should substitute error messages', () => {
        const result = parse(source.replace('then Recorded amount = case.amount', 'then error case.reason').replace('parameter amount Int', 'parameter amount Int\n        parameter reason String').replace('case Small amount = 10', 'case Small amount = 10\n          reason = "$strings.small"').replace('case Large amount = 100', 'case Large amount = 100\n          reason = "large"'));
        result.diagnostics.should.deep.equal([]);
        expandEffectiveSpecificationExamples(result.value).specifications[0].effective.thenErrors[0].name!.should.equal('$strings.small');
    });
    it.each([
        ['parameter amount', 'PLAY0570'],
        ['case Bad invalid', 'PLAY0571'],
        ['parameter amount Int\n  parameter amount Int\n  case Small amount = 1', 'PLAY0572'],
        ['parameter amount Int\n  case Small amount = 1\n  case Small amount = 2', 'PLAY0573'],
        ['parameter amount Int', 'PLAY0574'],
        ['case Small amount = 1', 'PLAY0574'],
        ['parameter amount Int\n  case Small', 'PLAY0575'],
        ['parameter amount Int\n  case Small amount = case.amount', 'PLAY0576'],
        ['when Record amount = case.amount', 'PLAY0577'],
    ])('should reject %s', (body, code) => {
        parseSpecificationSource(`specification Table\n  ${body}`).diagnostics.some(diagnostic => diagnostic.code === code).should.equal(true);
    });
});

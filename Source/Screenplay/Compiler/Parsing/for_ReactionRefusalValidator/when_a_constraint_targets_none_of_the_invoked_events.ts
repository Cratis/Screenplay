// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { Diagnostic } from '../../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';

for (const target of ['Claimed', 'OtherClaimed']) {
    describe(`when a named refusal constraint targets ${target}`, () => {
        let diagnostics: readonly Diagnostic[];
        beforeEach(() => {
            diagnostics = parse(`module Billing
  feature Payments
    slice Automation Claiming
      event Approved
      event Claimed
      event OtherClaimed
      command Claim
        produces Claimed
      constraint OtherClaim
        unique event ${target}
      reaction Claimer
        when Approved
          invokes Claim
            on refused by constraint OtherClaim
              acknowledge
`).diagnostics;
        });
        it('should warn only if the constraint cannot apply', () => diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.UnreachableRefusalBranch).map(diagnostic => diagnostic.severity).should.deep.equal(target === 'Claimed' ? [] : ['warning']));
        it('should keep the valid branch free of errors', () => diagnostics.filter(diagnostic => diagnostic.severity === 'error').should.be.empty);
    });
}

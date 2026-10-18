// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';

for (const branchProduces of [false, true]) {
    describe(`when an event has ${branchProduces ? 'a producing' : 'an acknowledging'} refusal branch`, () => {
        let errors: string[];
        beforeEach(() => {
            const result = parse(`eventsource Account
  identifier String
  stream Ledger
module M
  feature F
    slice Automation S
      event Started
      event Changed
      command Change
        id String identifier
        stream Account.Ledger
        produces Changed
          for id
      reaction R
        when Started
          invokes Change
            id = "account"
            on refused by validation
              ${branchProduces ? 'produces Changed' : 'acknowledge'}
      specification X
        when Change
          id = "account"
        then Changed
          no stream
`);
            errors = result.diagnostics.filter(diagnostic => diagnostic.severity === 'error').map(diagnostic => diagnostic.code);
        });
        it('should defer route comparison only if the branch produces the event', () => errors.should.deep.equal(branchProduces ? [] : [DiagnosticCodes.SpecificationStreamContradictsCommand]));
    });
}

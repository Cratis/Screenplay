// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { parse } from '../ScreenplayCompiler';

const source = `concept ResourceId : String
concept Month : String
module Work
  feature Reporting
    slice StateView Totals
      event Recorded
      readmodel Totals
        month Month key
      reducer Fold => Totals
        on Recorded
      query ByMonth => Totals
        by month Month
    slice StateChange Record
      command Record
        resource ResourceId identifier
        produces Recorded
          for resource`;

describe('when seeing a native reducer identity case', () => {
    it('should catalogue the permanent native warning', () => {
        DiagnosticCodes.ReducerIdentityMismatch.should.equal('PLAY0690');
    });
    it('should not claim TypeScript provenance analysis', () => {
        const result = parse(source);
        result.success.should.be.true;
        result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.ReducerIdentityMismatch).should.deep.equal([]);
    });
});

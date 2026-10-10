// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';

describe('when comparing a partial composite specification route', () => {
    it('should report missing parts without inventing a route contradiction from an absent value', () => {
        const result = parse(`eventsource Account
  identifier String
  stream Monthly
    streamId
      first String
      second String
module M
  feature F
    slice StateChange S
      command C
        id String identifier
        stream Account.Monthly
          streamId
            first = "one"
            second = "two"
        produces event Changed
      specification Partial
        when C
          id = "account"
        then Changed
          for "account"
          stream Account.Monthly
            streamId
              first = "one"
`);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain(DiagnosticCodes.InvalidSpecificationStreamRoute);
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).not.toContain(DiagnosticCodes.SpecificationStreamContradictsCommand);
        expect(result.value.modules[0].features[0].slices[0].specifications[0].thenEvents[0].stream!.streamIdParts.map(part => part.property)).toEqual(['first']);
    });
});

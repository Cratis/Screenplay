// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { enumerationPinningDiagnostics } from '../EnumerationPinning';

describe('when a stored type nests an enumeration through a shared type', () => {
    it('should report the nested enumeration once', () => {
        const compilation = parse(`concept Status : Enum
  open
  closed

type Detail
  status Status

type Line
  first  Detail
  second Detail

module M
  feature F
    slice StateChange S
      event LineRecorded
        line  Line
        other Line`);
        compilation.success.should.equal(true);
        enumerationPinningDiagnostics(compilation.value).map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0671']);
    });
});

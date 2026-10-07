// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { dependencyMapFor } from '../dependencyMapFor';

const map = dependencyMapFor(parse(`module Facts
  feature Record
    slice StateChange First
      event FactRecorded
    slice StateChange Second
      event FactRecorded
module Views
  feature Display
    slice StateView Show
      readmodel Facts
      projection Facts
        from FactRecorded
`).value);

describe('when mapping ambiguous references', () => {
    it('should retain the alternative slice addresses without embedding nodes', () => map.evidence.find(item => item.ambiguous)!.alternatives.should.deep.equal(['Facts.Record.Second']));
});

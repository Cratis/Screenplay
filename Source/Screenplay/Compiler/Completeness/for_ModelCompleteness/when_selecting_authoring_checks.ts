// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { completenessDiagnostics } from '../ModelCompleteness';

const source = `policy Member
  require role "A" or role "B"
persona Person
  policy Member
purpose Unused
module M
  feature F
    slice Automation S
      event E
      command Producer
        produces E
          for "one"
      command C
      reaction R
        runs as system role "Automation"
        when E
          invokes C`;

describe('when selecting authoring completeness checks', () => {
    it('should use C# check ordering, independent of selection ordering and duplicates', () => {
        const compilation = parse(source);
        expect(compilation.success, JSON.stringify(compilation.diagnostics)).toBe(true);
        const expected = ['PLAY0574', 'PLAY0576', 'PLAY0605', 'PLAY0606', 'PLAY0652'];
        expect(completenessDiagnostics(compilation, ['privilege', 'purposes', 'personas', 'PLAY0652']).map(diagnostic => diagnostic.code)).toEqual(expected);
        expect(completenessDiagnostics(compilation, ['all']).map(diagnostic => diagnostic.code)).toEqual(expected);
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

const declarations = `trigger External
  opaque
  known String
module M
  feature F
    slice Automation S`;
const action = `
      specification Kicking
        parameter p Int
        case One p = 1
        when trigger External
          opaque = case.p`;
const producer = `
      event Occurred
      reaction Forward
        when External
          produces Occurred
            for opaque
      specification Appending
        parameter p Int
        case One p = 1
        when append Occurred
          for case.p
        then Occurred`;

describe('when retaining untyped trigger declaration values', () => {
    it('should keep untyped names separate from typed trigger properties', () => {
        const result = parse(declarations);
        expect(result.value?.declaredTriggers?.[0].data.map(property => property.name)).toEqual(['known']);
        expect(result.value?.declaredTriggers?.[0].untypedValues).toEqual(['opaque']);
    });
    it('should not invent an incompatible type for a specification trigger value', () => {
        expect(parse(declarations + action).diagnostics).toEqual([]);
    });
    it('should not invent a producer destination type from an untyped trigger value', () => {
        expect(parse(declarations + producer).diagnostics).toEqual([]);
    });
});

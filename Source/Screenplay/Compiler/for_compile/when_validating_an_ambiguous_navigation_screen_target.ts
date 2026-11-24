// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

describe('when validating an ambiguous navigation screen target', () => {
    let result: ReturnType<typeof parse>;
    beforeEach(() => {
        result = parse(`module Shop
  outlet sidebar extends
    item projects title "Projects" target Shop.Projects screen Projects
  feature Projects
    slice StateView First
      screen Projects
    slice StateView Second
      screen Projects`);
    });
    it('should reject the document', () => expect(result.success).toBe(false));
    it('should not choose whichever screen comes first', () => expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY1169'));
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const source = `layout Shell
  outlet sidebar of navigation
module Shop
  outlet sidebar extends
    item projects title "Projects" target Shop.Projects screen Projects
  feature Projects
    slice StateView Listing
      screen CreateProject
        fits page
      screen Projects
        fits page`;

describe('when validating a navigation screen target', () => {
    let selected: ReturnType<typeof parse>;
    beforeEach(() => { selected = parse(source); });
    it('should accept a list declared after the create screen', () => expect(selected.success).toBe(true));
    it('should reject an explicit missing screen without falling back', () => expect(parse(source.replace('screen Projects\n', 'screen Missing\n')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY1168'));
});

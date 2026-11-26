// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

describe('when skipping an out of scope navigation screen target', () => {
    it.each([
        ['missing', 'Missing', ''],
        ['ambiguous', 'Projects', '    slice StateView Duplicate\n      screen Projects'],
        ['non-page', 'Dialog', '    slice StateView Dialog\n      screen Dialog\n        fits dialog'],
    ])('should not resolve a %s screen in another module', (_case, screen, extra) => {
        const result = parse(`layout Shell
  category application
  outlet sidebar of navigation
module Shop
  outlet sidebar extends
    item selected title "Selected" target Other.Projects screen ${screen}
module Other
  feature Projects
    slice StateView Listing
      screen Projects
${extra}`);
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0750']);
        result.diagnostics.filter(diagnostic => ['PLAY1168', 'PLAY1169', 'PLAY1170'].includes(diagnostic.code)).should.be.empty;
    });
});

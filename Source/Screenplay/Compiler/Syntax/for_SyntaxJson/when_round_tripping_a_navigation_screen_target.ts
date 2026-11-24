// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../Structure';
import { decodeExactSyntaxJson } from '../StrictSyntaxJson';
import { toSyntaxJson } from '../SyntaxJson';

const source = `numbers exact
module Shop
  outlet sidebar extends
    item projects title "Projects" target Shop.Projects
  feature Projects
    slice StateView Listing
      screen Projects`;

describe('when round tripping a navigation screen target', () => {
    let legacy: string;
    let selected: string;
    beforeEach(() => {
        legacy = JSON.stringify(toSyntaxJson(parse(source).value));
        selected = JSON.stringify(toSyntaxJson(parse(source.replace('target Shop.Projects', 'target Shop.Projects screen Projects')).value));
    });
    it('should read old targets without a screen member', () => {
        const restored = decodeExactSyntaxJson(legacy) as ApplicationSyntax;
        expect(restored.modules[0].outlets![0].items![0]).toMatchObject({ target: { module: 'Shop', feature: 'Projects', screen: null } });
        expect(legacy).not.toContain('"screen":');
    });
    it('should preserve the selected screen through the complete source protocol', () => {
        const restored = decodeExactSyntaxJson(selected) as ApplicationSyntax;
        expect(restored.modules[0].outlets![0].items![0]).toMatchObject({ target: { screen: 'Projects' } });
        const normalized = JSON.stringify(toSyntaxJson(restored));
        expect(JSON.stringify(toSyntaxJson(decodeExactSyntaxJson(normalized)))).toBe(normalized);
        expect(selected).toContain('"screen":"Projects"');
    });
});

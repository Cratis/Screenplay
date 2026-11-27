// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';

const document = (clause: string, screens = 'screen Projects\n  fits page', extra = '') => `layout Shell
  outlet sidebar of navigation
module Shop
  outlet sidebar extends
    item projects title "Projects" ${clause}
  feature Projects
    slice StateView Listing
${screens.split('\n').map(line => `      ${line}`).join('\n')}
${extra}`;

export const navigationScreenDocument = `layout Shell
  outlet sidebar of navigation
module Shop
  outlet sidebar extends
    item projects title "Projects" target Shop.Projects screen Projects
    item create title "Create" screen CreateProject target Shop.Projects
    item group title "More"
      item nested title "Archived" target Shop.Projects.Archived screen Archived
    item shared title "Shared" target Shop.Projects screen Shared
    item legacy title "Default" target Shop.Projects
  screen Shared
    fits page
  feature Projects
    slice StateView Listing
      screen CreateProject
        fits page
      screen Projects
        fits page
      uses screen Shared
    feature Archived
      slice StateView Listing
        screen Archived
`;

describe('when parsing a navigation screen target', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => { result = parse(navigationScreenDocument); });

    it('should accept either clause order and nested items', () => result.success.should.equal(true));
    it('should retain the selected screen', () => {
        const item = result.value.modules[0].outlets![0].items[0];
        if (item.kind !== 'NavigationItemSyntax') throw new Error('Expected navigation item');
        item.target!.screen!.should.equal('Projects');
    });
    it('should not reorder screens to choose a destination', () => result.value.modules[0].features[0].slices[0].screens[0].name.should.equal('CreateProject'));
    it('should not add a screen to old targets', () => {
        const item = result.value.modules[0].outlets![0].items[4];
        expect(item.kind === 'NavigationItemSyntax' && item.target?.screen).toBeUndefined();
    });

    it.each(['target Shop.Projects screen Projects screen Projects', 'target Shop.Projects screen "Projects"', 'target Shop.Projects screen Projects.List', 'target Shop.Projects screen', 'target Shop screen Projects', 'screen Projects'])('should reject malformed clause %s', clause => {
        const invalid = parse(document(clause));
        expect(invalid.success).toBe(false);
        expect(invalid.diagnostics.some(diagnostic => diagnostic.code === 'PLAY0741')).toBe(true);
    });

    it('should reject a missing executable screen', () => expect(parse(document('target Shop.Projects screen Missing')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY1168'));
    it('should reject a descendant screen instead of changing feature', () => expect(parse(document('target Shop.Projects screen Archived', 'screen Projects', '    feature Archive\n      slice StateView View\n        screen Archived')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY1168'));
    it('should reject an unadopted free screen', () => expect(parse(document('target Shop.Projects screen Free', 'screen Projects', '  screen Free')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY1168'));
    it('should reject a competing screen name', () => expect(parse(document('target Shop.Projects screen Projects', 'screen Projects', '    slice StateView Other\n      screen Projects')).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY1169'));
    it.each(['dialog', 'pane'])('should reject a %s-only screen', fit => expect(parse(document('target Shop.Projects screen Projects', `screen Projects\n  fits ${fit}`)).diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY1170'));
    it.each(['', '\n  fits page, dialog'])('should accept default and multi-fit page screens', fit => expect(parse(document('target Shop.Projects screen Projects', `screen Projects${fit}`)).success).toBe(true));
    it('should not rehome a free screen at its second adopter', () => {
        const invalid = parse(navigationScreenDocument.replace('target Shop.Projects screen Shared', 'target Shop.Other screen Shared') + '  feature Other\n    slice StateView Other\n      uses screen Shared\n');
        expect(invalid.diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY1168');
    });
});

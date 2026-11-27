// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';

const source = `numbers exact
layout Shell
  category Browsing
  type Master
  scopes module, slice statechange
  main page routed by selected primary optional
  side panel
  main pane
  on enter
    navigate back
module Work
  screen template Workspace
    display "Workspace"
    description "Browse items"
    scopes feature, slice stateview
    type Dashboard
    content
    on click
      navigate back
  dialog template Editor
    display "Editor"
    description "Edit an item"
    scopes none
    content
    on load
      navigate back
`;

describe('when retaining interaction owner metadata', () => {
    let application: ApplicationSyntax;

    beforeAll(() => { application = parse(source).value; });

    it('should retain layout classification and scope metadata beside its behavior', () => {
        const layout = application.layouts![0];
        layout.category!.should.equal('Browsing');
        layout.templateType!.should.equal('Master');
        layout.restrictsScopes.should.be.true;
        layout.scopes.should.deep.equal(['module', 'slice']);
        layout.sliceKinds.should.deep.equal(['StateChange']);
        layout.behaviors[0].bindings[0].actions[0].kind.should.equal('NavigateBackActionSyntax');
    });

    it('should retain explicit slot routing and role flags', () => {
        const slot = application.layouts![0].slots![0];
        [slot.name, slot.role, slot.routed, slot.routedBy, slot.primary, slot.optional].should.deep.equal(['main', 'Page', true, 'selected', true, true]);
    });

    it('should retain distinct slots without replacing the first declaration of a duplicate', () => {
        const slots = application.layouts![0].slots!;
        slots.map(slot => [slot.name, slot.role]).should.deep.equal([['main', 'Page'], ['side', 'Panel']]);
    });

    it('should retain screen template display and description metadata beside its behavior', () => {
        const template = application.modules[0].screenTemplates![0];
        template.displayName!.should.equal('Workspace');
        template.description!.should.equal('Browse items');
        template.templateType!.should.equal('Dashboard');
        template.scopes.should.deep.equal(['feature', 'slice']);
        template.sliceKinds.should.deep.equal(['StateView']);
        template.behaviors.should.have.lengthOf(1);
    });

    it('should distinguish an explicitly empty dialog scope from unrestricted scopes', () => {
        const template = application.modules[0].dialogTemplates![0];
        template.displayName!.should.equal('Editor');
        template.description!.should.equal('Edit an item');
        template.restrictsScopes.should.be.true;
        template.scopes.should.deep.equal([]);
        template.sliceKinds.should.deep.equal([]);
        template.behaviors.should.have.lengthOf(1);
    });
});

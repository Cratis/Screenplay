// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ComponentPropertySyntax, ScreenComponentSyntax } from '../../Screens';
import { a_recording_walker } from '../given/a_recording_walker';
import { given } from '../given/given';
import { component, location } from '../given/syntax';

describe('when walking a screen component without optional children', given(a_recording_walker, context => {
    const property: ComponentPropertySyntax = { kind: 'ComponentPropertySyntax', property: 'label', binding: null, value: null, location };
    const syntax: ScreenComponentSyntax = { ...component(), properties: [property] };

    beforeEach(() => context.visitScreenDirective(syntax));

    it('should not dispatch absent bindings or values', () => {
        context.hooks.should.deep.equal(['directive', 'node', 'node']);
    });

    it('should visit only the component and its property node', () => {
        context.nodes.should.have.ordered.members([syntax, syntax, property]);
    });
}));

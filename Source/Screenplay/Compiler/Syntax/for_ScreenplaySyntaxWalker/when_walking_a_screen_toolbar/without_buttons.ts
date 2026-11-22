// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ScreenToolbarSyntax } from '../../Screens';
import { a_recording_walker } from '../given/a_recording_walker';
import { given } from '../given/given';
import { location, toolbarItem } from '../given/syntax';

describe('when walking a screen toolbar without buttons', given(a_recording_walker, context => {
    const item = toolbarItem('details');
    const syntax: ScreenToolbarSyntax = { kind: 'ScreenToolbarSyntax', name: 'actions', items: [item], location };

    beforeEach(() => context.visitScreenDirective(syntax));

    it('should not add visits for an omitted button collection or condition', () => {
        context.hooks.should.deep.equal(['directive', 'node', 'node']);
        context.nodes.should.have.ordered.members([syntax, syntax, item]);
    });
}));

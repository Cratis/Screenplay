// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ScreenActionSyntax, ScreenGuardedActionSyntax, ScreenToolbarSyntax } from '../Syntax/Screens';
import { a_parsed_document } from './given/a_parsed_document';

const parse = () => a_parsed_document('module Work', '  feature Items', '    slice StateView Details',
    '      command Retry', '        itemId Uuid', '      query ExternalDetails => ExternalItem', '      screen Details', '        data ExternalItem via query ExternalDetails',
    '        action Retry for item', '          with itemId from item.itemId', '        action Work.Items.Details.Retry for screen',
    '        action "Again" for screen', '          when item.ready == true execute Retry',
    '        toolbar', '          item retry action Retry for item', '            parameter itemId from item.itemId');

describe('when parsing explicit action subjects', () => {
    it('should accept the explicit subjects', () => parse().diagnostics.should.deep.equal([]));
    it('should retain authored subjects and inputs', () => {
        const directives = parse().value.modules[0].features[0].slices[0].screens[0].directives;
        const action = directives[1] as ScreenActionSyntax;
        action.subject!.should.equal('Item');
        action.inputs![0].binding.should.equal('item.itemId');
        (directives[2] as ScreenActionSyntax).subject!.should.equal('Screen');
        (directives[3] as ScreenGuardedActionSyntax).subject!.should.equal('Screen');
        const item = (directives[4] as ScreenToolbarSyntax).items[0];
        item.subject!.should.equal('Item');
        item.parameters[0].name.should.equal('itemId');
    });
});

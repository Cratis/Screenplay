// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { toEventModelDocument } from '../../Mapping/EventModelDocumentVisitor';
import { screenDocumentFor } from '../screenDocumentKey';
import { screensOfApplication } from '../screensOfApplication';
import { application_of } from './given/a_compiled_application';

// Two personas the module's authorize lets in, so the slice is drawn in two rows - and the same screen belongs
// in both of them, because a Screenplay screen belongs to its slice rather than to one role.
const application = application_of(
    'persona Buyer',
    '  policy CanBrowse',
    'persona Seller',
    '  policy CanBrowse',
    'module Orders',
    '  authorize CanBrowse',
    '  feature Browsing',
    '    slice StateView Orders',
    '      readmodel Order',
    '        name string',
    '      screen Orders',
    '        title "Orders"');

describe('when personas use the slice', () => {
    const screens = screensOfApplication(application);
    const document = toEventModelDocument(application, 'Work');
    const slice = document.collections[0].modules[0].features[0].slices[0];

    it('should draw the slice for both personas', () => slice.actors.length.should.equal(2));

    it('should carry the screen in every row the slice is drawn in', () =>
        slice.actors.every(actor => screenDocumentFor(screens.documents, slice.id, actor.id) !== undefined).should.be.true);

    it('should carry the same screen in each of them', () => {
        const documents = slice.actors.map(actor => screenDocumentFor(screens.documents, slice.id, actor.id));
        documents[0]!.should.equal(documents[1]);
    });
});

describe('when a screen is adopted by a slice', () => {
    const adopted = application_of(
        'module Orders',
        '  screen Orders',
        '    title "Orders"',
        '  feature Browsing',
        '    slice StateView OrderList',
        '      readmodel Order',
        '        name string',
        '      uses screen Orders');
    const screens = screensOfApplication(adopted);
    const document = toEventModelDocument(adopted, 'Work');
    const slice = document.collections[0].modules[0].features[0].slices[0];

    it('should draw it with the slice that adopted it', () =>
        (screenDocumentFor(screens.documents, slice.id, slice.actors[0].id) !== undefined).should.be.true);
});

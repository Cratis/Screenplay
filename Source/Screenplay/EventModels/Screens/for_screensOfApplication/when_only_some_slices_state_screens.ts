// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { toEventModelDocument } from '../../Mapping/EventModelDocumentVisitor';
import { screenDocumentFor } from '../screenDocumentKey';
import { screensOfApplication } from '../screensOfApplication';
import { application_of } from './given/a_compiled_application';

// A nested feature, so a screen is found wherever the model puts it, and a slice beside it with no screen.
const application = application_of(
    'module Orders',
    '  feature Browsing',
    '    slice StateChange RecordOrder',
    '      event OrderRecorded',
    '    feature Detail',
    '      slice StateView OrderDetail',
    '        readmodel Order',
    '          name string',
    '        screen OrderDetail',
    '          title "Order"');

describe('when only some slices state screens', () => {
    const screens = screensOfApplication(application);
    const document = toEventModelDocument(application, 'Work');
    const feature = document.collections[0].modules[0].features[0];
    const withoutScreen = feature.slices[0];
    const nested = feature.subFeatures[0].slices[0];

    it('should carry the nested slice\'s screen', () =>
        (screenDocumentFor(screens.documents, nested.id, nested.actors[0].id) !== undefined).should.be.true);

    it('should carry nothing for the slice without one', () => {
        withoutScreen.actors.should.deep.equal([]);
        Object.keys(screens.documents).some(key => key.startsWith(`${withoutScreen.id}:`)).should.be.false;
    });

    it('should carry exactly one document', () => Object.keys(screens.documents).length.should.equal(1));
});

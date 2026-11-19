// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { toEventModelDocument } from '../../Mapping/EventModelDocumentVisitor';
import { screenDocumentFor } from '../screenDocumentKey';
import { screensOfApplication } from '../screensOfApplication';
import { application_of } from './given/a_compiled_application';

const application = application_of(
    'module Orders',
    '  feature Browsing',
    '    slice StateView Orders',
    '      readmodel Order',
    '        name string',
    '      screen Orders',
    '        title "Orders"');

describe('when a slice states a screen', () => {
    const screens = screensOfApplication(application);
    const document = toEventModelDocument(application, 'Work');
    const slice = document.collections[0].modules[0].features[0].slices[0];
    const actor = slice.actors[0];

    it('should carry a document for the cell the board draws', () => {
        (screenDocumentFor(screens.documents, slice.id, actor.id) !== undefined).should.be.true;
    });

    it('should carry a document the role says it has a screen for', () => actor.hasScreen.should.be.true);

    it('should carry the screen itself, under the name the editor opens', () => {
        const scene = JSON.parse(screenDocumentFor(screens.documents, slice.id, actor.id)!) as { screens: { name: string }[] };
        scene.screens.map(screen => screen.name).should.deep.equal(['Screen']);
    });

    it('should carry what the screen says into the document', () => {
        screenDocumentFor(screens.documents, slice.id, actor.id)!.should.contain('Orders');
    });

    it('should find nothing for a cell of another slice', () => {
        (screenDocumentFor(screens.documents, 'another slice', actor.id) === undefined).should.be.true;
    });

    it('should report nothing it could not draw', () => screens.findings.should.deep.equal([]));
});

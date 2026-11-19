// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { toEventModelDocument } from '../../Mapping/EventModelDocumentVisitor';
import { screenDocumentFor } from '../screenDocumentKey';
import { screensOfApplication } from '../screensOfApplication';
import { application_of } from './given/a_compiled_application';

// A screen whose design is a file the Studio backend reads. The TypeScript compiler recognizes the reference but
// models nothing of it, so there is nothing here to draw - which is a note, never an error.
const application = application_of(
    'module Orders',
    '  feature Browsing',
    '    slice StateView Orders',
    '      readmodel Order',
    '        name string',
    '      screen Orders',
    '        file "orders.screen.json"');

describe('when a screen keeps its design in a file', () => {
    const screens = screensOfApplication(application);
    const document = toEventModelDocument(application, 'Work');
    const slice = document.collections[0].modules[0].features[0].slices[0];

    it('should still say the role has a screen', () => slice.actors[0].hasScreen.should.be.true);

    it('should carry no document for it', () =>
        (screenDocumentFor(screens.documents, slice.id, slice.actors[0].id) === undefined).should.be.true);

    it('should say which screen it could not draw', () => screens.findings.map(finding => finding.screen).should.deep.equal(['Orders']));

    it('should say why', () => screens.findings[0].message.should.contain('file'));
});

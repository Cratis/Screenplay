// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { hasScreenDocuments, noScreenDocuments, screenDocumentFor } from '../screenDocumentKey';
import { noApplicationScreens, screensOfApplication } from '../screensOfApplication';
import { application_of } from './given/a_compiled_application';

describe('when a model has no screen', () => {
    const screens = screensOfApplication(application_of(
        'module Orders',
        '  feature Recording',
        '    slice StateChange RecordOrder',
        '      event OrderRecorded'));

    it('should carry no document', () => screens.documents.should.deep.equal(noScreenDocuments));
    it('should report nothing', () => screens.findings.should.deep.equal([]));
    it('should say it has no screen', () => hasScreenDocuments(screens.documents).should.be.false);
    it('should be the answer a model without screens always gets', () => screens.should.equal(noApplicationScreens));
});

describe('when a model declares nothing at all', () => {
    const screens = screensOfApplication(application_of('persona Buyer'));

    it('should carry no document', () => hasScreenDocuments(screens.documents).should.be.false);
});

describe('when asked for a screen outside a slice or a role', () => {
    const screens = { 'a:b': 'a document' };

    it('should find nothing without a slice', () => (screenDocumentFor(screens, '', 'b') === undefined).should.be.true);
    it('should find nothing without a role', () => (screenDocumentFor(screens, 'a', '') === undefined).should.be.true);
    it('should find the screen of a slice and a role', () => screenDocumentFor(screens, 'a', 'b')!.should.equal('a document'));
});

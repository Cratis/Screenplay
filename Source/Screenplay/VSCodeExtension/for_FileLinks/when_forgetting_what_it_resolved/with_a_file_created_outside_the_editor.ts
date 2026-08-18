// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DocumentLink, state } from '../../vscode.stub';
import { Document, a_document, an_editor_with, links_in } from '../given/an_editor';

// The editor's own file events fire for its own gestures — the explorer, an applied edit — and for
// nothing else. A file that arrives from `git checkout`, a terminal, or a run of the generator fires
// none of them, and that is the very workflow where a `.play` names a file that is about to exist.
describe('when forgetting what it resolved with a file created outside the editor', () => {
    let document: Document;
    let beforeItExisted: DocumentLink[];
    let whileStillCached: DocumentLink[];
    let afterTheWatcherSawIt: DocumentLink[];

    beforeEach(async () => {
        an_editor_with({ folder: '/repo' });
        document = a_document('file Invoicing/Register.cs');

        beforeItExisted = await links_in(document);

        state.files.add('/repo/Invoicing/Register.cs');
        whileStillCached = await links_in(document);

        state.watchers[0].created.fire();
        afterTheWatcherSawIt = await links_in(document);
    });

    it('should watch the whole workspace', () => {
        state.watchers.length.should.equal(1);
        state.watchers[0].pattern.should.equal('**/*');
    });

    it('should ignore a file merely being written to', () => {
        state.watchers[0].ignoresChanges.should.be.true;
    });

    it('should link nothing while the file does not exist', () => {
        beforeItExisted.should.be.empty;
    });

    it('should keep the answer it already gave until something says otherwise', () => {
        whileStillCached.should.be.empty;
    });

    it('should link the file once the watcher has seen it appear', () => {
        afterTheWatcherSawIt.length.should.equal(1);
        afterTheWatcherSawIt[0].target?.fsPath.should.equal('/repo/Invoicing/Register.cs');
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DocumentLink } from '../../vscode.stub';
import { a_document, an_editor_with, links_in } from '../given/an_editor';

// One file in the workspace is the file the document named, so the directive is a link to it.
describe('when providing links with a reference that resolves', () => {
    let links: DocumentLink[];

    beforeEach(async () => {
        an_editor_with({ folder: '/repo', files: ['/repo/Invoicing/Register.cs'] });
        links = await links_in(a_document('file Invoicing/Register.cs'));
    });

    it('should link the reference', () => {
        links.length.should.equal(1);
    });

    it('should point the link at the file', () => {
        links[0].target?.fsPath.should.equal('/repo/Invoicing/Register.cs');
    });

    it('should name the file it points at', () => {
        links[0].tooltip?.should.equal('/repo/Invoicing/Register.cs');
    });
});

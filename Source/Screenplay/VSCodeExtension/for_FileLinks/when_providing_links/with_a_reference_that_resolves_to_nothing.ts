// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DocumentLink } from '../../vscode.stub';
import { a_document, an_editor_with, links_in } from '../given/an_editor';

// A `.play` is read where the tree it names is absent, and Arc emits a synthesised path when it knows
// no real one, so pointing nowhere is an ordinary state of an ordinary document. It gets no link at all.
describe('when providing links with a reference that resolves to nothing', () => {
    let links: DocumentLink[];

    beforeEach(async () => {
        an_editor_with({ folder: '/repo', files: ['/repo/Invoicing/Something.cs'] });
        links = await links_in(a_document('file Invoicing/Register.cs'));
    });

    it('should mark nothing', () => {
        links.should.be.empty;
    });
});

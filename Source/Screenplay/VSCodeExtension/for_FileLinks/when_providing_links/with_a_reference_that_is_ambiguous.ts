// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DocumentLink } from '../../vscode.stub';
import { a_document, an_editor_with, links_in } from '../given/an_editor';

// Two files could be meant. The link exists so the tooltip can say so, and carries no target so that
// activating it goes nowhere — a link to the wrong file is worse than a link to nothing.
describe('when providing links with a reference that is ambiguous', () => {
    let links: DocumentLink[];

    beforeEach(async () => {
        an_editor_with({
            folder: '/repo',
            files: [
                '/repo/Modules/Invoicing/Handlers/Register.cs',
                '/repo/Modules/Billing/Handlers/Register.cs',
            ],
        });
        links = await links_in(a_document('file Handlers/Register.cs'));
    });

    it('should still mark the reference', () => {
        links.length.should.equal(1);
    });

    it('should point the link at nothing', () => {
        (links[0].target === undefined).should.be.true;
    });

    it('should name every file it could have meant', () => {
        links[0].tooltip?.should.contain('/repo/Modules/Invoicing/Handlers/Register.cs');
        links[0].tooltip?.should.contain('/repo/Modules/Billing/Handlers/Register.cs');
    });

    it('should say how many files match', () => {
        links[0].tooltip?.should.contain("2 files match 'Handlers/Register.cs'");
    });
});

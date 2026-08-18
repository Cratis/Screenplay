// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DocumentLink } from '../../vscode.stub';
import { a_document, an_editor_with, completed, links_in } from '../given/an_editor';

// A link without a target is an incomplete link, and the editor completes one by asking the provider
// when it is activated. Answering with the link unchanged is what makes the ambiguous case inert; a
// provider that answers nothing leaves the editor with a link it cannot follow and nothing to say why.
describe('when completing a link that has no target', () => {
    let link: DocumentLink;

    beforeEach(async () => {
        an_editor_with({
            folder: '/repo',
            files: [
                '/repo/Modules/Invoicing/Handlers/Register.cs',
                '/repo/Modules/Billing/Handlers/Register.cs',
            ],
        });
        const links = await links_in(a_document('file Handlers/Register.cs'));
        link = links[0];
    });

    it('should answer with the link it was given', () => {
        (completed(link) === link).should.be.true;
    });

    it('should leave it pointing at nothing', () => {
        (link.target === undefined).should.be.true;
    });

    it('should leave what it says about the candidates alone', () => {
        link.tooltip?.should.contain('/repo/Modules/Billing/Handlers/Register.cs');
    });
});

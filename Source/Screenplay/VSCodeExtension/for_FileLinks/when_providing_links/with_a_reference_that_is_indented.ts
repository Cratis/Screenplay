// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DocumentLink } from '../../vscode.stub';
import { a_document, an_editor_with, links_in } from '../given/an_editor';

// The language package reports one-based columns, the way it reports the position of a validation issue,
// and an editor range is zero-based. The link has to sit exactly on the path — a range off by one either
// underlines the space before it or runs a character past its end.
describe('when providing links with a reference that is indented', () => {
    let links: DocumentLink[];

    beforeEach(async () => {
        an_editor_with({ folder: '/repo', files: ['/repo/Invoicing/Register.cs'] });
        links = await links_in(
            a_document(['concept Invoice', '    identifier Id', '    file Invoicing/Register.cs'].join('\n')),
        );
    });

    it('should place the link on the line the reference is written on', () => {
        links[0].range.start.line.should.equal(2);
        links[0].range.end.line.should.equal(2);
    });

    it('should start the link at the first character of the path', () => {
        links[0].range.start.character.should.equal(9);
    });

    it('should end the link at the last character of the path', () => {
        links[0].range.end.character.should.equal(30);
    });
});

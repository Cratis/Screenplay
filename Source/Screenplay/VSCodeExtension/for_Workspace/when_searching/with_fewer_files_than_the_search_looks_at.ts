// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceSearch } from '../../FileReferenceResolution';
import { workspaceProbe } from '../../Workspace';
import { a_workspace_holding } from '../given/a_workspace';

// The ordinary case, and the one that has to keep working: a search that saw everything says so, or
// nothing would ever resolve through the fallback again.
describe('when searching with fewer files than the search looks at', () => {
    let search: FileReferenceSearch;

    beforeEach(async () => {
        a_workspace_holding([
            '/repo/Modules/Invoicing/Handlers/Register.cs',
            '/repo/node_modules/some-package/Register.cs',
        ]);
        search = await workspaceProbe().search('Register.cs');
    });

    it('should say the answer is complete', () => {
        search.truncated.should.be.false;
    });

    it('should find the file', () => {
        search.files.should.deep.equal(['/repo/Modules/Invoicing/Handlers/Register.cs']);
    });
});

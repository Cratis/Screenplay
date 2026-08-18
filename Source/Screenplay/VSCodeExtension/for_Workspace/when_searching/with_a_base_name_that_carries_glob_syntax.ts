// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceSearch } from '../../FileReferenceResolution';
import { workspaceProbe } from '../../Workspace';
import { a_workspace_holding } from '../given/a_workspace';

// A route file is named `[id].tsx` on every Next.js and Remix tree there is, and a glob reads brackets
// as a character class. Interpolated as written, `[id].tsx` searches for the single-character names
// `i.tsx` and `d.tsx` and never for the file itself — a whole naming convention the fallback cannot see.
describe('when searching with a base name that carries glob syntax', () => {
    let search: FileReferenceSearch;

    beforeEach(async () => {
        a_workspace_holding(['/repo/routes/[id].tsx', '/repo/i.tsx', '/repo/d.tsx']);
        search = await workspaceProbe().search('[id].tsx');
    });

    it('should find the file that is named that', () => {
        search.files.should.contain('/repo/routes/[id].tsx');
    });

    it('should not read the base name as a pattern', () => {
        search.files.should.not.contain('/repo/i.tsx');
        search.files.should.not.contain('/repo/d.tsx');
    });
});

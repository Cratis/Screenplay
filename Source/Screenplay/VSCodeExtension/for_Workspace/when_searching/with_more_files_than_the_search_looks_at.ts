// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceSearch } from '../../FileReferenceResolution';
import { workspaceProbe } from '../../Workspace';
import { a_workspace_holding } from '../given/a_workspace';

// `index.ts` appears hundreds of times in any real monorepo. The editor promises nothing about which of
// them a capped search hands back, so a full result set has to come back saying it may be a subset —
// otherwise whatever survived the filter looks like the only file that could have been meant.
describe('when searching with more files than the search looks at', () => {
    let search: FileReferenceSearch;

    beforeEach(async () => {
        a_workspace_holding(
            Array.from({ length: 200 }, (_, index) => `/repo/module-${index}/index.ts`),
        );
        search = await workspaceProbe().search('index.ts');
    });

    it('should say the answer may be incomplete', () => {
        search.truncated.should.be.true;
    });

    it('should not hand back the whole workspace to say so', () => {
        search.files.length.should.be.lessThan(200);
    });
});

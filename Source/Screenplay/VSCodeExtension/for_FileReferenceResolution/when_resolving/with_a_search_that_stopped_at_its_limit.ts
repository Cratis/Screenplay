// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { FileReferenceResolution, resolveFileReference } from '../../FileReferenceResolution';
import { a_probe, the_roots } from '../given/a_probe';

// Two modules hold `Handlers/Register.cs`, and thirty-one unrelated files share the base name. The
// search stops before it reaches the second of the two, so one survivor is left — and one survivor of an
// incomplete search is not the same fact as one file in the workspace. Resolving it would link an
// arbitrary one of two files with full confidence, which is the one thing the design says never to do.
describe('when resolving with a search that stopped at its limit', () => {
    let result: FileReferenceResolution;

    beforeEach(async () => {
        const probe = new a_probe(
            [
                ...Array.from({ length: 31 }, (_, index) => `/repo/Elsewhere/${index}/Register.cs`),
                '/repo/Modules/Invoicing/Handlers/Register.cs',
                '/repo/Modules/Billing/Handlers/Register.cs',
            ],
            32,
        );
        result = await resolveFileReference('Handlers/Register.cs', the_roots(), probe);
    });

    it('should not resolve to the one match it happened to see', () => {
        result.kind.should.not.equal('resolved');
    });

    it('should report what it saw and link none of it', () => {
        result.should.deep.equal({
            kind: 'ambiguous',
            truncated: true,
            candidates: ['/repo/Modules/Invoicing/Handlers/Register.cs'],
        });
    });
});

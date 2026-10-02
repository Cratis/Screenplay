// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { completionEntriesFor, planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';

describe('when completing a file import', () => {
    it('should complete a path inside the quotes, replacing what is typed', () => {
        planCompletions(['module Ordering', '  import "Ord'], 1, '  import "Ord').should.deep.equal({ kind: 'playFiles', replaceLength: 3 });
    });

    it('should offer nothing after a quote outside an import', () => {
        planCompletions(['module Ordering', '  description "'], 1, '  description "').should.deep.equal({ kind: 'none' });
    });

    for (const [chain, scope] of [[[], 'top level'], [['module'], 'module'], [['feature', 'module'], 'feature']] as const) {
        it(`should offer a file import at the ${scope}`, () => {
            completionEntriesFor([...chain]).some((entry) => entry.label === 'import "…"').should.be.true;
        });
    }

    it('should document both kinds of import on hover', () => {
        hoverContent(['import "**/*.play"'], 0, 'import', 1, 7)!.should.contain('places the imported files there');
    });
});

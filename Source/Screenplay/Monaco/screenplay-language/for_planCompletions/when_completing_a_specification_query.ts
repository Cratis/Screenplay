// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { completionEntriesFor, CompletionPlan, planCompletions } from '../completion-planner';

describe('when completing a specification query', () => {
    let plan: CompletionPlan;

    beforeEach(() => {
        plan = planCompletions([
            'specification LookingUpAProject',
            '  then query Pro',
        ], 1, '  then query Pro');
    });

    it('should offer the queries declared by the document', () => {
        plan.kind.should.equal('queries');
    });

    it('should offer a query assertion in a specification', () => {
        completionEntriesFor(['specification']).some(entry => entry.label === 'then query').should.be.true;
    });
});

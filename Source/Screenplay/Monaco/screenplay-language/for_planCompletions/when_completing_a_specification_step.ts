// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { completionEntriesFor, planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';

const specification = ['slice StateView Hours', '  specification AtOpening'];
const labelsAfter = (textBefore: string): string[] => {
    const plan = planCompletions([...specification, textBefore], 2, textBefore);
    return plan.kind === 'entries' ? plan.entries.map((entry) => entry.label) : [plan.kind];
};

describe('when completing a specification step', () => {
    it('should offer what can follow given', () => {
        labelsAfter('    given ').should.include.members(['clock', 'capture']);
    });

    it('should offer what can follow when', () => {
        labelsAfter('    when ').should.include.members(['clock', 'trigger', 'capture', 'query']);
    });

    it('should offer what can follow then', () => {
        labelsAfter('    then res').should.include.members(['result', 'result exactly', 'no result']);
    });

    it('should offer the queries after when query', () => {
        labelsAfter('    when query Opening').should.deep.equal(['queries']);
    });

    it('should offer every step on an empty line', () => {
        completionEntriesFor(['specification']).map((entry) => entry.label).should.include.members(['given clock', 'when trigger', 'when capture', 'when query', 'then result', 'then no result']);
    });

    it('should document a step word in a specification', () => {
        hoverContent([...specification, '    when trigger NightlySync'], 2, 'trigger', 10, 17)!.should.contain('application trigger fires');
    });

    it('should document a step word not in a specification as the keyword', () => {
        hoverContent(['trigger NightlySync'], 0, 'trigger', 1, 8)!.should.not.contain('application trigger fires');
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { completionEntriesFor } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';

const headers = ['module M', 'feature F', 'slice StateChange S', 'command C', 'readmodel V', 'reaction R'];

describe('when authoring documentation', () => {
    it.each(['module', 'feature', 'slice', 'command', 'readmodel', 'reaction'])('should offer a markdown block for %s', owner => {
        completionEntriesFor([owner]).find(item => item.label === 'documentation')!.insertText.should.contain('```markdown');
    });
    it('should offer a description for specifications', () => {
        completionEntriesFor(['specification']).some(item => item.label === 'description').should.be.true;
    });
    it.each(headers)('should explain report-only documentation under %s', header => {
        hoverContent([header, '  documentation'], 1, 'documentation', 3, 16)!.should.contain('does not change executable model bytes');
    });
    it.each(headers)('should diagnose a missing documentation fence under %s', header => {
        const prefix = header.startsWith('module') ? [] : header.startsWith('feature') ? ['module M'] : ['module M', '  feature F', ...(header.startsWith('slice') ? [] : ['    slice StateChange S'])];
        const indentation = '  '.repeat(prefix.length);
        validateLines([...prefix, `${indentation}${header}`, `${indentation}  documentation`]).some(issue => issue.code === 'PLAY0558').should.be.true;
    });
    it('should not mistake a typed property for documentation', () => {
        (hoverContent(['command C', '  documentation String'], 1, 'documentation', 3, 16) === null).should.be.true;
    });
});

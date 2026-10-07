// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { validateLines } from '../validation';

describe('when surfacing example route diagnostics', () => {
    it.each(['stream Account.Events', 'streamId = "partition"', 'no stream'])('should surface the invalid example body for %s', route => {
        const issues = validateLines(['example Fixture : Happened', `  ${route}`, '  amount = 1']);
        expect(issues.filter(issue => issue.code === 'PLAY0526')).toHaveLength(1);
    });
});

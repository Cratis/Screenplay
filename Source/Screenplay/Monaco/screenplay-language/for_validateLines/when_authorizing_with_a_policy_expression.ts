// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { validateLines } from '../validation';

const policies = ['policy IsSenior', '  require authenticated', 'policy InWindow', '  require authenticated', 'policy IsCommittee', '  require authenticated'];
const unknown = (lines: string[]) => validateLines(lines).filter((issue) => issue.code === 'PLAY0167').map((issue) => issue.message);

describe('when authorizing with a policy expression', () => {
    it('does not take and for a policy', () => expect(unknown([...policies, 'command C', '  authorize IsSenior and InWindow'])).toEqual([]));
    it('does not take or for a policy', () => expect(unknown([...policies, 'command C', '  authorize IsSenior or InWindow'])).toEqual([]));
    it('does not take not for a policy', () => expect(unknown([...policies, 'command C', '  authorize not IsSenior'])).toEqual([]));
    it('does not take parentheses for part of a name', () => expect(unknown([...policies, 'command C', '  authorize IsSenior and (InWindow or IsCommittee)'])).toEqual([]));
    it('follows an expression onto the next line', () => expect(unknown([...policies, 'command C', '  authorize IsSenior', '    and InWindow'])).toEqual([]));
    it('still reports a name no policy declares', () => expect(unknown([...policies, 'command C', '  authorize IsSenior and Missing'])).toEqual(["Unknown policy 'Missing' — declare it with 'policy Missing'."]));
});

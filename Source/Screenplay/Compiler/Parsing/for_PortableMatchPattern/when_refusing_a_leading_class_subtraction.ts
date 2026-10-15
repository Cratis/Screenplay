// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { validateMatchPattern } from '../PortableMatchPattern';

describe('when refusing subtraction before a class item', () => {
    it('should identify the leading subtraction operator without treating it as a literal', () => {
        const issue = validateMatchPattern('[-[a]]')!;
        issue.construct.should.equal('class subtraction');
        issue.offset.should.equal(1);
        issue.invalid.should.be.false;
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { validateMatchPattern } from '../PortableMatchPattern';

describe('when refusing subtraction in a character range', () => {
    it('should refuse a bracket endpoint rather than constructing a range', () => {
        const issue = validateMatchPattern('[a-[b]]')!;
        issue.construct.should.equal('class subtraction');
        issue.offset.should.equal(2);
        issue.invalid.should.be.false;
    });
});

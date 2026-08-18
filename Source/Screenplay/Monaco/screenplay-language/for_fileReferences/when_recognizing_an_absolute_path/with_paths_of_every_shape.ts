// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { isAbsoluteFileReferencePath } from '../../file-references';

// A reference is relative to the repository root, so it means the same thing on every machine. An
// absolute one names a place on one machine and is wrong without looking anything up.
describe('when recognizing an absolute path', () => {
    it('should recognize a posix path', () => {
        isAbsoluteFileReferencePath('/Users/someone/Invoices/Register.cs').should.be.true;
    });

    it('should recognize a windows path', () => {
        isAbsoluteFileReferencePath('C:/repos/Invoices/Register.cs').should.be.true;
        isAbsoluteFileReferencePath('C:\\repos\\Invoices\\Register.cs').should.be.true;
    });

    it('should recognize a home-relative path', () => {
        isAbsoluteFileReferencePath('~/repos/Invoices/Register.cs').should.be.true;
    });

    it('should not recognize a repository-relative path', () => {
        isAbsoluteFileReferencePath('Invoices/Register.cs').should.be.false;
    });
});

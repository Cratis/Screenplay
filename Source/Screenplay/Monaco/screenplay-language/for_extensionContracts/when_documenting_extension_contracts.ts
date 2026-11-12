// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { keywordDocs } from '../keyword-docs';
import { validateLines } from '../validation';

describe('when documenting extension contracts', () => {
    it('should explain the contract keywords', () => expect(['display', 'requires', 'placeholder', 'frontend', 'backend'].every(keyword => (keywordDocs as Record<string, string>)[keyword]?.includes('contract'))).toBe(true));
    it('should accept a trigger qualified by a system in a reaction', () => expect(validateLines(['module M', '  feature F', '    slice Automation S', '      reaction R', '        when Payments.PaymentSucceeded']).map(issue => issue.message)).not.toContain("Expected 'when <Name>'"));
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { completionEntriesFor } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';
import { DiagnosticCodes } from '@cratis/screenplay-compiler';
import { registerSubLanguage } from '../sub-language-registry';
import { pdl } from '../sub-languages/pdl';

registerSubLanguage('projection', pdl);

describe('when editing additional descriptions', () => {
    it.each(['concept', 'policy', 'constraint', 'projection', 'screen', 'form'])('should complete descriptions in %s', kind => {
        completionEntriesFor([kind]).some(entry => entry.label === 'description').should.be.true;
    });
    it('should describe the shared description grammar', () => {
        hoverContent(['concept C : String', '  description "Intent"'], 1, 'description', 3, 14)!.should.contain('human-readable');
    });
    it('should forward native form-description diagnostics instead of hiding malformed text', () => {
        validateLines(['module M', '  form F for C', '    description invalid']).some(issue => issue.code === DiagnosticCodes.InvalidDescription).should.be.true;
    });
});

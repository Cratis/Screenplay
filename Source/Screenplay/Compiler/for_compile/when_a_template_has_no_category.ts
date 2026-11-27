// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';

const result = parse('layout Shell\n  content\n\nmodule Sales\n  screen template Page\n    body\n\n  dialog template Confirm\n    body\n\n  screen template Categorized\n    category Page\n    body\n');
const missing = result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.TemplateCategoryMissing);

describe('when a template has no category', () => {
    it('should warn for each template without one', () => missing.map(diagnostic => diagnostic.location.line).should.deep.equal([1, 5, 8]));
    it('should only warn', () => missing.every(diagnostic => diagnostic.severity === 'warning').should.be.true);
    it('should name the kind and the template', () => missing[1].message.should.contain("The screen template 'Page' declares no 'category'"));
    it('should still succeed because a missing category only warns', () => result.success.should.be.true);
});

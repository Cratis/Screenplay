// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ScreenToolbarSyntax } from '../Syntax/Screens';
import { a_parsed_document } from './given/a_parsed_document';

describe('when rejecting subjects on non action toolbar items', () => {
    let result: ReturnType<typeof a_parsed_document>;
    let toolbar: ScreenToolbarSyntax;

    beforeEach(() => {
        result = a_parsed_document('module Work', '  feature Items', '    slice StateView Details',
            '      screen Details', '        toolbar',
            '          item action navigate to Details for item',
            '          item go navigate to action for screen',
            '          item open dialog action for item');
        toolbar = result.value.modules[0].features[0].slices[0].screens[0].directives[0] as ScreenToolbarSyntax;
    });

    it('should reject each inapplicable subject clause', () => result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.InvalidActionSubjectClause).should.have.lengthOf(3));
    it('should not attach subjects to non action items', () => toolbar.items.every(item => item.subject == null).should.be.true);
    it('should preserve the navigation named action', () => toolbar.items[1].target.should.equal('action'));
    it('should preserve the item named action', () => toolbar.items[0].name.should.equal('action'));
    it('should preserve the dialog named action', () => toolbar.items[2].target.should.equal('action'));
});

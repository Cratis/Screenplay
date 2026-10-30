// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import * as compiler from '../../ScreenplayCompiler';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';

const prefix = 'module M\n  feature F\n    slice StateChange S\n      command C\n';
const reported = (source: string) => compiler.parse(prefix + source).diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.ImplicitAuthorizationConjunction);
const shape = (source: string) => JSON.stringify(compiler.parse(prefix + source).value, (key, value: unknown) => key === 'location' ? undefined : value);

describe('when reading adjacent policies', () => {
    it.each([
        ['        authorize Staff Managers\n', '        authorize Staff and Managers\n'],
        ['        authorize (Staff or Managers) Auditors\n', '        authorize (Staff or Managers) and Auditors\n'],
        ['        authorize Staff and Managers\n          Auditors\n', '        authorize Staff and Managers\n          and Auditors\n'],
        ['        authorize Staff\n          Managers // both\n', '        authorize Staff\n          and Managers // both\n'],
    ])('should report one information diagnostic at the authorize line and parse as the explicit form of %j', (implicitForm, explicitForm) => {
        const diagnostics = reported(implicitForm);
        expect(diagnostics).toHaveLength(1);
        expect(diagnostics[0].severity).toBe('information');
        expect(diagnostics[0].location.line).toBe(5);
        expect(reported(explicitForm)).toEqual([]);
        expect(shape(implicitForm)).toBe(shape(explicitForm));
    });

    it.each(['        authorize Staff\n', '        authorize Staff and Managers\n', '        authorize Staff or Managers\n', '        authorize Staff\n          or Managers\n'])('should not report explicit operators in %j', source => {
        expect(reported(source)).toEqual([]);
    });
});

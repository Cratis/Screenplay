// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';

describe('when refusing an invalid invocation mapping', () => {
    it('should diagnose the invalid line and preserve a subsequent valid mapping', () => {
        const result = parse('module M\n  feature F\n    slice Automation S\n      reaction R\n        when Changed\n          invokes C\n            invalid mapping\n            value = "kept"');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toContain(DiagnosticCodes.InvalidPropertyMapping);
        const invocation = result.value.modules[0].features[0].slices[0].reactions[0].triggers[0].invokes[0];
        expect(invocation.mappings.map(mapping => mapping.property)).toEqual(['value']);
    });
});

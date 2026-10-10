// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

describe('when skipping opaque requirement children', () => {
    it('should retain recognized metadata after an unknown child block', () => {
        const result = parse('module M\n  feature F\n    slice StateChange S\n      command C\n        amount Int\n        validate\n          require amount > 0\n            opaque\n              ignored String\n            message "Positive only"\n            severity warning');
        const validation = result.value.modules[0].features[0].slices[0].commands[0].validations[0];
        expect(validation.kind).toBe('DeclarativeValidateSyntax');
        if (validation.kind !== 'DeclarativeValidateSyntax') throw new Error('Expected declarative validation');
        expect(validation.requirements).toHaveLength(1);
        expect(validation.requirements![0]).toMatchObject({ message: 'Positive only', severity: 'Warning' });
        expect(result.value.modules[0].features[0].slices[0].commands[0].properties.map(property => property.name)).toEqual(['amount']);
    });
});

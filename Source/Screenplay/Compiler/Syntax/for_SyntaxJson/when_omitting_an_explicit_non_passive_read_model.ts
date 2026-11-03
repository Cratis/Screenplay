// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../SyntaxJson';

describe('when omitting an explicit non-passive read model', () => {
    const parsed = parse('module M\n  feature F\n    slice StateView S\n      readmodel Row\n        id String').value.modules[0].features[0].slices[0].readModels[0];

    it('should omit an explicit false flag like the .NET writer does', () => {
        expect(toSyntaxJson({ ...parsed, isPassive: false })).not.toHaveProperty('isPassive');
    });

    it('should keep a true flag', () => {
        expect(toSyntaxJson({ ...parsed, isPassive: true })).toHaveProperty('isPassive', true);
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { toCompleteSyntaxJson, toSyntaxJson } from '../SyntaxJson';

describe('when omitting an empty specification description', () => {
    it('should omit null metadata in both projections while preserving authored text', () => {
        const specification = parse('module M\n  feature F\n    slice StateChange S\n      command C\n      specification Scenario\n        when C').value.modules[0].features[0].slices[0].specifications[0];
        const authored = { ...specification, description: null };
        expect(toSyntaxJson(authored)).not.toHaveProperty('description');
        expect(toCompleteSyntaxJson(authored)).not.toHaveProperty('description');
        expect(toSyntaxJson({ ...specification, description: 'One scenario' })).toHaveProperty('description', 'One scenario');
        expect(toCompleteSyntaxJson({ ...specification, description: 'One scenario' })).toHaveProperty('description', 'One scenario');
    });
});

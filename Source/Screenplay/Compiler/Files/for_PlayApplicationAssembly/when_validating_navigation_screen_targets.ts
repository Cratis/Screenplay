// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { compileApplication } from '../PlayApplicationAssembly';

const shell = 'import "feature.play"\nmodule Shop\n  outlet sidebar extends\n    item projects title "Projects" target Shop.Projects screen Projects';
const feature = 'module Shop\n  feature Projects\n    slice StateView Listing\n      screen Projects';

describe('when validating assembled navigation screen targets', () => {
    it('should resolve the screen after merging focused files', () => expect(compileApplication(new Map([['model.play', shell], ['feature.play', feature]]), ['model.play']).success).toBe(true));
    it('should report a missing screen only once after merging', () => {
        const result = compileApplication(new Map([['model.play', shell], ['feature.play', feature.replace('screen Projects', 'screen Other')]]), ['model.play']);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY1168')).toHaveLength(1);
    });
});

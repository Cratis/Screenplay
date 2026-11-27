// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { compileApplication } from '../PlayApplicationAssembly';

const model = (category: string) => `import "feature.play"\nlayout Shell\n${category}  main page`;
const feature = 'module Shop\n  feature Projects\n    slice StateView Listing';

describe('when validating template categories across assembled files', () => {
    it('should report a layout without a category once after merging', () => {
        const result = compileApplication(new Map([['model.play', model('')], ['feature.play', feature]]), ['model.play']);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0720')).toHaveLength(1);
    });
    it('should accept a layout that names its category', () => {
        const result = compileApplication(new Map([['model.play', model('  category application\n')], ['feature.play', feature]]), ['model.play']);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0720')).toHaveLength(0);
    });
});

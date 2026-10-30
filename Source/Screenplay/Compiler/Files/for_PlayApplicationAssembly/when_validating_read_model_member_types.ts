// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { compileApplication } from '../PlayApplicationAssembly';

const model = 'import "values.play"\nmodule M\n  feature F\n    slice StateView View\n      readmodel Parent\n        child Child[]';

describe('when validating assembled read-model member types', () => {
    it('should resolve a composite from another file before validating members', () => {
        const result = compileApplication(new Map([
            ['model.play', model], ['values.play', 'type Child\n  value String'],
        ]), ['model.play']);
        result.success.should.be.true;
        result.diagnostics.should.be.empty;
    });
    it('should not reject a member when the assembled type is absent', () => {
        const result = compileApplication(new Map([
            ['model.play', model], ['values.play', 'type Other\n  value String'],
        ]), ['model.play']);
        result.success.should.be.true;
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0273').should.be.empty;
    });
});

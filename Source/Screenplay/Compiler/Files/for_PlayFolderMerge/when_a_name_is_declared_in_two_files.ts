// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { parseFolder } from '../PlayApplicationAssembly';

describe('when a name is declared in two files', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parseFolder([
            { path: 'b.play', source: 'concept ProjectId : String\nmodule Projects\n  description "Second"\n  feature Registration\n    slice StateChange Register' },
            { path: 'a.play', source: 'type ProjectId\n  value String\nmodule Projects\n  description "First"\n  feature Registration\n    slice StateChange Register' },
        ]);
    });

    // Concepts claim their names before types do, whichever file each is in - as in the C# merge.
    it('should keep the concept over a type of the same name', () => {
        [result.value.concepts.length, result.value.types.length].should.deep.equal([1, 0]);
    });

    it('should report the duplicates and the conflicting description', () => {
        result.diagnostics.map(diagnostic => [diagnostic.code, diagnostic.location.path]).should.deep.equal([
            ['PLAY0173', 'a.play'],
            ['PLAY0174', 'b.play'],
            ['PLAY0173', 'b.play'],
        ]);
    });

    it('should combine the modules into one', () => {
        [result.value.modules.length, result.value.modules[0].description].should.deep.equal([1, 'First']);
    });
});

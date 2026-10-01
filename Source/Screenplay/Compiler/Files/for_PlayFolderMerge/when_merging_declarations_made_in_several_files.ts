// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { parseFolder } from '../PlayFolderMerge';

describe('when merging declarations made in several files', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parseFolder([
            { path: 'b.play', source: 'domain Second\nimport Cratis.Identity\nimport Cratis.Billing\nmodule A\n  description "Same"\n  feature F\n    description "Same"' },
            { path: 'a.play', source: 'domain First\nimport Cratis.Identity\nmodule A\n  description "Same"\n  feature F\n    description "Same"\n    feature Nested\n      slice StateChange S' },
        ]);
    });

    it('should keep the domain of the file read first and report the other', () => {
        [result.value.domain!.name, result.diagnostics.map(diagnostic => `${diagnostic.code}:${diagnostic.location.path}`)]
            .should.deep.equal(['First', ['PLAY0172:b.play']]);
    });

    it('should keep each import once, in the order first met', () => {
        result.value.imports.map(item => item.qualifiedName).should.deep.equal(['Cratis.Identity', 'Cratis.Billing']);
    });

    it('should not report descriptions that agree', () => {
        result.value.modules[0].description!.should.equal('Same');
    });

    it('should merge the nested features of a merged feature', () => {
        result.value.modules[0].features[0].features[0].slices.map(slice => slice.name).should.deep.equal(['S']);
    });
});

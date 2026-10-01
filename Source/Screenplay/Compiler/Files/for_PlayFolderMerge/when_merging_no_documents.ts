// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../../ScreenplayCompiler';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { parseFolder } from '../PlayFolderMerge';

describe('when merging no documents', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parseFolder([]);
    });

    it('should succeed with an empty application', () => {
        [result.success, result.value.modules.length, result.value.domain].should.deep.equal([true, 0, null]);
    });

    it('should place the application at the start', () => {
        result.value.location.should.deep.equal({ line: 1, column: 1 });
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when parsing an invalid document', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'modul Projects',
            '  feature Ignored',
            'module Billing',
            '  feature Invoicing',
            '    slice Sideways Pay',
            '  domain Late',
            'domain Late.Domain',
        );
    });

    it('should fail', () => {
        result.success.should.be.false;
    });

    it('should report the codes the C# compiler reports', () => {
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0001', 'PLAY0028', 'PLAY0022', 'PLAY0004']);
    });

    it('should still read the valid module', () => {
        result.value.modules.map(module => module.name).should.deep.equal(['Billing']);
    });

    it('should fall back to a state change for an unknown slice type', () => {
        result.value.modules[0].features[0].slices[0].type.should.equal('StateChange');
    });
});

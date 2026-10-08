// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';

describe('when a file placed in a feature holds a module construct', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parse('screen template Misplaced\n  content\nmodule Ordering\n  description "Not here either"', 'Orders.play', ['Ordering', 'Orders']);
    });

    it('should report it, and the module', () => {
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.UnexpectedInPlacedFile, DiagnosticCodes.ModuleInPlacedFile]);
    });

    it('should name the placement', () => {
        result.diagnostics[0].message.should.equal('Unexpected \'screen\' in a file imported into feature \'Ordering.Orders\' - expected an application declaration or description, documentation, depends on <Name>, authorize, import, feature, slice, contribute, example, \'on <trigger>\' or \'uses <Behavior>\'');
    });
});

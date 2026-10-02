// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';

describe('when a file placed in a module holds what a module cannot', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parse('slice StateChange Stray\n  command Stray\nmodule 1Bad\n  description "Skipped"', 'Ordering.play', ['Ordering']);
    });

    it('should report the slice and the module', () => {
        result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)
            .should.deep.equal([`${DiagnosticCodes.UnexpectedInPlacedFile}@1`, `${DiagnosticCodes.ModuleInPlacedFile}@3`]);
    });

    it('should say what a module body holds', () => {
        result.diagnostics[0].message.should.equal('Unexpected \'slice\' in a file imported into module \'Ordering\' - expected an application declaration or description, authorize, import, screen template, dialog template, form, contribute, feature, \'on <trigger>\' or \'uses <Behavior>\'');
    });

    it('should still place the module', () => {
        result.value.modules.map(module => `${module.name}:${module.isPlacement}`).should.deep.equal(['Ordering:true']);
    });
});

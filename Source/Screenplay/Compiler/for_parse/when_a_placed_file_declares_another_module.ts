// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CompilationResult, parse } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';

describe('when a placed file declares another module', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parse([
            'module Ordering',
            '  description "Restating the placement is fine"',
            '  slice StateChange Misplaced',
            'module Billing',
            '  description "Another module is not"',
            'layout Shell',
            '  content',
            'screen template Misplaced',
            '  content',
            'feature Orders',
            '  slice StateChange PlaceOrder',
            'import "Orders/*.play"',
        ].join('\n'), 'Ordering.play', ['Ordering']);
    });

    it('should join the restated module to the placement', () => {
        result.value.modules.map(module => `${module.name}:${module.description}`).should.deep.equal(['Ordering:Restating the placement is fine']);
    });

    it('should report what the restated module cannot hold, then the other module', () => {
        result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)
            .should.deep.equal([`${DiagnosticCodes.UnknownModuleDirective}@3`, `${DiagnosticCodes.ModuleInPlacedFile}@4`]);
    });

    it('should say where the file is imported', () => {
        result.diagnostics[1].message.should.equal('This file is imported into module \'Ordering\', so it cannot declare module \'Billing\' - import it at the top level of a document instead');
    });

    it('should take a module body construct', () => {
        result.value.modules[0].features.map(feature => feature.name).should.deep.equal(['Orders']);
    });

    it('should take a file import into the module', () => {
        result.value.modules[0].fileImports.map(each => each.pattern).should.deep.equal(['Orders/*.play']);
    });
});

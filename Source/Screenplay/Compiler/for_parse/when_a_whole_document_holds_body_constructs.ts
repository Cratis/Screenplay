// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

describe('when a whole document holds body constructs', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'slice StateChange Register',
            '  command Register',
            'gizmo Thing',
            'import "Bad\\Path.play"',
            'module Ordering',
            '  import Customers.Registered',
            '  feature Orders',
            '    import "Orders.play" twice',
        );
    });

    it('should report each one', () => {
        result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal([
            `${DiagnosticCodes.UnknownTopLevelConstruct}@1`,
            `${DiagnosticCodes.UnknownTopLevelConstruct}@3`,
            `${DiagnosticCodes.InvalidFileImport}@4`,
            `${DiagnosticCodes.InvalidFileImport}@6`,
            `${DiagnosticCodes.InvalidFileImport}@8`,
        ]);
    });

    it('should hint that a body construct belongs in a module or feature', () => {
        result.diagnostics[0].message.should.equal('Unexpected \'slice\' at the top level - \'slice\' belongs in a module or feature; wrap it in one, or import this file from inside one');
    });

    it('should list the top level constructs for anything else', () => {
        result.diagnostics[1].message.should.contain('expected domain, import, concept');
    });

    it('should say an import inside a module names files', () => {
        result.diagnostics[3].message.should.equal('Invalid import \'import Customers.Registered\' - inside a module or feature, import names files: \'import "<path or glob>"\'');
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

for (const performed of [true, false]) {
    describe(`when parsing repeated ${performed ? 'when query' : 'then query'} arguments`, () => {
        let result: CompilationResult<ApplicationSyntax>;
        beforeEach(() => {
            result = a_parsed_document(
                'module Items',
                '  feature Browse',
                '    slice StateView List',
                '      specification Check',
                ...(performed
                    ? ['        when query Find', '          status = "open"', '          status = "closed"', '        then no result']
                    : ['        then query Find', '          arguments', '            status = "open"', '            status = "closed"']),
            );
        });

        it('should report PLAY0468 instead of a fixture-assignment diagnostic', () => {
            result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([DiagnosticCodes.UnknownSpecificationQueryArgument]);
        });

        it('should retain both argument occurrences for binding and tooling', () => {
            const specification = result.value.modules[0].features[0].slices[0].specifications[0];
            const values = performed ? specification.whenQuery!.arguments : specification.thenQueries![0].arguments;
            values.map(argument => argument.property).should.deep.equal(['status', 'status']);
        });
    });
}

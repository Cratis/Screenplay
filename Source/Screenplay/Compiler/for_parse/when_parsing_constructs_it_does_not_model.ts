// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

// Constructs this compiler does not model are skipped whole - including a fenced block indented less
// than the construct it belongs to - so what follows them is still read.
describe('when parsing constructs it does not model', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = a_parsed_document(
            'policy Admins',
            '  claim role = "admin"',
            'module Projects',
            '  authorize policy Admins',
            '  feature Registration',
            '    slice StateChange RegisterProject',
            '      screen Register',
            '        ```typescript',
            'export const Register = () => <div/>;',
            '        ```',
            '      event ProjectRegistered',
            '        name String',
        );
    });

    it('should report nothing', () => {
        result.diagnostics.should.deep.equal([]);
    });

    it('should still read what follows a skipped construct', () => {
        result.value.modules[0].features[0].slices[0].events[0].name.should.equal('ProjectRegistered');
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse } from '../../ScreenplayCompiler';
import { parseFolder } from '../../Files/PlayApplicationAssembly';

const source = 'policy IsAdmin\n  require role "Administrator"\npolicy IsAdmin\n  require role "Auditor"';

describe('when parsing duplicate policies', () => {
    it('should report an error at the second declaration', () => {
        const result = parse(source, 'policies.play');
        result.success.should.be.false;
        result.diagnostics.should.deep.equal([{
            code: DiagnosticCodes.DuplicateDeclaration,
            severity: 'error',
            arguments: {},
            message: "Duplicate declaration of policy 'IsAdmin' - policy names must be unique",
            location: { line: 3, column: 1, path: 'policies.play' },
        }]);
    });
    it('should report every declaration after the first', () => {
        parse(`${source}\npolicy IsAdmin\n  require authenticated`).diagnostics.map(diagnostic => diagnostic.location.line).should.deep.equal([3, 5]);
    });
    it('should preserve the diagnostic in a single file folder', () => {
        const result = parseFolder([{ path: 'only.play', source }]);
        result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0168@3']);
    });
});

describe('when parsing distinct policy names', () => {
    it('should treat names as ordinal', () => {
        parse('policy Access\n  require authenticated\npolicy access\n  require authenticated\npolicy Other\n  require authenticated').diagnostics.should.deep.equal([]);
    });
});

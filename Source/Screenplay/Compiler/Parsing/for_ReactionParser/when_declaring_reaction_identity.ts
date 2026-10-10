// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { should } from 'chai';
import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';

should();
const source = (identity: string) => `module M\n  feature F\n    slice Automation S\n      event E\n      command C\n      reaction R\n        ${identity}\n        when E\n          invokes C`;

describe('when declaring reaction identity', () => {
    it.each(['runs as system', 'runs as system role "A"', 'runs as system role "A" and role "Auditor"'])('should parse %s', identity => {
        const result = parse(source(identity));
        result.diagnostics.should.be.empty;
        result.value.modules[0].features[0].slices[0].reactions[0].runsAs!.syntaxKind.should.equal('system');
    });
    it.each(['runs as Persona', 'runs as system role Name', 'runs as system role ""', 'runs as system role "A" and role "A"', 'runs as system\n        runs as system', 'runs as system\n          role "A"'])('should reject %s', identity => {
        parse(source(identity)).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0647']);
    });
    it('should reject identity under a trigger', () => {
        parse('module M\n  feature F\n    slice Automation S\n      event E\n      reaction R\n        when E\n          runs as system').diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0647']);
    });
    it('should retain escaped literal roles', () => {
        parse(source('runs as system role "A\\"B"')).value.modules[0].features[0].slices[0].reactions[0].runsAs!.roles.should.deep.equal(['A"B']);
    });
    it.each([['', '', ['PLAY0648']], ['', '\n            on refused by authorization\n              acknowledge', ['PLAY0557']], ['runs as system', '', []], ['runs as system', '\n            on refused by authorization\n              acknowledge', []]])('should give only the applicable missing identity warning', (identity, refusal, expected) => {
        parse('policy Access\n  require authenticated\n' + source(identity).replace('      command C', '      command C\n        authorize Access') + refusal).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(expected);
    });
    it('should warn for unused identity', () => {
        parse(source('runs as system').replace('\n          invokes C', '')).diagnostics.map(diagnostic => diagnostic.code).should.deep.equal(['PLAY0649']);
    });
    it.each(['file R.cs', '```csharp\n            return [];\n            ```'])('should not warn for an implementation body', body => {
        parse(source('runs as system').replace('invokes C', body)).diagnostics.should.be.empty;
    });
});

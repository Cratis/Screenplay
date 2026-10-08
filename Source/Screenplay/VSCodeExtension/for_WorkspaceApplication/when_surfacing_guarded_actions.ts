// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import vectors from '../../Compiler/Conformance/diagnostics.json';
import { WorkspaceApplication } from '../WorkspaceApplication';

const guardedCode = (code: string) => /^PLAY034[5-8]$/.test(code);
const cases = vectors.cases.filter(vector => vector.diagnostics.some(diagnostic => guardedCode(diagnostic.split('@')[0])));

describe('when surfacing guarded action diagnostics in workspace files', () => {
    it('should cover every guarded action validation code', () => {
        new Set(cases.flatMap(vector => vector.diagnostics.map(diagnostic => diagnostic.split('@')[0])).filter(guardedCode)).size.should.equal(4);
    });

    it.each(cases)('should preserve code severity message and location for $name', vector => {
        const source = vector.source.join('\n');
        const workspace = new WorkspaceApplication();
        workspace.set('application.play', source);
        const actual = workspace.diagnosticsFor('application.play').filter(diagnostic => guardedCode(diagnostic.code));
        actual.should.deep.equal(parse(source, 'application.play').diagnostics.filter(diagnostic => guardedCode(diagnostic.code)));
        actual.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(vector.diagnostics.filter(diagnostic => guardedCode(diagnostic.split('@')[0])));
    });

    it('should resolve subjects and commands across imported workspace files', () => {
        const workspace = new WorkspaceApplication();
        workspace.set('application.play', 'module M\n  feature F\n    import "declarations.play"\n    import "screen.play"');
        workspace.set('declarations.play', 'slice StateChange Commands\n  command Retry\n    id Uuid\nslice StateView Views\n  readmodel Item\n    status String\n    id Uuid\n  query Details => Item');
        workspace.set('screen.play', 'slice StateView S\n  screen Details\n    data Views.Item via query Views.Details\n    action "Again"\n      when item.missing == null execute Commands.Retry\n        with missing from item.id');
        workspace.diagnosticsFor('screen.play').filter(diagnostic => guardedCode(diagnostic.code)).map(diagnostic => [diagnostic.code, diagnostic.severity, diagnostic.location.path, diagnostic.location.line, diagnostic.location.column]).should.deep.equal([
            ['PLAY0345', 'warning', 'screen.play', 5, 7], ['PLAY0348', 'warning', 'screen.play', 6, 9],
        ]);
        workspace.diagnosticsFor('declarations.play').filter(diagnostic => guardedCode(diagnostic.code)).should.deep.equal([]);
    });
});

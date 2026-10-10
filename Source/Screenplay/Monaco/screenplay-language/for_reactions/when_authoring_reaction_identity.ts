// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { should } from 'chai';
import { describe, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { validateLines } from '../validation';

should();
const source = (identity = '') => ['policy Access', '  require role "Automation"', 'module M', '  feature F', '    slice Automation S', '      event E', '      command C', '        authorize Access', '      reaction R', ...(identity ? [`        ${identity}`] : []), '        when E', '          invokes C'];

describe('when authoring reaction identity', () => {
    it('should complete identity directly in the reaction', () => {
        const plan = planCompletions(['reaction R', '  '], 1, '  ');
        (plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : []).should.include('runs as system role');
    });
    it('should explain the executable admission', () => {
        const lines = ['reaction R', '  runs as system role "Automation"'];
        hoverContent(lines, 1, 'runs', 3, 7)!.should.include('ESM v10');
    });
    it('should warn when a gated invocation has no identity', () => {
        validateLines(source()).filter(issue => issue.code === 'PLAY0648').should.have.lengthOf(1);
    });
    it('should suppress callerless warnings with declared identity', () => {
        validateLines(source('runs as system role "Automation"')).filter(issue => issue.code === 'PLAY0648' || issue.code === 'PLAY0557').should.be.empty;
    });
    it('should diagnose malformed identity', () => {
        validateLines(source('runs as Persona')).filter(issue => issue.code === 'PLAY0647').should.have.lengthOf(1);
    });
    it('should diagnose unused identity', () => {
        validateLines(source('runs as system').slice(0, -1)).filter(issue => issue.code === 'PLAY0649').should.have.lengthOf(1);
    });
    it('should preserve supplied CSharp-only findings', () => {
        for (const code of ['PLAY0650', 'PLAY0651', 'PLAY0652']) {
            validateLines(source('runs as system'), { compilerDiagnostics: [{ code, severity: 'warning', message: 'Check identity', location: { line: 10, column: 9 } }] }).map(issue => issue.code).should.include(code);
        }
    });
});

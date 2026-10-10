// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { contextVariableEntries } from '../completion-items';
import { completionEntriesFor } from '../completion-planner';
import { mergeSymbols, scanDocument } from '../symbols';
import { validateLines } from '../validation';

const metadata = ['identity', '  department String from claim "department"'];

describe('when authoring identity metadata', () => {
    it('should offer identity once at the top level', () => {
        completionEntriesFor([]).map(entry => entry.label).should.contain('identity');
        completionEntriesFor([], { headers: [], document: metadata }).map(entry => entry.label).should.not.contain('identity');
    });
    it('should offer every source form inside the block', () => {
        completionEntriesFor(['identity']).map(entry => entry.label).should.include.members(['detail from claim', 'detail from query', 'detail from code', 'detail from file']);
    });
    it('should add declared details to caller completion', () => {
        contextVariableEntries(scanDocument(metadata)).map(entry => entry.label).should.contain('$identity.department');
    });
    it('should complete details from another document', () => {
        contextVariableEntries(mergeSymbols(scanDocument(metadata), scanDocument(['policy P']))).map(entry => entry.label).should.contain('$identity.department');
    });
    it('should accept declared details but leave context identity built-ins only', () => {
        const policy = ['policy P', '  require claim "x" matches $identity.department'];
        validateLines([...policy, ...metadata]).filter(issue => issue.code === 'PLAY0155').should.deep.equal([]);
        validateLines([...policy.map(line => line.replace('$identity.', '$context.identity.')), ...metadata]).filter(issue => issue.code === 'PLAY0155').should.have.lengthOf(1);
    });
    it('should reject invalid source bodies with the compiler code', () => {
        validateLines([...metadata, '    cache forever']).map(issue => issue.code).should.contain('PLAY0638');
    });
    it('should reject a missing source with the compiler code', () => {
        validateLines(['identity', '  department String']).map(issue => issue.code).should.contain('PLAY0637');
    });
    it('should reject built-in redeclarations with the compiler code', () => {
        validateLines(['identity', '  id String from claim "id"']).map(issue => issue.code).should.contain('PLAY0640');
    });
});

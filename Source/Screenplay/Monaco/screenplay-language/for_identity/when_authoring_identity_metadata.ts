// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { contextVariableEntries } from '../completion-items';
import { completionEntriesFor } from '../completion-planner';
import { mergeSymbols, scanDocument } from '../symbols';
import { validateLines } from '../validation';
import { hoverContent } from '../hover-content';

const metadata = ['identity', '  department String from claim "department"'];
const agents = ['agents', '  tokens personal', '  query All', '    scope read', '    description "Read"'];
const agentModel = ['policy Staff', '  require role "Staff"', 'module Sales', '  authorize Staff', '  feature Listing', '    slice StateView List', '      readmodel Invoice', '        id String', '      query All => Invoice[]'];

describe('when authoring agent exposure', () => {
    it('should offer the singleton only when absent', () => {
        completionEntriesFor([]).map(entry => entry.label).should.contain('agents');
        completionEntriesFor([], { headers: [], document: agents }).map(entry => entry.label).should.not.contain('agents');
    });
    it('should complete allowlist directives rather than command handlers', () => {
        completionEntriesFor(['agents']).map(entry => entry.label).should.include.members(['tokens personal', 'query', 'command']);
        completionEntriesFor(['command', 'agents']).map(entry => entry.label).should.deep.equal(['scope write', 'description']);
        completionEntriesFor(['query', 'agents']).map(entry => entry.label).should.include.members(['scope read', 'scope write', 'description']);
        completionEntriesFor(['module']).map(entry => entry.label).should.not.contain('agents');
    });
    it('should explain personal tokens, existing gates and preview refusal', () => {
        const docs = hoverContent(agents, 1, 'tokens', 3, 9)!;
        docs.should.contain('personal');
        docs.should.contain('authorization');
        docs.should.contain('PLAY0268');
        docs.should.contain('refused');
        (hoverContent(['readmodel Invoice', '  scope String'], 1, 'scope', 3, 8) === docs).should.equal(false);
    });
    it.each([
        ['tokens personal', 'tokens oauth', 'PLAY0973'],
        ['tokens personal', '', 'PLAY0972'],
        ['scope read', 'scope invalid', 'PLAY0970'],
        ['description "Read"', '', 'PLAY0969'],
        ['query All', 'query Missing', 'PLAY0966'],
        ['scope read', 'approval manual\n    scope read', 'PLAY0974'],
        ['tokens personal', 'tokens personal\n  scope global', 'PLAY0975'],
    ])('should surface compiler refusal %s -> %s (%s)', (original, replacement, code) => {
        validateLines([...agentModel, ...agents.join('\n').replace(original, replacement).split('\n')]).map(issue => issue.code).should.contain(code);
    });
    it('should surface duplicate block and empty surface diagnostics', () => {
        validateLines([...agentModel, ...agents, ...agents]).map(issue => issue.code).should.contain('PLAY0965');
        validateLines([...agentModel, 'agents', '  tokens personal']).map(issue => issue.code).should.include.members(['PLAY0976', 'PLAY0977']);
    });
    it('should surface ungated and global-query diagnostics', () => {
        validateLines([...agentModel.filter(line => line !== '  authorize Staff'), ...agents]).map(issue => issue.code).should.contain('PLAY0980');
        validateLines([...agentModel, '        scoped to global', ...agents]).map(issue => issue.code).should.contain('PLAY0981');
    });
});

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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { expect } from 'chai';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { mergeDocuments } from '../Files/PlayFolderMerge';
import { parse } from '../ScreenplayCompiler';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const model = `policy Staff
  claim staff
module Sales
  authorize Staff
  feature Invoices
    slice StateView Listing
      readmodel Invoice
        id String
      query All => Invoice[]
    slice StateChange Drafting
      command Issue
        id String identifier`;
const allowlist = `agents
  tokens personal
  query Sales.Invoices.Listing.All
    scope read
    description "Read invoices"
  command Sales.Invoices.Drafting.Issue
    scope write
    description "Issue an invoice"`;

describe('when compiling agents', () => {
    it('should keep an explicit allowlist', () => {
        const result = parse(`${model}\n${allowlist}`);
        expect(result.success).to.be.true;
        expect(result.value.agents!.tokens!.syntaxKind).to.equal('Personal');
        expect(result.value.agents!.operations).to.have.lengthOf(2);
        expect(toSyntaxJson(result.value)).to.have.property('agents');
        expect(result.value.agents!.operations[0].description).to.equal('Read invoices');
        expect(result.value.modules[0].features[0].slices[0].queries[0].authorize).to.be.null;
    });

    it('should leave absence absent', () => {
        const result = parse(model);
        expect(result.success).to.be.true;
        expect(result.value.agents == null).to.be.true;
        expect(toSyntaxJson(result.value)).not.to.have.property('agents');
        expect(result.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.UnexposedAgentOperations)).to.be.false;
    });

    it('should warn but accept an explicit empty surface', () => {
        const result = parse(`${model}\nagents\n  tokens personal`);
        expect(result.success).to.be.true;
        expect(result.value.agents!.operations).to.have.lengthOf(0);
        expect(result.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.AgentsExposeNothing)).to.be.true;
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.UnexposedAgentOperations)).to.have.lengthOf(1);
    });

    it('should warn without granting authorization', () => {
        const result = parse(`${model.replace('  authorize Staff\n', '')}\n${allowlist}`);
        expect(result.success).to.be.true;
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.UngatedAgentOperation && diagnostic.severity === 'warning')).to.have.lengthOf(2);
    });

    it('should resolve short unambiguous references', () => {
        expect(parse(`${model}\n${allowlist.replace('Sales.Invoices.Listing.All', 'All').replace('Sales.Invoices.Drafting.Issue', 'Issue')}`).success).to.be.true;
    });

    it('should refuse a performed query without portable identity', () => {
        const performed = model.replace('      query All => Invoice[]', '      query All => Invoice[]\n        performer\n          ```csharp\n          return [];\n          ```');
        assertRefusal(performed, 'performed query');
    });

    it('should refuse a graph query without portable identity', () => {
        const graph = model.replace('      query All => Invoice[]', `      graph Directory
        node InvoiceNode
          id String key
      query All => Invoice[] graph Directory
        match invoice:InvoiceNode
        return
          id = invoice.id`);
        assertRefusal(graph, 'graph query');
    });

    it('should refuse a global query', () => {
        const result = parse(`${model.replace('      query All => Invoice[]', '      query All => Invoice[]\n        scoped to global')}\n${allowlist}`);
        expect(result.success).to.be.false;
        expect(result.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.AgentGlobalQueryRefused)).to.be.true;
    });

    it('should refuse invalid exposure forms', () => {
        const cases: readonly [string, string][] = [
            [allowlist.replace('tokens personal', 'tokens oauth'), DiagnosticCodes.UnsupportedAgentTokenKind],
            [allowlist.replace('  tokens personal\n', ''), DiagnosticCodes.AgentTokensMissing],
            [allowlist.replace('    scope read\n', ''), DiagnosticCodes.AgentOperationMissingScope],
            [allowlist.replace('    description "Read invoices"\n', ''), DiagnosticCodes.AgentOperationMissingDescription],
            [allowlist.replace('scope write', 'scope read'), DiagnosticCodes.AgentCommandRequiresWriteScope],
            [allowlist.replace('Sales.Invoices.Listing.All', 'Missing'), DiagnosticCodes.UnknownAgentOperation],
            [allowlist.replace('  tokens personal', '  tokens personal\n  scope read'), DiagnosticCodes.GlobalAgentScope],
            [allowlist.replace('    scope read', '    approval manual\n    scope read'), DiagnosticCodes.AgentApprovalNotSupported],
            [`${allowlist}\n  query All\n    scope read\n    description "Repeated"`, DiagnosticCodes.DuplicateAgentOperation],
            [`${allowlist}\n${allowlist}`, DiagnosticCodes.DuplicateAgentsDeclaration]
        ];
        for (const [block, code] of cases) {
            const result = parse(`${model}\n${block}`);
            expect(result.success).to.be.false;
            expect(result.diagnostics.some(diagnostic => diagnostic.code === code), code).to.be.true;
        }
    });

    it('should refuse duplicate blocks across documents', () => {
        const source = `${model}\n${allowlist}`;
        const result = mergeDocuments([parse(source), parse(source)]);
        expect(result.success).to.be.false;
        expect(result.diagnostics.some(diagnostic => diagnostic.code === DiagnosticCodes.DuplicateAgentsDeclaration)).to.be.true;
    });
});

function assertRefusal(source: string, kind: string): void {
    expect(parse(source).success).to.be.true;
    const result = parse(`${source}\n${allowlist}`);
    expect(result.success).to.be.false;
    const diagnostic = result.diagnostics.find(diagnostic => diagnostic.code === DiagnosticCodes.UnsupportedAgentQueryKind)!;
    expect(diagnostic).to.not.be.undefined;
    expect(diagnostic.message).to.equal(`Query 'Sales.Invoices.Listing.All' is a ${kind} and cannot be exposed to agents yet`);
}

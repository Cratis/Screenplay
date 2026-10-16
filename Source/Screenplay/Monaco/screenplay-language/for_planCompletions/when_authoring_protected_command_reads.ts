// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { topLevelItems } from '../completion-items';
import { planCompletions } from '../completion-planner';
import { diagnosticCodes } from '../diagnostic-codes';
import { hoverContent } from '../hover-content';
import { blockHeaderPattern, languageConfiguration } from '../language';
import { responseAnalysis } from '../response-analysis';
import { scanDocument } from '../symbols';
import { validateLines } from '../validation';

describe('when authoring protected command reads', () => {
    it('should offer the explicit root mode and explain conditional admission', () => {
        const item = topLevelItems.find(entry => entry.label === 'command reads protected')!;
        item.insertText.should.equal('command reads protected');
        item.documentation.should.contain('ESM v25');
        hoverContent(['command reads protected'], 0, 'protected', 14, 23)!.should.contain('Conflict/DecisionStateChanged');
    });

    it.each(['command reads protected', 'command  reads  protected // consistency'])('should not indent the preamble %s as a command body', line => {
        blockHeaderPattern.test(line).should.be.false;
        languageConfiguration.indentationRules!.increaseIndentPattern.test(line).should.be.false;
    });

    it.each(['command Submit', 'command reads', 'command reads // contextual name'])('should keep the ordinary command body in %s', line => {
        blockHeaderPattern.test(line).should.be.true;
        languageConfiguration.indentationRules!.increaseIndentPattern.test(line).should.be.true;
        responseAnalysis([line, '  value String']).commands.get(0)!.name.should.equal(line.split(' ')[1]);
    });

    it.each([
        ['command reads protected', 'command reads protected'],
        ['command reads protected', 'command reads legacy'],
        ['command reads unknown'],
    ])('should forward declaration diagnostics in %j', (...lines: string[]) => {
        validateLines(lines).map(issue => issue.code).should.contain(diagnosticCodes.invalidCommandReadsDeclaration);
    });

    const model = ['command reads protected', 'module M', '  feature F', '    slice StateChange S', '      readmodel View', '        id String key', '        value String', '      command Update', '        id String identifier', '        reads View as current by id', '        validate'];

    it.each(['          require ', '          require current.', '          require current.value == '])('should complete declared read paths after %s', before => {
        const lines = [...model, before];
        const plan = planCompletions(lines, lines.length - 1, before);
        plan.kind.should.equal('entries');
        if (plan.kind === 'entries') plan.entries.map(entry => entry.label).should.contain('current.value');
    });

    it('should insert only the property after a typed read prefix', () => {
        const lines = [...model, '          require current.v'];
        const plan = planCompletions(lines, lines.length - 1, lines.at(-1)!);
        if (plan.kind !== 'entries') throw new Error('Expected read completions');
        plan.entries.find(entry => entry.label === 'current.value')!.insertText.should.equal('value');
    });

    it('should complete read paths in production mappings', () => {
        const lines = [...model.slice(0, -1), '        produces Recorded', '          value = '];
        const plan = planCompletions(lines, lines.length - 1, lines.at(-1)!);
        if (plan.kind !== 'entries') throw new Error('Expected read completions');
        plan.entries.map(entry => entry.label).should.contain('current.value');
    });

    it('should not offer executable read paths without the preamble', () => {
        const lines = [...model.slice(1), '          require '];
        const plan = planCompletions(lines, lines.length - 1, lines.at(-1)!);
        if (plan.kind === 'entries') plan.entries.map(entry => entry.label).should.not.contain('current.value');
    });

    it('should inherit the preamble from the application document', () => {
        const lines = [...model.slice(1), '          require '];
        const symbols = scanDocument(lines);
        symbols.authoringSources = ['command reads protected'];
        const plan = planCompletions(lines, lines.length - 1, lines.at(-1)!, symbols);
        if (plan.kind !== 'entries') throw new Error('Expected read completions');
        plan.entries.map(entry => entry.label).should.contain('current.value');
    });

    it('should not offer an ambiguous unaliased view name', () => {
        const lines = [...model.slice(0, -2), '        reads View by id', '        reads View as other by id', '        validate', '          require '];
        const plan = planCompletions(lines, lines.length - 1, lines.at(-1)!);
        if (plan.kind !== 'entries') throw new Error('Expected read completions');
        plan.entries.map(entry => entry.label).should.not.contain('View.value');
        plan.entries.map(entry => entry.label).should.contain('other.value');
    });

    it('should keep the mode global beside an isolated command fragment', () => {
        const lines = ['command reads protected', 'command Submit', '  value String'];
        const analysis = responseAnalysis(lines);
        analysis.commands.get(1)!.name.should.equal('Submit');
        analysis.diagnostics.map(diagnostic => diagnostic.code).should.not.contain(diagnosticCodes.invalidCommandReadsDeclaration);
    });
});

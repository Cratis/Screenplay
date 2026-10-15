// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { topLevelItems } from '../completion-items';
import { diagnosticCodes } from '../diagnostic-codes';
import { hoverContent } from '../hover-content';
import { blockHeaderPattern, languageConfiguration } from '../language';
import { responseAnalysis } from '../response-analysis';
import { validateLines } from '../validation';

describe('when authoring protected command reads', () => {
    it('should offer the explicit root mode without promising execution', () => {
        const item = topLevelItems.find(entry => entry.label === 'command reads protected')!;
        item.insertText.should.equal('command reads protected');
        item.documentation.should.contain('not yet admitted');
        hoverContent(['command reads protected'], 0, 'protected', 14, 23)!.should.contain('not yet admitted');
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

    it('should keep the mode global beside an isolated command fragment', () => {
        const lines = ['command reads protected', 'command Submit', '  value String'];
        const analysis = responseAnalysis(lines);
        analysis.commands.get(1)!.name.should.equal('Submit');
        analysis.diagnostics.map(diagnostic => diagnostic.code).should.not.contain(diagnosticCodes.invalidCommandReadsDeclaration);
    });
});

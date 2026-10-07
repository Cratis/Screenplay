// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CompilationResult } from '../ScreenplayCompiler';
import { ScreenGuardedActionSyntax } from '../Syntax/Screens';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { ApplicationSyntax } from '../Syntax/Structure';
import { a_parsed_document } from './given/a_parsed_document';

class RecordingWalker extends ScreenplaySyntaxWalker {
    readonly kinds: string[] = [];
    override visitNode(node: SyntaxNode): void { this.kinds.push(node.kind); }
}

describe.each(['', 'numbers exact'])('when rejecting unsupported guarded condition characters with %s', preamble => {
    it.each([
        ['["open"]', '['],
        ['"open";', ';'],
        ['{"open"', '{'],
    ])('should reject %s instead of changing the condition', (operand, character) => {
        const result = a_parsed_document(...(preamble ? [preamble] : []), 'module M', '  feature F', '    slice StateView S', '      screen Details',
            '        action "Again"', `          when item.status == ${operand} execute Retry`);
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([
            DiagnosticCodes.UnsupportedActionConditionOperand, DiagnosticCodes.GuardedActionWithoutAlternatives,
        ]);
        result.diagnostics[0].message.should.equal(`Guarded action conditions contain unsupported character '${character}'`);
    });
    it('should accept dotnet whitespace separators', () => {
        const result = a_parsed_document(...(preamble ? [preamble] : []), 'module M', '  feature F', '    slice StateView S', '      screen Details',
            '        action "Again"', '          when item.status\u0085==\u0085"open" execute Retry');
        result.diagnostics.should.deep.equal([]);
    });
    it('should reject a byte order mark as a separator', () => {
        const result = a_parsed_document(...(preamble ? [preamble] : []), 'module M', '  feature F', '    slice StateView S', '      screen Details',
            '        action "Again"', '          when item.status\ufeff==\ufeff"open" execute Retry');
        result.diagnostics.map(diagnostic => diagnostic.code).should.deep.equal([
            DiagnosticCodes.UnsupportedActionConditionOperand, DiagnosticCodes.GuardedActionWithoutAlternatives,
        ]);
        result.diagnostics[0].message.should.equal("Guarded action conditions contain unsupported character '\ufeff'");
    });
    it('should preserve punctuation inside a recognized string literal', () => {
        const result = a_parsed_document(...(preamble ? [preamble] : []), 'module M', '  feature F', '    slice StateView S', '      screen Details',
            '        action "Again"', '          when item.status == "[open];{closed}" execute Retry');
        result.diagnostics.should.deep.equal([]);
    });
});

describe('when parsing exact guarded condition numbers', () => {
    it('should accept a dotnet whitespace separator after an exponent', () => {
        const result = a_parsed_document('numbers exact', 'module M', '  feature F', '    slice StateView S', '      screen Details',
            '        action "Again"', '          when item.count >= 1e+2\u0085and item.count < 2e+2 execute Retry');
        result.diagnostics.should.deep.equal([]);
    });
});

describe('when parsing guarded actions', () => {
    let result: CompilationResult<ApplicationSyntax>;
    let action: ScreenGuardedActionSyntax;
    let walker: RecordingWalker;
    beforeEach(() => {
        result = a_parsed_document('module M', '  feature F', '    slice StateView S', '      screen Details',
            '        action "Read \\"source\\" again"',
            '          when item.status == "open" and item.answeredAt == null execute Release',
            '            with id from item.id',
            '          when item.note contains "execute Retry" execute Retry',
            '          otherwise execute Start',
            '            with id from item.id',
            '          navigate to List by id',
            '        action $strings.actions.close',
            '          when item.ready == true execute Close',
            '          otherwise hidden',
            '        action "Continue"',
            '          when item.ready != false execute Continue');
        action = result.value.modules[0].features[0].slices[0].screens[0].directives[0] as ScreenGuardedActionSyntax;
        walker = new RecordingWalker();
        walker.visitScreenDirective(action);
    });
    it('should report nothing', () => result.diagnostics.should.deep.equal([]));
    it('should unescape the label', () => action.label.should.equal('Read "source" again'));
    it('should preserve alternative order and arguments', () => {
        action.alternatives.map(node => node.command).should.deep.equal(['Release', 'Retry']);
        action.alternatives[0].arguments.map(node => [node.name, node.binding]).should.deep.equal([['id', 'item.id']]);
    });
    it('should read fallback arguments and navigation', () => {
        [action.otherwise?.outcome, action.otherwise?.command, action.otherwise?.arguments[0].binding, action.navigate?.screen, action.navigate?.by]
            .should.deep.equal(['Execute', 'Start', 'item.id', 'List', 'id']);
    });
    it('should retain localized labels and explicit hidden fallback', () => {
        const hidden = result.value.modules[0].features[0].slices[0].screens[0].directives[1] as ScreenGuardedActionSyntax;
        [hidden.label, hidden.otherwise?.outcome, hidden.otherwise?.command, hidden.otherwise?.arguments].should.deep.equal(['$strings.actions.close', 'Hidden', null, []]);
    });
    it('should retain an omitted fallback', () => {
        const omitted = result.value.modules[0].features[0].slices[0].screens[0].directives[2] as ScreenGuardedActionSyntax;
        [omitted.otherwise, omitted.navigate].should.deep.equal([null, null]);
    });
    it('should visit the guarded node once through its specialized entry point', () => {
        const direct = new RecordingWalker();
        direct.visitScreenGuardedAction(action);
        direct.kinds.should.deep.equal(walker.kinds);
    });
    it('should walk conditions arguments fallback and navigation in order', () => walker.kinds.should.deep.equal([
        'ScreenGuardedActionSyntax', 'ScreenActionAlternativeSyntax', 'LogicalConditionSyntax',
        'ComparisonConditionSyntax', 'LiteralExpressionSyntax', 'ComparisonConditionSyntax', 'LiteralExpressionSyntax',
        'InteractionArgumentSyntax', 'ScreenActionAlternativeSyntax', 'ComparisonConditionSyntax', 'LiteralExpressionSyntax',
        'ScreenActionOtherwiseSyntax', 'InteractionArgumentSyntax', 'ScreenNavigateSyntax',
    ]));
});

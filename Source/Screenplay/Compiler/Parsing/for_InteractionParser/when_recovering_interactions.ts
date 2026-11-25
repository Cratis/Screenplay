// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parseForAuthoring } from '../../ScreenplayCompiler';
import { InteractionActionSyntax } from '../../Syntax/Interactions';

function named(body: string) {
    return parseForAuthoring(`numbers exact\nbehavior Recovery\n${body}\n  on leave\n    navigate back\nscreen Sibling\n  title "Still here"\n`);
}

function actions(body: string) { return named(body).value.behaviors![0].bindings[0].actions; }

function diagnostics(body: string, code: string): number {
    return named(body).diagnostics.filter(diagnostic => diagnostic.code === code).length;
}

describe('when recovering interactions', () => {
    it('should skip an unusable binding header and its body without consuming the next binding or screen', () => {
        const result = named('  on interval every\n    execute Hidden');
        result.value.behaviors![0].bindings.map(binding => binding.trigger.kind).should.deep.equal(['BuiltInInteractionTriggerSyntax']);
        result.value.screens![0].name.should.equal('Sibling');
    });

    it('should preserve an empty binding and the following sibling binding', () => {
        const behavior = named('  on click').value.behaviors![0];
        behavior.bindings.should.have.lengthOf(2);
        behavior.bindings[0].actions.should.deep.equal([]);
        behavior.bindings[1].actions[0].kind.should.equal('NavigateBackActionSyntax');
    });

    it('should keep valid metadata after malformed and repeated metadata without consuming bindings', () => {
        const behavior = named('  description "First"\n  description "Second"\n  order 4\n  order invalid\n  parameter target String\n  parameter target Other\n  parameter !\n  on click\n    navigate back').value.behaviors![0];
        behavior.description!.should.equal('First');
        behavior.order!.should.equal(4);
        behavior.parameters.map(parameter => parameter.type!.name).should.deep.equal(['String']);
        behavior.bindings.should.have.lengthOf(2);
    });

    it('should preserve the first opaque guard and the next action after a repeated guard', () => {
        const body = '  on click\n    where item.first\n    where item.second\n    navigate back';
        named(body).value.behaviors![0].bindings[0].condition!.should.equal('item.first');
        actions(body)[0].kind.should.equal('NavigateBackActionSyntax');
        diagnostics(body, DiagnosticCodes.LegacyInteractionWhere).should.equal(2);
    });

    it('should skip unknown actions and malformed action arguments without consuming following actions', () => {
        const parsed = actions('  on click\n    nonsense action\n      with hidden from item.hidden\n    execute Save\n      with broken\n      with id from item.id\n    navigate back');
        parsed.map(action => action.kind).should.deep.equal(['ExecuteCommandActionSyntax', 'NavigateBackActionSyntax']);
        parsed[0].arguments.map(argument => argument.name).should.deep.equal(['id']);
    });

    it('should retain valid uses assignments after a malformed assignment and preserve the next directive', () => {
        const result = parseForAuthoring('numbers exact\nscreen Home\n  uses Missing extra\n    lonely\n    target item.id\n  title "Sibling"');
        const [uses, title] = result.value.screens![0].directives;
        if (uses.kind !== 'ScreenUsesBehaviorSyntax') throw new Error('Missing uses wrapper');
        uses.uses.behavior.should.equal('');
        uses.uses.arguments.map(argument => [argument.name, argument.value]).should.deep.equal([['target', 'item.id']]);
        title.kind.should.equal('ScreenTitleSyntax');
    });

    it('should retain unsupported alternatives while reporting their existing diagnostic once', () => {
        const body = '  on load\n    when item.enabled\n      navigate back';
        named(body).value.behaviors![0].bindings[0].alternatives.should.have.lengthOf(1);
        diagnostics(body, DiagnosticCodes.UnsupportedInteractionAlternatives).should.equal(1);
    });

    it('should preserve mixed flat actions and alternatives and report the conflict once', () => {
        const body = '  on click\n    navigate back\n    when item.enabled\n      refresh Items';
        actions(body).should.have.lengthOf(1);
        named(body).value.behaviors![0].bindings[0].alternatives.should.have.lengthOf(1);
        diagnostics(body, DiagnosticCodes.MixedInteractionAlternatives).should.equal(1);
    });

    it('should retain empty alternatives without swallowing the fallback and following bindings', () => {
        const body = '  on click\n    when item.enabled\n    otherwise\n      navigate back';
        const binding = named(body).value.behaviors![0].bindings[0];
        binding.alternatives[0].actions.should.deep.equal([]);
        binding.otherwise!.actions[0].kind.should.equal('NavigateBackActionSyntax');
        diagnostics(body, DiagnosticCodes.InteractionAlternativeWithoutActions).should.equal(1);
    });

    it('should recover misplaced and duplicate fallbacks with single-counted diagnostics', () => {
        const body = '  on click\n    otherwise\n      navigate back\n    when item.enabled\n      refresh Items\n    otherwise\n      close dialog';
        const binding = named(body).value.behaviors![0].bindings[0];
        binding.alternatives.should.have.lengthOf(1);
        binding.otherwise!.actions[0].kind.should.equal('CloseDialogActionSyntax');
        diagnostics(body, DiagnosticCodes.MisplacedActionOtherwise).should.equal(2);
    });

    it('should reject inline alternative shorthand once without consuming the next binding', () => {
        const body = '  on click\n    when item.enabled execute Save';
        named(body).value.behaviors![0].bindings.should.have.lengthOf(2);
        diagnostics(body, DiagnosticCodes.InlineInteractionAlternative).should.equal(1);
    });

    it('should discard outcome-less and unsupported continuations without consuming following actions', () => {
        const parsed = actions('  on click\n    on success\n      execute Hidden\n    navigate back\n      on failure\n        execute Hidden\n    refresh Items');
        parsed.map(action => action.kind).should.deep.equal(['NavigateBackActionSyntax', 'RefreshQueryActionSyntax']);
        parsed[0].onFailure.should.deep.equal([]);
    });

    it('should stop recursive action retention at the native limit and preserve the next binding', () => {
        const nested = Array.from({ length: 20 }, (_, depth) => `${' '.repeat(4 + depth * 4)}execute Save\n${' '.repeat(6 + depth * 4)}on success`).join('\n');
        const behavior = named(`  on click\n${nested}`).value.behaviors![0];
        let retained: readonly InteractionActionSyntax[] = behavior.bindings[0].actions;
        let depth = 0;
        while (retained.length > 0) { depth++; retained = retained[0].onSuccess; }
        depth.should.equal(16);
        behavior.bindings[1].actions[0].kind.should.equal('NavigateBackActionSyntax');
    });

    it('should preserve row-click navigation beside ordinary table interactions', () => {
        const result = parseForAuthoring('numbers exact\nscreen Home\n  table Item\n    on row-click navigate to Details\n    on click\n      execute Save\n    uses Selection\n      id item.id\n  title "Sibling"');
        const [table, title] = result.value.screens![0].directives;
        if (table.kind !== 'ScreenTableSyntax') throw new Error('Missing table');
        table.rowClick!.screen.should.equal('Details');
        table.behaviors![0].bindings[0].actions[0].kind.should.equal('ExecuteCommandActionSyntax');
        table.usedBehaviors![0].arguments[0].value.should.equal('item.id');
        title.kind.should.equal('ScreenTitleSyntax');
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeAll, describe, it } from 'vitest';
import { DiagnosticCodes } from '../../Diagnostics/DiagnosticCodes';
import { parse, parseForAuthoring } from '../../ScreenplayCompiler';
import { BehaviorSyntax } from '../../Syntax/Interactions';
import { SyntaxJsonValue, toSyntaxJson } from '../../Syntax/SyntaxJson';
import { assertExactSyntaxRoundTrip } from './given/assert_exact_syntax_round_trip';
import { decodeExactSyntaxJson } from '../../Syntax/StrictSyntaxJson';

const source = `numbers exact
behavior Feedback
  on click
    notify info "Saved \\"today\\""
    notify warning item.warning
    notify error $strings.failure
    confirm "Continue?"
    confirm item.question
    confirm $strings.question
`;

describe('when retaining interaction values', () => {
    let behavior: BehaviorSyntax;
    let json: string;

    beforeAll(() => {
        const application = parse(source).value;
        behavior = application.behaviors![0];
        json = JSON.stringify(toSyntaxJson(application));
    });

    it('should distinguish quoted messages from binding expressions and string references', () => {
        behavior.bindings[0].actions.map(action => {
            if (action.kind !== 'NotifyActionSyntax' && action.kind !== 'ConfirmActionSyntax') throw new Error('Missing message action');
            return [action.message, action.messageIsLiteral];
        }).should.deep.equal([['Saved "today"', true], ['item.warning', false], ['$strings.failure', false], ['Continue?', true], ['item.question', false], ['$strings.question', false]]);
    });

    it('should retain notification levels separately from the node kind', () => {
        behavior.bindings[0].actions.slice(0, 3).map(action => action.kind === 'NotifyActionSyntax' ? action.level : null).should.deep.equal(['Info', 'Warning', 'Error']);
    });

    it('should refuse an unknown trigger enum member in Exact JSON', () => {
        const invalid = json.replace('"syntaxKind":"Click"', '"syntaxKind":"NotATrigger"');
        invalid.should.not.equal(json);
        (() => decodeExactSyntaxJson(invalid)).should.throw();
    });

    it('should preserve the native Unknown trigger sentinel in Exact JSON', () => {
        const unknown = json.replace('"syntaxKind":"Click"', '"syntaxKind":"Unknown"');
        unknown.should.not.equal(json);
        assertExactSyntaxRoundTrip(JSON.parse(unknown) as SyntaxJsonValue);
    });

    it('should refuse unknown action members in Exact JSON', () => {
        (() => decodeExactSyntaxJson(json.replace('"kind":"ConfirmActionSyntax"', '"kind":"ConfirmActionSyntax","unexpected":true'))).should.throw();
    });

    it('should refuse a mistyped literal marker in Exact JSON', () => {
        (() => decodeExactSyntaxJson(json.replace('"messageIsLiteral":true', '"messageIsLiteral":"true"'))).should.throw();
    });

    it('should leave existing guard diagnostics and authoring facts single-counted in both modes', () => {
        const guarded = `behavior Selection
  on click
    where item.enabled
    when item.status == "ready"
      execute Save
    otherwise
      notify info "Waiting"
module Work
  screen template Workspace
    uses Selection
      target item.id
`;
        for (const preamble of ['', 'numbers exact\n']) {
            const result = parseForAuthoring(preamble + guarded);
            result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.LegacyInteractionWhere).should.have.lengthOf(1);
            result.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.MixedInteractionAlternatives).should.have.lengthOf(1);
            result.interactionBehaviors.should.have.lengthOf(1);
            result.interactionUses.should.have.lengthOf(1);
            result.guardedInteractions.should.have.lengthOf(1);
            result.interactionUses[0].arguments.should.deep.equal([{ name: 'target', value: 'item.id' }]);
        }
    });
});

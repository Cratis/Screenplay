// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { ExpressionSyntax } from '../../Syntax/Expressions';
import { PolicyConditionSyntax } from '../../Syntax/Policies';
import { personaDiagnostics } from '../PersonaCompleteness';

const location = { line: 2, column: 3 };
const role = (role: string): PolicyConditionSyntax => ({ kind: 'RoleConditionSyntax', role, location });
const literal = (value: string | number): ExpressionSyntax => ({ kind: 'LiteralExpressionSyntax', value, location });
const cases: readonly { name: string; target: ExpressionSyntax; text: string }[] = [
    { name: 'exact numeric literal', target: { kind: 'LiteralExpressionSyntax', value: { literalType: 'ExactNumber', value: '0.1234567890123456789012345678' }, location }, text: '0.1234567890123456789012345678' },
    { name: 'causation root', target: { kind: 'CausedByExpressionSyntax', property: null, location }, text: '$causedBy' },
    { name: 'causation property', target: { kind: 'CausedByExpressionSyntax', property: 'subject', location }, text: '$causedBy.subject' },
    { name: 'refusal member', target: { kind: 'RefusalExpressionSyntax', member: 'message', location }, text: '$refusal.message' },
    { name: 'case parameter', target: { kind: 'CaseValueExpressionSyntax', parameter: 'owner', location }, text: 'case.owner' },
    {
        name: 'interpolated template',
        target: { kind: 'TemplateExpressionSyntax', parts: [
            { kind: 'TemplateTextSyntax', text: 'owner: ', location },
            { kind: 'TemplateInterpolationSyntax', expression: { kind: 'PathExpressionSyntax', path: 'owner', location }, location },
        ], location },
        text: '`owner: ${owner}`',
    },
    { name: 'structured list', target: { kind: 'ListExpressionSyntax', items: [literal('a\n"b'), literal(42)], location }, text: '["a\\n\\"b",42]' },
    {
        name: 'structured object',
        target: { kind: 'ObjectExpressionSyntax', members: [
            { kind: 'ObjectMemberSyntax', name: 'owner"name', value: literal('ops\tteam'), location },
            { kind: 'ObjectMemberSyntax', name: 'count', value: literal(2), location },
        ], location },
        text: '{"owner\\"name":"ops\\tteam","count":2}',
    },
];

// These target node kinds are supplied directly through the typed syntax API rather than the
// policy parser's raw-expression fallback, which is already held by the shared source vectors.
describe('when formatting typed claim targets in persona alternatives', () => {
    for (const { name, target, text } of cases) {
        it(`should preserve the ${name} in the unchosen buildable alternative`, () => {
            const compilation = parse(`policy Member
  require role "A"
persona Person
  policy Member
module M
  feature F
    slice StateChange S
      command C
        authorize Member`);
            compilation.success.should.equal(true);
            const condition: PolicyConditionSyntax = {
                kind: 'LogicalPolicyConditionSyntax', operator: 'Or', left: role('A'), location,
                right: {
                    kind: 'LogicalPolicyConditionSyntax', operator: 'Or', right: role('B'), location,
                    left: { kind: 'ClaimConditionSyntax', claim: 'owner', matchesSubject: false, matches: target, location },
                },
            };
            const diagnostics = personaDiagnostics({
                ...compilation.value,
                policies: [{ ...compilation.value.policies![0], condition }],
            });
            diagnostics.should.deep.equal([{
                code: 'PLAY0576', severity: 'information', location,
                message: `Persona 'Person' policy 'Member' has an unchosen buildable alternative 'claim "owner" matches ${text} or role "B"'; add a policy that pins it.`,
            }]);
        });
    }
});

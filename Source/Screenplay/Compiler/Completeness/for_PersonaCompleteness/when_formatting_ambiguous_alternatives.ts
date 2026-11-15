// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { PolicyConditionSyntax } from '../../Syntax/Policies';
import { personaDiagnostics } from '../PersonaCompleteness';

const location = { line: 2, column: 3 };
const role = (role: string): PolicyConditionSyntax => ({ kind: 'RoleConditionSyntax', role, location });
const anonymous: PolicyConditionSyntax = { kind: 'AnonymousConditionSyntax', location };
const authenticated: PolicyConditionSyntax = { kind: 'AuthenticatedConditionSyntax', location };
const claim = (name: string): PolicyConditionSyntax => ({ kind: 'ClaimConditionSyntax', claim: name, matchesSubject: false, matches: { kind: 'LiteralExpressionSyntax', value: 'x', location }, location });
const logical = (operator: 'And' | 'Or', left: PolicyConditionSyntax, right: PolicyConditionSyntax): PolicyConditionSyntax => ({ kind: 'LogicalPolicyConditionSyntax', operator, left, right, location });

const cases: readonly { name: string; policy: PolicyConditionSyntax; text: string }[] = [
    { name: 'a right alternative that is a role', policy: logical('Or', role('A'), role('B')), text: 'role "B"' },
    { name: 'a right alternative that is anonymous', policy: logical('Or', role('A'), anonymous), text: 'anonymous' },
    { name: 'a right alternative that is authenticated', policy: logical('Or', role('A'), authenticated), text: 'authenticated' },
    { name: 'a right alternative that is a literal claim', policy: logical('Or', role('A'), claim('owner')), text: 'claim "owner" matches "x"' },
    { name: 'a right alternative that is a subject claim', policy: logical('Or', role('A'), { kind: 'ClaimConditionSyntax', claim: 'owner', matchesSubject: true, matches: null, location }), text: 'claim "owner" matches subject' },
    { name: 'a right alternative that is a claim without a target', policy: logical('Or', role('A'), { kind: 'ClaimConditionSyntax', claim: 'owner', matchesSubject: false, matches: null, location }), text: 'claim "owner" matches ""' },
    { name: 'a role name that needs escaping', policy: logical('Or', role('A'), role('B"\\')), text: 'role "B\\"\\\\"' },
];

const source = `policy Member
  require role "A"
persona Person
  policy Member
module M
  feature F
    slice StateChange S
      command C
        authorize Member`;

describe('when formatting ambiguous persona alternatives', () => {
    for (const { name, policy, text } of cases) {
        it(`should print ${name}`, () => {
            const compilation = parse(source);
            compilation.success.should.equal(true);
            const diagnostics = personaDiagnostics({
                ...compilation.value,
                policies: [{ ...compilation.value.policies![0], condition: policy }],
            });
            diagnostics.should.deep.equal([{
                code: 'PLAY0576', severity: 'information', location,
                message: `Persona 'Person' policy 'Member' has an unchosen buildable alternative '${text}'; add a policy that pins it.`,
            }]);
        });
    }
});

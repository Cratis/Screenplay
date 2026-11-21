// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse } from '../../../../ScreenplayCompiler';
import { ExpressionSyntax } from '../../../../Syntax/Expressions';
import { PolicyConditionSyntax } from '../../../../Syntax/Policies';
import { personaDiagnostics } from '../../../PersonaCompleteness';

export const location = { line: 2, column: 3 };
const role = (role: string): PolicyConditionSyntax => ({ kind: 'RoleConditionSyntax', role, location });

export function withClaim(target: ExpressionSyntax) {
    const compilation = parse('policy Member\n  require role "A"\npersona Person\n  policy Member\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        authorize Member');
    const condition: PolicyConditionSyntax = {
        kind: 'LogicalPolicyConditionSyntax', operator: 'Or', left: role('A'), location,
        right: {
            kind: 'LogicalPolicyConditionSyntax', operator: 'Or', right: role('B'), location,
            left: { kind: 'ClaimConditionSyntax', claim: 'owner', matchesSubject: false, matches: target, location },
        },
    };
    return personaDiagnostics({ ...compilation.value, policies: [{ ...compilation.value.policies![0], condition }] });
}

export function ambiguity(text: string) {
    return [{
        code: 'PLAY0576', severity: 'information', location,
        message: `Persona 'Person' policy 'Member' has an unchosen buildable alternative 'claim "owner" matches ${text} or role "B"'; add a policy that pins it.`,
    }];
}

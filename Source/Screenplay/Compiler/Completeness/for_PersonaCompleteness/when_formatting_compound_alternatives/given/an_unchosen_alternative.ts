// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse } from '../../../../ScreenplayCompiler';
import { PolicyConditionSyntax } from '../../../../Syntax/Policies';
import { personaDiagnostics } from '../../../PersonaCompleteness';

export const location = { line: 2, column: 3 };
export const role = (role: string): PolicyConditionSyntax => ({ kind: 'RoleConditionSyntax', role, location });
export const logical = (left: PolicyConditionSyntax, operator: 'And' | 'Or', right: PolicyConditionSyntax): PolicyConditionSyntax => ({ kind: 'LogicalPolicyConditionSyntax', left, operator, right, location });

export function withAlternative(alternative: PolicyConditionSyntax) {
    const compilation = parse('policy Member\n  require role "A"\npersona Person\n  policy Member\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        authorize Member');
    return personaDiagnostics({ ...compilation.value, policies: [{ ...compilation.value.policies![0], condition: logical(role('A'), 'Or', alternative) }] });
}

export function ambiguity(text: string) {
    return [{ code: 'PLAY0576', severity: 'information', location,
        message: `Persona 'Person' policy 'Member' has an unchosen buildable alternative '${text}'; add a policy that pins it.` }];
}

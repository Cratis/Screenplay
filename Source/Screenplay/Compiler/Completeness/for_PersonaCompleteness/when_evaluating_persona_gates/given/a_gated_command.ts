// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { parse } from '../../../../ScreenplayCompiler';
import { personaDiagnostics } from '../../../PersonaCompleteness';

export function applicationWithGate(body: string, requirement = 'Required', personaCondition = 'role "A"') {
    return parse(`policy Member\n  require ${personaCondition}\npolicy Required\n${body}\npersona Person\n  policy Member\nmodule M\n  authorize Member\n  feature F\n    slice StateChange S\n      command C\n        authorize ${requirement}`, 'gates.play').value;
}

export function withGate(body: string, requirement = 'Required', personaCondition = 'role "A"') {
    return personaDiagnostics(applicationWithGate(body, requirement, personaCondition));
}

export const denial = [{ code: 'PLAY0575', severity: 'warning',
    message: "Command 'C' is definitely denied to every declared persona's synthesized caller.",
    location: { line: 11, column: 7, path: 'gates.play' } }];

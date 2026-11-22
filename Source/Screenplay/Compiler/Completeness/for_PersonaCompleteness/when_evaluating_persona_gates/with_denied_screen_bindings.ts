// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../../ScreenplayCompiler';
import { personaDiagnostics } from '../../PersonaCompleteness';

describe('when evaluating a screen whose command and query bindings are denied', () => {
    let diagnostics: ReturnType<typeof personaDiagnostics>;

    beforeEach(() => {
        const compilation = parse('policy Member\n  require role "A"\npolicy Restricted\n  require role "Other"\npersona Person\n  policy Member\nmodule M\n  authorize Member\n  feature F\n    slice StateChange S\n      command C\n        authorize Restricted\n      readmodel Rm\n        value String\n      query Q => Rm\n        authorize Restricted\n      screen Locked\n        data Rm via query Q\n        action C', 'screens.play');
        diagnostics = personaDiagnostics(compilation.value);
    });

    it('should preserve command, query and screen diagnostic order and locations', () => diagnostics.should.deep.equal([
        { code: 'PLAY0575', severity: 'warning', message: "Command 'C' is definitely denied to every declared persona's synthesized caller.", location: { line: 11, column: 7, path: 'screens.play' } },
        { code: 'PLAY0575', severity: 'warning', message: "Query 'Q' is definitely denied to every declared persona's synthesized caller.", location: { line: 15, column: 7, path: 'screens.play' } },
        { code: 'PLAY1143', severity: 'warning', message: "Screen 'Locked' offers nothing to any declared persona: every command and query it binds is definitely denied to every synthesized caller.", location: { line: 17, column: 7, path: 'screens.play' } },
    ]));
});

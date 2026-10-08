// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '@cratis/screenplay-compiler';
import { validateLines } from '@cratis/screenplay-language';
import { describe, expect, it } from 'vitest';
import { WorkspaceApplication } from '../WorkspaceApplication';

const source = ['slice Automation Handling', '  event Approved', '  reaction Claimer', '    when Approved', '      invokes Claim', '        on refused by authorization', '          acknowledge'];
const code = DiagnosticCodes.AuthorizationRefusalWithoutIdentity;

describe('when an authorization refusal has no invoking identity in the workspace', () => {
    it('should surface the warning once in the invoking file with the command declared elsewhere', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'policy Access\n  require authenticated\nmodule M\n  authorize Access\n  feature F\n    import "command.play"\n    import "reaction.play"');
        application.set('command.play', 'slice StateChange Claiming\n  command Claim');
        application.set('reaction.play', source.join('\n'));
        const diagnostics = application.diagnosticsFor('reaction.play');
        expect(diagnostics.filter(diagnostic => diagnostic.code === code)).toEqual([
            { code, severity: 'warning', location: { path: 'reaction.play', line: 6, column: 9 }, message: "Command 'Claim' is authorization-gated, but this invocation has no declared identity. This authorization refusal branch always fires in the reference runner because there is no caller; Arc runs reactor commands as the system. Declare an invoking identity once supported (#383)." },
        ]);
        expect(application.diagnosticsFor('command.play').filter(diagnostic => diagnostic.code === code)).toEqual([]);
        const issues = validateLines(source, { application: application.symbolsExcept('reaction.play'), placement: application.placementOf('reaction.play'), path: 'reaction.play', compilerDiagnostics: diagnostics });
        expect(issues.filter(issue => issue.code === code)).toHaveLength(1);
    });
});

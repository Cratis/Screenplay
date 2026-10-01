// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { CompilationResult } from '../../ScreenplayCompiler';
import { PolicyRequirementSyntax } from '../../Syntax/Authorization';
import { ApplicationSyntax } from '../../Syntax/Structure';
import { parseFolder } from '../PlayFolderMerge';

const read = (requirement: PolicyRequirementSyntax): string => requirement.kind === 'PolicyReferenceSyntax'
    ? requirement.name
    : `(${read(requirement.left)} ${requirement.operator.toLowerCase()} ${read(requirement.right)})`;

describe('when merging personas and authorization', () => {
    let result: CompilationResult<ApplicationSyntax>;

    beforeEach(() => {
        result = parseFolder([
            { path: 'b.play', source: 'persona Clerk\npersona Auditor\nmodule A\n  authorize IsAdmin\n  feature F\n    authorize IsClerk' },
            { path: 'a.play', source: 'persona Clerk\nmodule A\n  authorize IsAdmin\n  feature F\n    authorize IsAuditor\n    slice StateChange S' },
        ]);
    });

    it('should keep a persona declared once and report the repeat', () =>
        [result.value.personas.map(persona => persona.name), result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0173').map(diagnostic => diagnostic.location.path)]
            .should.deep.equal([['Clerk', 'Auditor'], ['b.play']]));
    it('should keep a gate repeated in another file once and warn about it', () => {
        read(result.value.modules[0].authorize!.requirement).should.equal('IsAdmin');
        result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0394').map(diagnostic => diagnostic.location.path).should.deep.equal(['b.play']);
    });
    it('should require every distinct gate the files declare', () => read(result.value.modules[0].features[0].authorize!.requirement).should.equal('(IsAuditor and IsClerk)'));
});

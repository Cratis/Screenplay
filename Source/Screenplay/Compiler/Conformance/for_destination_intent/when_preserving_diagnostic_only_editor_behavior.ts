// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { findQuickFixes, isQuickFixDiagnostic } from '../../Authoring/QuickFixes';

const mixed = 'module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        projectId Uuid identifier\n        name String\n        produces event Registered\n          name String = name\n        produces Audited\n          name = name\n        produces when name == "repeat"\n          Audited\n            for projectId\n            name = name\n      event Audited\n        name String\n';

describe('when destination intent is an MCP-only contract', () => {
    it('should still diagnose mixed omissions without an automatic recipe', () => {
        const result = parse(mixed);
        expect(result.diagnostics.filter(item => item.code === 'PLAY0470')).toHaveLength(2);
        expect(isQuickFixDiagnostic('PLAY0470')).toBe(false);
        expect(findQuickFixes(mixed, { diagnosticCode: 'PLAY0470' })).toEqual([]);
    });

    it('should accept the explicitly selected candidate through existing grammar', () => {
        const explicit = mixed.replace('        produces event Registered\n', '        produces event Registered\n          for projectId\n')
            .replace('        produces Audited\n', '        produces Audited\n          for projectId\n');
        const result = parse(explicit);
        expect(result.success).toBe(true);
        expect(result.diagnostics).toEqual([]);
        const command = result.value.modules[0].features[0].slices[0].commands[0];
        expect(command.produces).toHaveLength(3);
        expect(command.produces.map(item => item.for)).toEqual(command.produces.map(() => expect.objectContaining({ kind: 'PathExpressionSyntax', path: 'projectId' })));
    });
});

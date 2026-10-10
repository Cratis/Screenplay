// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { parse } from '../ScreenplayCompiler';

const message = "Command 'Claim' is authorization-gated, but this invocation has no declared identity. This authorization refusal branch always fires in the reference runner because there is no caller. Declare 'runs as system role \"<Role>\"'; Arc runs commands as the system only for a reactor carrying [ExecuteCommandsAsSystem].";
const code = DiagnosticCodes.AuthorizationRefusalWithoutIdentity;

describe('when an authorization refusal has no invoking identity', () => {
    it.each([
        ['module', 'authorization', true], ['feature', 'authorization', true], ['nested', 'authorization', true],
        ['command', 'authorization', true], ['none', 'authorization', false], ['command', 'validation', false],
        ['command', 'constraint', false], ['command', '', false],
    ])('should check the %s gate for a %s branch', (gate, selector, expected) => {
        const source = 'policy Access\n  require not role "Blocked"\nmodule Billing\n' +
            (gate === 'module' ? '  authorize Access\n' : '') +
            '  feature Payments\n' + (gate === 'feature' ? '    authorize Access\n' : '') +
            '    feature Claims\n' + (gate === 'nested' ? '      authorize Access\n' : '') +
            '      slice Automation Claiming\n        event Approved\n        event Claimed\n        command Claim\n' +
            (gate === 'command' ? '          authorize Access\n' : '') +
            '          produces Claimed\n        reaction Claimer\n          when Approved\n            invokes Claim\n' +
            `              on refused${selector ? ` by ${selector}` : ''}\n                acknowledge\n`;
        const result = parse(source, 'application.play');
        expect(result.success).toBe(true);
        const warnings = result.diagnostics.filter(diagnostic => diagnostic.code === code);
        expect(warnings).toHaveLength(expected ? 1 : 0);
        if (expected) expect(warnings[0]).toEqual({ code, severity: 'warning', message, location: { path: 'application.play', line: source.split('\n').findIndex(line => line.includes('on refused')) + 1, column: 15 } });
    });

    it.each([
        ['module', false, true], ['module', false, false], ['module', true, true], ['module', true, false],
        ['feature', false, true], ['feature', false, false], ['feature', true, true], ['feature', true, false],
        ['nested', false, true], ['nested', false, false], ['nested', true, true], ['nested', true, false],
    ])('should use the command own %s containers with gate %s and refusal %s without crashing', (container, gated, withRefusal) => {
        const otherGate = !gated && withRefusal ? '    authorize Access\n' : '';
        const commandGate = gated ? '    authorize Access\n' : '';
        const prefix = container === 'module'
            ? 'module M\n  feature A\nmodule M\n' + (gated ? '  authorize Access\n' : '') + '  feature B\n'
            : container === 'feature'
                ? 'module M\n  feature F\n' + otherGate + '  feature F\n' + commandGate
                : 'module M\n  feature F\n' + otherGate + '    feature Other\n  feature F\n' + commandGate + '    feature Claims\n';
        let body = '    slice Automation S\n      event Approved\n      command Claim\n      reaction R\n        when Approved\n          invokes Claim\n' +
            (withRefusal ? '            on refused by authorization\n              acknowledge\n' : '');
        if (container === 'nested') body = body.split('\n').map(line => `  ${line}`).join('\n');
        const result = parse('policy Access\n  require authenticated\n' + prefix + body);

        expect(result.success).toBe(true);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === code)).toHaveLength(gated && withRefusal ? 1 : 0);
    });

    it('should check the merged command gates once at the invocation file', () => {
        const result = compileApplication(new Map([
            ['command.play', 'policy Access\n  require role "Manager"\nmodule Billing\n  authorize Access\n  feature Claims\n    slice StateChange Claiming\n      command Claim\n        produces Claimed\n      event Claimed'],
            ['reaction.play', 'module Reactions\n  feature Handling\n    slice Automation Receiving\n      event Approved\n      reaction Claimer\n        when Approved\n          invokes Claim\n            on refused by authorization\n              acknowledge'],
        ]));
        expect(result.success).toBe(true);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === code)).toEqual([
            { code, severity: 'warning', message, location: { path: 'reaction.play', line: 8, column: 13 } },
        ]);
    });
});

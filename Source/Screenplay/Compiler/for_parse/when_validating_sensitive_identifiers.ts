// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const message = "Concept 'SecretId' is @sensitive and cannot be an event source identifier - use a surrogate Uuid identifier and keep the @sensitive value as a property";

describe('when validating sensitive identifiers', () => {
    it.each([
        ['command identifier', ['module M', '  feature F', '    slice StateChange S', '      command C', '        id SecretId identifier'], 6],
        ['event source identifier', ['eventsource Secrets', '  identifier SecretId'], 3],
        ['command destination', ['module M', '  feature F', '    slice StateChange S', '      event E', '      command C', '        secret SecretId', '        produces E', '          for secret'], 9],
        ['nested command destination', ['type Destination', '  secret SecretId', 'module M', '  feature F', '    slice StateChange S', '      event E', '      command C', '        destination Destination', '        produces E', '          for destination.secret'], 11],
        ['reaction event destination', ['module M', '  feature F', '    slice Automation S', '      event E', '        secret SecretId', '      event Recorded', '      reaction R', '        when E', '          produces Recorded', '            for secret'], 11],
        ['reaction clause destination', ['module M', '  feature F', '    slice Automation S', '      event Recorded', '      reaction R', '        when External', '          secret SecretId', '          produces Recorded', '            for secret'], 10],
        ['declared trigger destination', ['trigger External', '  secret SecretId', 'module M', '  feature F', '    slice Automation S', '      event Recorded', '      reaction R', '        when External', '          produces Recorded', '            for secret'], 11],
    ])('should reject a sensitive %s', (_, source, line) => {
        const result = parse(['concept SecretId : Uuid @sensitive', ...source as string[]].join('\n'));
        expect(result.diagnostics).toHaveLength(1);
        expect(result.diagnostics[0]).toMatchObject({ code: 'PLAY0515', message, location: { line } });
    });

    it.each(['@pii @sensitive', '@sensitive @pii'])('should reject combined classifications once for %s', attributes => {
        const result = parse(`concept SecretId : Uuid ${attributes}\neventsource Secrets\n  identifier SecretId`);
        expect(result.diagnostics).toHaveLength(1);
        expect(result.diagnostics[0].message).toBe(message.replaceAll('@sensitive', '@pii'));
    });

    it('should keep protected values as ordinary properties', () => {
        const source = ['concept Secret : String @sensitive', 'concept Personal : String @pii @sensitive', 'module M', '  feature F', '    slice StateChange S', '      command C', '        id Uuid identifier', '        secret Secret', '        personal Personal'];
        expect(parse(source.join('\n')).diagnostics).toEqual([]);
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';

const message = "Concept 'SecretId' is secret and cannot be an event source identifier - use a surrogate Uuid identifier and keep the secret value as a property";

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
        const result = parse(['concept SecretId : Uuid secret', ...source as string[]].join('\n'));
        result.diagnostics.should.have.lengthOf(1);
        const diagnostic = result.diagnostics[0];
        ({ code: diagnostic.code, message: diagnostic.message, line: diagnostic.location.line }).should.deep.equal({ code: 'PLAY0515', message, line });
    });

    it.each(['pii secret', 'secret pii'])('should reject combined classifications once for %s', attributes => {
        const result = parse(`concept SecretId : Uuid ${attributes}\neventsource Secrets\n  identifier SecretId`);
        result.diagnostics.should.have.lengthOf(1);
        result.diagnostics[0].message.should.equal(message.replaceAll('secret', 'pii'));
    });

    it('should keep protected values as ordinary properties', () => {
        const source = ['concept Secret : String secret', 'concept Personal : String pii secret', 'module M', '  feature F', '    slice StateChange S', '      command C', '        id Uuid identifier', '        secret Secret', '        personal Personal'];
        parse(source.join('\n')).diagnostics.should.deep.equal([]);
    });
});

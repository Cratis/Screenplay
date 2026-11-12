// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse, parseForAuthoring } from '../ScreenplayCompiler';

const source = `secret Key
  description "The key"

system Mailer
  ability Send
  extension "ghcr.io/x/mail" version 2
  setting apiKey from secret Key
  setting sender = "Acme"
  setting retries = 3
  setting open = false
  activation on demand idle after 10 minutes
`;

describe('when reading system configuration', () => {
    const result = parse(source);
    const system = result.value.systems![0];

    it('should report nothing', () => expect(result.diagnostics).toEqual([]));
    it('should read the secret', () => expect(result.value.secrets!.map(secret => [secret.name, secret.description])).toEqual([['Key', 'The key']]));
    it('should read the extension', () => expect([system.extension?.reference, system.extension?.version]).toEqual(['ghcr.io/x/mail', '2']));
    it('should read the settings in order', () => expect(system.settings!.map(setting => [setting.name, setting.secret, setting.value, setting.valueKind])).toEqual([['apiKey', 'Key', null, null], ['sender', null, 'Acme', 'string'], ['retries', null, '3', 'number'], ['open', null, 'false', 'boolean']]));
    it('should read the activation', () => expect([system.activation?.always, system.activation?.idleAmount, system.activation?.idleUnit]).toEqual([false, 10, 'minute']));
    it('should omit the members of a system that configures nothing', () => expect(Object.keys(parse('system S\n  ability A\n').value.systems![0])).not.toContain('settings'));
});

describe('when validating system configuration', () => {
    const codes = (text: string) => parseForAuthoring(text, undefined, undefined, true).diagnostics.map(diagnostic => diagnostic.code);

    it('should refuse a duplicate secret ignoring case', () => expect(codes('secret A\nsecret a\nsystem S\n  setting x from secret A\n')).toEqual(['PLAY0991']));
    it('should refuse an unknown secret', () => expect(codes('system S\n  setting x from secret Missing\n')).toEqual(['PLAY0992']));
    it('should refuse an unused secret', () => expect(codes('secret A\n')).toEqual(['PLAY0993']));
    it('should refuse a duplicate mapping', () => expect(codes('system S\n  setting x = 1\n  setting x = 2\n')).toEqual(['PLAY0994']));
    it('should refuse a malformed secret', () => expect(codes('secret\n')).toContain('PLAY0990'));
    it('should refuse a malformed setting', () => expect(codes('system S\n  setting x\n')).toEqual(['PLAY0495']));
    it('should refuse a secret with anything but a description', () => expect(codes('secret A\n  value "x"\nsystem S\n  setting x from secret A\n')).toEqual(['PLAY0990']));
    it('should refuse a malformed extension', () => expect(codes('system S\n  extension ghcr\n')).toEqual(['PLAY0495']));
    it('should refuse an empty extension reference', () => expect(codes('system S\n  extension " "\n')).toEqual(['PLAY0495']));
    it('should refuse a second extension', () => expect(codes('system S\n  extension "a"\n  extension "b"\n')).toEqual(['PLAY0495']));
    it('should refuse a malformed activation', () => expect(codes('system S\n  activation sometimes\n')).toEqual(['PLAY0495']));
    it('should refuse a second activation', () => expect(codes('system S\n  activation always\n  activation always\n')).toEqual(['PLAY0495']));
    it('should refuse a zero idle timeout', () => expect(codes('system S\n  activation on demand idle after 0 minutes\n')).toEqual(['PLAY0495']));
    it('should refuse children under a one-line setting', () => expect(codes('system S\n  setting x = 1\n    y\n')).toEqual(['PLAY0495']));
    it('should accept always and on demand without an idle timeout', () => expect([codes('system S\n  activation always\n'), codes('system S\n  activation on demand\n')]).toEqual([[], []]));
});

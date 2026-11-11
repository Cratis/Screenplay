// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rules = (JSON.stringify(grammar).match(/"match":\s*"((?:[^"\\]|\\.)*)"/g) ?? [])
    .map(entry => JSON.parse(`{${entry}}`).match as string);
const exec = (line: string) => rules.filter(rule => /\((extension|setting|activation|secret)\)/.test(rule)).map(rule => new RegExp(rule).exec(line)).find(match => match !== null);

describe('when highlighting system configuration', () => {
    it('should highlight an extension with its version', () => {
        const match = exec('  extension "ghcr.io/cratis/extension-mail" version 2')!;
        expect([match[1], match[3], match[4]]).toEqual(['extension', 'version', '2']);
    });
    it('should highlight a setting mapped from a secret', () => {
        const match = exec('  setting apiKey from secret MailerApiKey')!;
        expect([match[1], match[2], match[4], match[5]]).toEqual(['setting', 'apiKey', 'secret', 'MailerApiKey']);
    });
    it('should highlight an on demand activation with an idle timeout', () => expect(exec('  activation on demand idle after 10 minutes')?.[2]).toBe('on demand idle after 10 minutes'));
    it('should highlight a top level secret', () => expect(exec('secret MailerApiKey')?.slice(1, 3)).toEqual(['secret', 'MailerApiKey']));
    it('should not highlight a property called setting', () => expect(exec('    setting String')).toBeUndefined());
});

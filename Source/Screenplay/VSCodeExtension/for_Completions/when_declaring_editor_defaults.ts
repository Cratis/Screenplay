// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const manifest = JSON.parse(readFileSync(new URL('../package.json', import.meta.url), 'utf8'));
const dictionary = JSON.parse(readFileSync(new URL('../cspell-screenplay.json', import.meta.url), 'utf8'));

describe('when declaring editor defaults', () => {
    it('should turn Copilot inline suggestions off for Screenplay files', () => {
        manifest.contributes.configurationDefaults['github.copilot.enable'].screenplay.should.be.false;
    });

    it('should leave editor inline suggestions on, so the language service can offer structure', () => {
        (manifest.contributes.configurationDefaults['[screenplay]']['editor.inlineSuggest.enabled'] === undefined).should.be.true;
    });

    it('should give the spell checker the language keywords', () => {
        ['readmodel', 'reducer', 'eventsource'].filter(word => !dictionary.words.includes(word)).should.deep.equal([]);
    });
});

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const patterns = Object.values(grammar.repository as Record<string, { patterns?: { match?: string; name?: string }[] }>)
    .flatMap(entry => entry.patterns ?? []);

const line = '  theme "Scene Default Light"';

function matchedBy(scope: string): string | undefined {
    for (const pattern of patterns) {
        if (pattern.name !== scope || pattern.match === undefined) continue;
        const match = new RegExp(pattern.match).exec(line);
        if (match) return match[0];
    }
    return undefined;
}

describe('when highlighting a ui profile theme named by a quoted package theme', () => {
    it('scopes the quoted name as a string', () => matchedBy('string.quoted.double.screenplay')?.should.equal('"Scene Default Light"'));
});

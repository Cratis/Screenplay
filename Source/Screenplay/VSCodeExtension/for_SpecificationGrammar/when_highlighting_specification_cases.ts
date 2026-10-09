// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, expect, it } from 'vitest';
import { loadWASM, OnigScanner } from 'vscode-oniguruma';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
let scanner: OnigScanner;

function patterns(value: unknown): { match: string }[] {
    if (Array.isArray(value)) return value.flatMap(patterns);
    if (value === null || typeof value !== 'object') return [];
    const record = value as Record<string, unknown>;
    return [...(typeof record.match === 'string' ? [record as { match: string }] : []), ...Object.values(record).flatMap(patterns)];
}

beforeAll(async () => {
    const wasm = readFileSync(createRequire(import.meta.url).resolve('vscode-oniguruma/release/onig.wasm'));
    await loadWASM(wasm.buffer.slice(wasm.byteOffset, wasm.byteOffset + wasm.byteLength));
    scanner = new OnigScanner(patterns(grammar).filter(rule => rule.match.includes('(parameter|case)') || rule.match.includes('\\b(case)')).map(rule => rule.match));
});

describe('when highlighting specification cases', () => {
    it.each(['  parameter amount Int', '  case Small amount = 10', '    amount = case.amount', '  then error case.reason'])('should recognize %s', line => {
        expect(scanner.findNextMatchSync(line, 0)).not.toBeNull();
    });
});

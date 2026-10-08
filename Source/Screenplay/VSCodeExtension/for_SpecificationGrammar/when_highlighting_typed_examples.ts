// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { beforeAll, describe, expect, it } from 'vitest';
import { loadWASM, OnigScanner } from 'vscode-oniguruma';

const grammar = JSON.parse(readFileSync(new URL('../syntaxes/screenplay.tmLanguage.json', import.meta.url), 'utf8'));
const rule = grammar.repository['example-header'];
let scanner: OnigScanner;
beforeAll(async () => {
    const wasm = readFileSync(createRequire(import.meta.url).resolve('vscode-oniguruma/release/onig.wasm'));
    await loadWASM(wasm.buffer.slice(wasm.byteOffset, wasm.byteOffset + wasm.byteLength));
    scanner = new OnigScanner([rule.match]);
});

describe('when highlighting typed examples', () => {
    it.each(['Acme', 'Acmé', 'Acme\u0301\u203f\u0661'])('should scope the declaration keyword, name, colon and qualified type for %s', name => {
        const line = `      example ${name} : Invoices.Register.Invoice // fixture`;
        const match = scanner.findNextMatchSync(line, 0);
        expect(match?.captureIndices.slice(1).map(capture => line.slice(capture.start, capture.end))).toEqual(['example', name, ':', 'Invoices.Register.Invoice']);
        expect(Object.values(rule.captures).map((capture: unknown) => (capture as { name: string }).name)).toEqual([
            'keyword.control.screenplay', 'entity.name.type.screenplay', 'keyword.operator.screenplay', 'entity.name.type.screenplay',
        ]);
    });
    it.each(['      example String', '      // example Acme : Invoice', '      example Acme𐐀 : Invoice'])('should not claim a property, comment or invalid compiler name: %s', line => {
        expect(scanner.findNextMatchSync(line, 0)).toBeNull();
    });
});

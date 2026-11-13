// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { emptyExtensionLock, extensionRangeOf, findExtensionLockEntry, formatExtensionLock, parseExtensionLock } from '../Parsing/ExtensionLock';

const digest = `sha256:${'a'.repeat(64)}`;
const hash = `sha256:${'b'.repeat(64)}`;
const lock = {
    entries: [
        { reference: 'npm:@acme/charts', range: 'latest', resolved: 'npm:@acme/charts', version: '1.0.0', digest, contractHash: hash, publisher: 'acme' },
        { reference: 'cratis/stripe', range: '^2.3', resolved: 'ghcr.io/cratis/extension-stripe', version: '2.3.1', digest, contractHash: hash, publisher: 'https://github.com/cratis' }
    ]
};

describe('when round tripping a lock', () => {
    const text = formatExtensionLock(lock);
    const parsed = parseExtensionLock(text);

    it('should parse without diagnostics', () => expect(parsed.diagnostics).toEqual([]));
    it('should order entries by reference', () => expect(parsed.value.entries.map(entry => entry.reference)).toEqual(['cratis/stripe', 'npm:@acme/charts']));
    it('should keep every entry', () => expect(findExtensionLockEntry(parsed.value, 'cratis/stripe')).toEqual(lock.entries[1]));
    it('should write the same text again', () => expect(formatExtensionLock(parsed.value)).toBe(text));
    it('should not be json', () => expect(text.trimStart().startsWith('{')).toBe(false));
    it('should find nothing for an unknown reference', () => expect(findExtensionLockEntry(emptyExtensionLock, 'x')).toBeUndefined());
    it('should normalize the range a model asks for', () => expect([extensionRangeOf(null), extensionRangeOf('"^2.3"'), extensionRangeOf('2')]).toEqual(['latest', '^2.3', '2']));
});

describe('when parsing a malformed lock', () => {
    const codes = (text: string): string[] => parseExtensionLock(text).diagnostics.map(diagnostic => diagnostic.code);

    it('should require the version line', () => expect(codes('extension "a"\n')).toContain(DiagnosticCodes.InvalidExtensionLock));
    it('should report an incomplete entry', () => expect(codes('lock 1\nextension "a"\n  version 1.0.0\n')).toEqual([DiagnosticCodes.InvalidExtensionLock]));
    it('should report an unknown line', () => expect(codes('lock 1\nwhat\n')).toEqual([DiagnosticCodes.InvalidExtensionLock]));
    it('should accept an empty lock', () => expect(codes('lock 1\n')).toEqual([]));
    it('should report a reference pinned twice', () => {
        const entry = `extension "a"\n  range "1"\n  resolved "a"\n  version 1.0.0\n  digest ${digest}\n  contract ${hash}\n  publisher "p"\n`;
        expect(codes(`lock 1\n${entry}${entry}`)).toEqual([DiagnosticCodes.InvalidExtensionLock]);
    });
});

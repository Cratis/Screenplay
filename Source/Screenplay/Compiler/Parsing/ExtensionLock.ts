// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { Diagnostic } from '../Diagnostics/Diagnostic';
import { sourceLocation } from '../Diagnostics/SourceLocation';
import { pattern } from '../Text/patterns';
import { escapeString, unescapeString } from '../Text/StringLiteral';

/** The name of the lock file beside the model root. */
export const extensionLockFileName = 'screenplay.lock';

/** The range recorded when a model names no version. */
export const latestExtensionRange = 'latest';

/** One pinned extension reference of the lock file. */
export interface ExtensionLockEntry {
    readonly reference: string;
    readonly range: string;
    readonly resolved: string;
    readonly version: string;
    readonly digest: string;
    readonly contractHash: string;
    readonly publisher: string;
}

export interface ExtensionLock {
    readonly entries: readonly ExtensionLockEntry[];
}

export interface ExtensionLockResult {
    readonly value: ExtensionLock;
    readonly diagnostics: readonly Diagnostic[];
}

const referencePattern = pattern('^extension\\s+"((?:[^"\\\\]|\\\\.)*)"\\s*$');
const fieldPattern = pattern('^\\s+([a-z]+)\\s+(\\S.*)$');
const fields = ['range', 'resolved', 'version', 'digest', 'contract', 'publisher'] as const;

/** The range a model's extension line asks for, normalized for comparison with a lock entry. */
export const extensionRangeOf = (version: string | null): string => version === null ? latestExtensionRange : version.replace(/^"|"$/g, '');

export const emptyExtensionLock: ExtensionLock = { entries: [] };

export function findExtensionLockEntry(extensionLock: ExtensionLock, reference: string): ExtensionLockEntry | undefined {
    return extensionLock.entries.find(entry => entry.reference === reference);
}

/** Parses lock file text; the TypeScript counterpart of the C# ExtensionLock.Parse. */
export function parseExtensionLock(text: string, path?: string): ExtensionLockResult {
    const diagnostics: Diagnostic[] = [];
    const entries: ExtensionLockEntry[] = [];
    let reference: string | null = null;
    let values = new Map<string, string>();
    let versionSeen = false;
    let start = sourceLocation(1, 1, path);
    const error = (message: string, line: number): void => {
        diagnostics.push({ severity: 'error', code: DiagnosticCodes.InvalidExtensionLock, message, location: sourceLocation(line, 1, path) });
    };
    const flush = (): void => {
        if (reference === null) return;
        const missing = fields.filter(key => !values.has(key));
        if (missing.length > 0) diagnostics.push({ severity: 'error', code: DiagnosticCodes.InvalidExtensionLock, message: `Lock entry '${reference}' is missing ${missing.join(', ')}.`, location: start });
        else entries.push({ reference, range: values.get('range')!, resolved: values.get('resolved')!, version: values.get('version')!, digest: values.get('digest')!, contractHash: values.get('contract')!, publisher: values.get('publisher')! });
        reference = null;
        values = new Map();
    };
    text.split('\n').forEach((raw, index) => {
        const number = index + 1;
        const line = raw.replace(/\r$/, '');
        const trimmed = line.trim();
        if (trimmed.length === 0 || trimmed.startsWith('#')) return;
        if (trimmed === 'lock 1') {
            versionSeen = true;
            return;
        }
        const header = referencePattern.exec(line);
        if (header !== null) {
            flush();
            reference = unescapeString(header[1]);
            start = sourceLocation(number, 1, path);
            if (entries.some(entry => entry.reference === reference)) error(`The lock pins '${reference}' twice.`, number);
            return;
        }
        const field = reference === null ? null : fieldPattern.exec(line);
        if (field !== null && (fields as readonly string[]).includes(field[1])) {
            const value = field[2].trim();
            values.set(field[1], value.startsWith('"') ? unescapeString(value.slice(1, -1)) : value);
        } else error(`Unexpected lock line '${trimmed}'.`, number);
    });
    flush();
    if (!versionSeen) error("A lock file starts with 'lock 1'.", 1);
    return { value: { entries: [...entries].sort((left, right) => compareOrdinal(left.reference, right.reference)) }, diagnostics };
}

const compareOrdinal = (left: string, right: string): number => left < right ? -1 : left > right ? 1 : 0;

/** Writes the lock as text, deterministically so it diffs cleanly. */
export function formatExtensionLock(extensionLock: ExtensionLock): string {
    let text = "# screenplay.lock: pins the extensions this model references. Written by 'screenplay restore'; commit it, do not edit it by hand.\nlock 1\n";
    for (const entry of [...extensionLock.entries].sort((left, right) => compareOrdinal(left.reference, right.reference))) {
        text += `\nextension "${escapeString(entry.reference)}"\n`
            + `  range "${escapeString(entry.range)}"\n`
            + `  resolved "${escapeString(entry.resolved)}"\n`
            + `  version ${entry.version}\n`
            + `  digest ${entry.digest}\n`
            + `  contract ${entry.contractHash}\n`
            + `  publisher "${escapeString(entry.publisher)}"\n`;
    }
    return text;
}

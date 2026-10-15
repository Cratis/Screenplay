// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { conceptItems, CompletionEntry } from './completion-items';

export function generationCompletions(lines: string[], lineIndex: number, textBefore: string): CompletionEntry[] | null {
    const header = lines.slice(0, lineIndex).reverse().find(line => /^\s*concept\s+[^\s:]+\s*:\s*\w+/.test(line));
    const primitive = header?.match(/:\s*(\w+)/)?.[1];
    if (primitive === 'String' && /^\s*(?:validate\s+)?checksum\s+\w*$/.test(textBefore)) {
        return ['luhn', 'mod11', 'mod97'].map(algorithm => ({ label: algorithm, insertText: algorithm, documentation: 'Exact trailing checksum validation over preceding ASCII digits; independent of generation.' }));
    }
    if (/^\s*generate\s+\w*$/.test(textBefore)) {
        const allowed = primitive === 'Uuid' ? ['uuid', 'uuidv7'] : primitive === 'Int' ? ['random'] : primitive === 'String' ? ['uuid', 'uuidv7', 'ulid', 'typeid', 'nanoid', 'bytes', 'pattern', 'rule'] : [];
        return conceptItems.filter(item => allowed.includes(item.label.slice('generate '.length))).map(item => ({ ...item, label: item.label.slice('generate '.length), insertText: item.insertText.slice('generate '.length) }));
    }
    const strategy = textBefore.match(/^\s*generate\s+(\w+)\s+/)?.[1];
    if (!strategy) return null;
    const outsideStrings = textBefore.replace(/"(?:[^"\\]|\\.)*"/g, '');
    if (outsideStrings.includes('"')) return [];
    if (strategy === 'bytes' && !/\b(base64url|hex|base32)\b/.test(outsideStrings)) {
        return ['base64url', 'hex', 'base32'].map(encoding => ({ label: encoding, insertText: encoding, documentation: 'RFC 4648 without padding; exact byte count and zero unused trailing bits.' }));
    }
    const entries: CompletionEntry[] = [];
    if (primitive === 'String' && ['uuid', 'uuidv7', 'ulid', 'nanoid', 'bytes'].includes(strategy)) entries.push({ label: 'prefix', insertText: 'prefix "${1:ref_}"', documentation: 'At most 32 printable ASCII characters; adds no random bits.' });
    if (strategy === 'ulid' || (primitive === 'String' && ['uuid', 'uuidv7'].includes(strategy)) || (strategy === 'bytes' && /\b(hex|base32)\b/.test(outsideStrings))) entries.push({ label: 'case', insertText: 'case ${1|lower,upper|}', documentation: 'Changes the encoded portion only; never the prefix.' });
    if (strategy === 'nanoid') entries.push({ label: 'alphabet', insertText: 'alphabet "${1:abcdef0123456789}"', documentation: '2..256 distinct printable ASCII characters; bits = size × log2(alphabet size).' });
    if (strategy === 'pattern') entries.push({ label: 'checksum', insertText: 'checksum ${1|luhn,mod11,mod97|}', documentation: 'Append deterministic check digits without adding entropy. Fixtures check digit shape; author validate checksum separately to enforce arithmetic. mod11 excludes output 10 from support.' });
    return entries.filter(entry => !new RegExp(`\\b${entry.label}\\b`).test(outsideStrings));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PurposeReferenceSyntax, PurposeSyntax, PurposeTransferSyntax } from '../Syntax/Purposes';
import { stringBodyPattern, unescapeString } from '../Text/StringLiteral';
import { pattern } from '../Text/patterns';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^purpose\\s+([A-Za-z_]\\w*)$');
const vocabulary = pattern(`^(basis|condition)\\s+(\\w+)(?:\\s+"(${stringBodyPattern})")?$`);
const textField = pattern(`^(interest|authorization|retention|recipient)\\s+"(${stringBodyPattern})"$`);
const transfer = pattern(`^transfer\\s+"(${stringBodyPattern})"\\s+safeguard\\s+"(${stringBodyPattern})"$`);
const bases = ['consent', 'contract', 'legalObligation', 'vitalInterests', 'publicTask', 'legitimateInterests'];
const conditions = ['explicitConsent', 'employmentLaw', 'vitalInterests', 'notForProfit', 'madePublic', 'legalClaims', 'substantialPublicInterest', 'healthCare', 'publicHealth', 'research'];
const exceptions = ['expression', 'legalObligation', 'publicTask', 'publicHealth', 'archiving', 'legalClaims'];

export function parsePurpose(context: ParserContext, line: SourceLine): PurposeSyntax {
    const name = purposeName(context, line);
    let purpose: PurposeSyntax = { kind: 'PurposeSyntax', name, description: null, basis: null, basisReference: null, interest: null, condition: null, conditionReference: null, authorization: null, subjects: [], retention: null, recipients: [], transfers: [], erasureException: null, location: locationOf(line) };
    const seen = new Set<string>();
    const recipients: string[] = [];
    const transfers: PurposeTransferSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(child.content);
        if (keyword !== 'recipient' && keyword !== 'transfer' && seen.has(keyword)) {
            context.error(DiagnosticCodes.DuplicatePurposeField, `Purpose '${name}' already declares '${keyword}' - at most one is allowed`, locationOf(child));
            context.skipBlock(child.indent);
            continue;
        }
        seen.add(keyword);
        if (keyword === 'description') purpose = { ...purpose, description: parseDescription(context, child, purpose.description, `Purpose '${name}'`) };
        else if (keyword === 'basis' || keyword === 'condition') {
            const match = vocabulary.exec(child.content);
            const values = keyword === 'basis' ? bases : conditions;
            if (match === null || !values.includes(match[2])) {
                context.error(DiagnosticCodes.InvalidPurposeVocabulary, `Invalid ${keyword} in purpose '${name}' - expected ${values.join(', ')} with an optional quoted reference`, locationOf(child));
            } else {
                const reference = match[3] === undefined ? null : unescapeString(match[3]);
                purpose = keyword === 'basis' ? { ...purpose, basis: match[2], basisReference: reference } : { ...purpose, condition: match[2], conditionReference: reference };
            }
        } else if (keyword === 'interest' || keyword === 'authorization' || keyword === 'retention' || keyword === 'recipient') {
            const match = textField.exec(child.content);
            if (match === null) context.error(DiagnosticCodes.InvalidPurposeDeclaration, `Invalid purpose field '${child.content}' - expected '${keyword} "<text>"'`, locationOf(child));
            else if (keyword === 'recipient') recipients.push(unescapeString(match[2]));
            else purpose = { ...purpose, [keyword]: unescapeString(match[2]) };
        } else if (keyword === 'subjects') {
            const subjects = child.content.slice('subjects'.length).split(',').map(value => value.trim());
            if (subjects.every(value => /^[A-Za-z_]\w*$/.test(value))) purpose = { ...purpose, subjects: [...new Set(subjects)] };
            else context.error(DiagnosticCodes.InvalidPurposeDeclaration, `Invalid purpose subjects '${child.content}' - expected comma-separated identifiers`, locationOf(child));
        } else if (keyword === 'erasure') {
            const match = /^erasure\s+exception\s+(\w+)$/.exec(child.content);
            if (match !== null && exceptions.includes(match[1])) purpose = { ...purpose, erasureException: match[1] };
            else context.error(DiagnosticCodes.InvalidPurposeVocabulary, `Invalid erasure exception '${child.content}' - expected 'erasure exception ${exceptions.join('|')}'`, locationOf(child));
        } else if (keyword === 'transfer') {
            const match = transfer.exec(child.content);
            if (match === null) context.error(DiagnosticCodes.InvalidPurposeDeclaration, `Invalid transfer '${child.content}' - expected 'transfer "<destination>" safeguard "<text>"'`, locationOf(child));
            else transfers.push({ kind: 'PurposeTransferSyntax', destination: unescapeString(match[1]), safeguard: unescapeString(match[2]), location: locationOf(child) });
        } else {
            context.error(DiagnosticCodes.InvalidPurposeDeclaration, `Unexpected '${child.content}' in purpose '${name}'`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return { ...purpose, recipients, transfers };
}

export function parsePurposeReference(context: ParserContext, line: SourceLine, references: PurposeReferenceSyntax[]): void {
    const name = purposeName(context, line);
    if (name.length > 0 && !references.some(reference => reference.name === name)) references.push({ kind: 'PurposeReferenceSyntax', name, location: locationOf(line) });
}

function purposeName(context: ParserContext, line: SourceLine): string {
    const match = header.exec(line.content);
    if (match !== null) return match[1];
    context.error(DiagnosticCodes.InvalidPurposeDeclaration, `Invalid purpose declaration '${line.content}' - expected 'purpose <Name>'`, locationOf(line));
    return '';
}

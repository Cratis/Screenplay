// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PlayPlacement } from '../Files/PlayPlacement';
import { parse } from '../ScreenplayCompiler';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

export interface QuickFixEdit {
    readonly start: number;
    readonly length: number;
    readonly text: string;
}

export interface QuickFix {
    readonly diagnosticCode: string;
    readonly title: string;
    readonly scope: 'occurrence' | 'document';
    readonly edits: readonly QuickFixEdit[];
}

export interface QuickFixOptions {
    readonly line?: number;
    readonly placement?: PlayPlacement;
}

// Diagnostics point at the complete type, not at the suffix. Never derive edits from message text.
export function legacyOptionalTypeLength(line: string, diagnostic: Diagnostic): number {
    if (diagnostic.code !== DiagnosticCodes.LegacyOptionalSuffix) return 0;
    return /^[\p{L}\p{Mn}\p{Nd}\p{Pc}.]+(?:\[\])?\?/u.exec(line.slice(diagnostic.location.column - 1))?.[0].length ?? 0;
}

// Recipes own their syntax postcondition. Future id/destination repairs can add a recipe with an
// explicit intended structural delta; no recipe may bypass reparsing or diagnostic disappearance.
export function findQuickFixes(source: string, options: QuickFixOptions = {}): QuickFix[] {
    const original = parse(source, undefined, options.placement);
    if (!original.success) return [];
    const lines = source.split(/\r\n|\r|\n/);
    const starts = [0];
    for (const match of source.matchAll(/\r\n|\r|\n/g)) starts.push(match.index + match[0].length);
    const occurrences = original.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.LegacyOptionalSuffix).flatMap(diagnostic => {
        const length = legacyOptionalTypeLength(lines[diagnostic.location.line - 1], diagnostic);
        return length === 0 ? [] : [{ diagnostic, edit: { start: starts[diagnostic.location.line - 1] + diagnostic.location.column + length - 2, length: 1, text: ' optional' } }];
    });
    if (occurrences.length === 0) return [];
    const syntax = JSON.stringify(toSyntaxJson(original.value));
    const candidates: { fix: QuickFix; lines: Set<number> }[] = [];
    const selected = occurrences.find(occurrence => occurrence.diagnostic.location.line === options.line);
    if (selected !== undefined) {
        candidates.push({ fix: { diagnosticCode: DiagnosticCodes.LegacyOptionalSuffix, title: "Use 'optional' instead of '?'", scope: 'occurrence', edits: [selected.edit] }, lines: new Set([selected.diagnostic.location.line]) });
    }
    candidates.push({ fix: { diagnosticCode: DiagnosticCodes.LegacyOptionalSuffix, title: "Use 'optional' throughout this document", scope: 'document', edits: occurrences.map(occurrence => occurrence.edit) }, lines: new Set(occurrences.map(occurrence => occurrence.diagnostic.location.line)) });
    return candidates.filter(({ fix, lines }) => {
        const candidate = applyQuickFixEdits(source, fix.edits);
        if (candidate === undefined) return false;
        const parsed = parse(candidate, undefined, options.placement);
        return parsed.success && JSON.stringify(toSyntaxJson(parsed.value)) === syntax &&
            !parsed.diagnostics.some(diagnostic => diagnostic.code === fix.diagnosticCode && lines.has(diagnostic.location.line));
    }).map(candidate => candidate.fix);
}

// One forward pass; applying N edits by repeatedly slicing the document would be quadratic.
export function applyQuickFixEdits(source: string, edits: readonly QuickFixEdit[]): string | undefined {
    const parts: string[] = [];
    let end = 0;
    for (const edit of edits) {
        if (edit.start < end || edit.start < 0 || edit.length < 0 || edit.start + edit.length > source.length) return undefined;
        parts.push(source.slice(end, edit.start), edit.text);
        end = edit.start + edit.length;
    }
    parts.push(source.slice(end));
    return parts.join('');
}

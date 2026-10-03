// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PlayPlacement } from '../Files/PlayPlacement';
import { parseForAuthoring } from '../ScreenplayCompiler';
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

export function findQuickFixes(source: string, options: QuickFixOptions = {}): QuickFix[] {
    return prepareQuickFixes(source, options)(options.line);
}

// One immutable buffer version owns the analysis and document verdict. Cursor moves reuse them;
// occurrence verdicts have a bounded cache, and no candidate source or syntax tree is retained.
export function prepareQuickFixes(source: string, options: Pick<QuickFixOptions, 'placement'> = {}): (line?: number) => QuickFix[] {
    const original = parseForAuthoring(source, undefined, options.placement);
    if (!original.success) return () => [];
    const lines = source.split(/\r\n|\r|\n/);
    const starts = [0];
    for (const match of source.matchAll(/\r\n|\r|\n/g)) starts.push(match.index + match[0].length);
    const occurrences = original.diagnostics.filter(diagnostic => diagnostic.code === DiagnosticCodes.LegacyOptionalSuffix).flatMap(diagnostic => {
        const length = legacyOptionalTypeLength(lines[diagnostic.location.line - 1], diagnostic);
        return length === 0 ? [] : [{ diagnostic, edit: { start: starts[diagnostic.location.line - 1] + diagnostic.location.column + length - 2, length: 1, text: ' optional' } }];
    });
    if (occurrences.length === 0) return () => [];
    const syntax = verificationShape(original);
    const byLine = new Map(occurrences.map(occurrence => [occurrence.diagnostic.location.line, occurrence]));
    const verifiedOccurrences = new Map<number, QuickFix | undefined>();
    let documentChecked = false;
    let documentFix: QuickFix | undefined;
    const verify = (fix: QuickFix, line?: number): QuickFix | undefined => {
        const candidate = applyQuickFixEdits(source, fix.edits);
        if (candidate === undefined) return undefined;
        const parsed = parseForAuthoring(candidate, undefined, options.placement);
        return parsed.success && verificationShape(parsed) === syntax &&
            !parsed.diagnostics.some(diagnostic => diagnostic.code === fix.diagnosticCode && (line === undefined || line === diagnostic.location.line)) ? fix : undefined;
    };
    return line => {
        if (!documentChecked) {
            documentFix = verify({ diagnosticCode: DiagnosticCodes.LegacyOptionalSuffix, title: "Use 'optional' throughout this document", scope: 'document', edits: occurrences.map(occurrence => occurrence.edit) });
            documentChecked = true;
        }
        const selected = line === undefined ? undefined : byLine.get(line);
        let occurrenceFix: QuickFix | undefined;
        if (selected !== undefined && line !== undefined) {
            if (!verifiedOccurrences.has(line)) {
                if (verifiedOccurrences.size === 32) verifiedOccurrences.delete(verifiedOccurrences.keys().next().value!);
                verifiedOccurrences.set(line, verify({ diagnosticCode: DiagnosticCodes.LegacyOptionalSuffix, title: "Use 'optional' instead of '?'", scope: 'occurrence', edits: [selected.edit] }, line));
            }
            occurrenceFix = verifiedOccurrences.get(line);
        }
        return [occurrenceFix, documentFix].filter((fix): fix is QuickFix => fix !== undefined);
    };
}

function verificationShape(parsed: ReturnType<typeof parseForAuthoring>): string {
    // Trigger data is intentionally absent from SyntaxJson's application projection. Comparing its
    // committed name/type/optionality separately closes that gap without changing the public AST.
    return JSON.stringify([toSyntaxJson(parsed.value), parsed.triggerData.map(value => toSyntaxJson(value))]);
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

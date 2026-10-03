// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PlayPlacement } from '../Files/PlayPlacement';
import { commentStart, splitLines } from '../Parsing/SourceLineSplitter';
import { parseForAuthoring } from '../ScreenplayCompiler';
import { EventSyntax } from '../Syntax/Declarations';
import { ProductionQuickFixes } from './ProductionQuickFixes';
import { QuickFixCandidate } from './QuickFixCandidate';

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
    /** Diagnostic line, which can differ from the inserted text's line. */
    readonly line?: number;
}

export interface QuickFixOptions {
    readonly line?: number;
    readonly placement?: PlayPlacement;
    readonly diagnosticCode?: string;
}

export function isQuickFixDiagnostic(code: unknown): code is string {
    return code === DiagnosticCodes.LegacyOptionalSuffix || code === DiagnosticCodes.RedundantEventId || code === DiagnosticCodes.OmittedProductionDestination;
}

// Diagnostics point at the complete type, not at the suffix. Never derive edits from message text.
export function legacyOptionalTypeLength(line: string, diagnostic: Diagnostic): number {
    if (diagnostic.code !== DiagnosticCodes.LegacyOptionalSuffix) return 0;
    return /^[\p{L}\p{Mn}\p{Nd}\p{Pc}.]+(?:\[\])?\?/u.exec(line.slice(diagnostic.location.column - 1))?.[0].length ?? 0;
}

export function findQuickFixes(source: string, options: QuickFixOptions = {}): QuickFix[] {
    return prepareQuickFixes(source, options)(options.line, options.diagnosticCode);
}

// One immutable buffer version owns the analysis and document verdict. Cursor moves reuse them;
// occurrence verdicts have a bounded cache, and no candidate source or syntax tree is retained.
export function prepareQuickFixes(source: string, options: Pick<QuickFixOptions, 'placement'> = {}): (line?: number, diagnosticCode?: string) => QuickFix[] {
    const original = parseForAuthoring(source, undefined, options.placement);
    if (!original.success) return () => [];
    const lines = splitLines(source);
    const productions = new ProductionQuickFixes(original.value, lines);
    const diagnostics = [...original.diagnostics, ...productions.diagnostics];
    const counts = new Map<string, number>();
    const byLine = new Map<number, QuickFixCandidate>();
    const events = new Map<number, EventSyntax>();
    let event: EventSyntax | undefined;
    for (const line of lines) {
        event = productions.eventsByLine.get(line.number) ?? event;
        if (event !== undefined) events.set(line.number, event);
    }
    for (const diagnostic of diagnostics) {
        counts.set(diagnostic.code, (counts.get(diagnostic.code) ?? 0) + 1);
        const line = lines[diagnostic.location.line - 1];
        const length = legacyOptionalTypeLength(line.raw, diagnostic);
        if (length > 0) {
            byLine.set(line.number, { line: line.number, fix: { diagnosticCode: diagnostic.code, title: "Use 'optional' instead of '?'", scope: 'occurrence', edits: [{ start: line.startOffset + diagnostic.location.column + length - 2, length: 1, text: ' optional' }] } });
        } else if (diagnostic.code === DiagnosticCodes.RedundantEventId && commentStart(line.raw) < 0) {
            const declaration = events.get(line.number);
            if (declaration?.id !== declaration?.name || declaration === undefined) continue;
            byLine.set(line.number, { line: line.number, fix: { diagnosticCode: diagnostic.code, title: 'Remove the redundant event id', scope: 'occurrence', edits: [{ start: line.startOffset, length: (lines[line.number]?.startOffset ?? source.length) - line.startOffset, text: '' }] },
                change: { node: declaration, replacement: { ...declaration, id: null } as EventSyntax } });
        }
    }
    for (const candidate of productions.candidates(source, lines)) byLine.set(candidate.line, candidate);
    const optional = [...byLine.values()].filter(candidate => candidate.fix.diagnosticCode === DiagnosticCodes.LegacyOptionalSuffix);
    const syntax = verificationShape(original);
    const verified = new Map<number, QuickFix | undefined>();
    let documentChecked = false;
    let documentFix: QuickFix | undefined;
    const verify = (fix: QuickFix, change?: QuickFixCandidate['change']): QuickFix | undefined => {
        const candidate = applyQuickFixEdits(source, fix.edits);
        if (candidate === undefined) return undefined;
        const parsed = parseForAuthoring(candidate, undefined, options.placement);
        if (!parsed.success || verificationShape(parsed) !== (change === undefined ? syntax : verificationShape(original, change))) return undefined;
        const reported = fix.diagnosticCode === DiagnosticCodes.OmittedProductionDestination ? new ProductionQuickFixes(parsed.value, splitLines(candidate)).diagnostics : parsed.diagnostics;
        return reported.filter(diagnostic => diagnostic.code === fix.diagnosticCode).length === (counts.get(fix.diagnosticCode) ?? 0) - fix.edits.length ? fix : undefined;
    };
    return (line, diagnosticCode) => {
        const includeDocument = diagnosticCode === undefined || diagnosticCode === DiagnosticCodes.LegacyOptionalSuffix;
        if (includeDocument && !documentChecked) {
            if (optional.length > 0) documentFix = verify({ diagnosticCode: DiagnosticCodes.LegacyOptionalSuffix, title: "Use 'optional' throughout this document", scope: 'document', edits: optional.flatMap(candidate => candidate.fix.edits) });
            documentChecked = true;
        }
        const selected = line === undefined ? undefined : byLine.get(line);
        let occurrenceFix: QuickFix | undefined;
        if (selected !== undefined && line !== undefined && (diagnosticCode === undefined || selected.fix.diagnosticCode === diagnosticCode)) {
            if (!verified.has(line)) {
                if (verified.size === 32) verified.delete(verified.keys().next().value!);
                verified.set(line, verify({ ...selected.fix, line }, selected.change));
            }
            occurrenceFix = verified.get(line);
        }
        return [occurrenceFix, includeDocument ? documentFix : undefined].filter((fix): fix is QuickFix => fix !== undefined);
    };
}

function verificationShape(parsed: ReturnType<typeof parseForAuthoring>, change?: QuickFixCandidate['change']): string {
    // Compare every modeled member (including trigger data), ignoring only source locations. A recipe
    // may replace exactly one original node; no other id or destination is normalized away.
    return JSON.stringify([parsed.value, parsed.triggerData], (key, value: unknown) => key === 'location' ? undefined : change !== undefined && value === change.node ? change.replacement : value);
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

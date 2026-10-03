// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PlayPlacement } from '../Files/PlayPlacement';
import { commentStart, splitLines } from '../Parsing/SourceLineSplitter';
import { parseForAuthoring } from '../ScreenplayCompiler';
import { EventSyntax } from '../Syntax/Declarations';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
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
}

export interface QuickFixOptions {
    readonly line?: number;
    readonly placement?: PlayPlacement;
    readonly diagnosticCode?: string;
}

export function isQuickFixDiagnostic(code: unknown): code is string {
    return code === DiagnosticCodes.LegacyOptionalSuffix || code === DiagnosticCodes.RedundantEventId;
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
// single-occurrence verdicts have a bounded cache. Range requests verify the independent recipes
// together once per version, rather than reparsing N times for N intersecting markers.
export function prepareQuickFixes(source: string, options: Pick<QuickFixOptions, 'placement'> = {}): (line?: number | readonly { line: number; diagnosticCode: string }[], diagnosticCode?: string) => QuickFix[] {
    const original = parseForAuthoring(source, undefined, options.placement);
    if (!original.success) return () => [];
    const lines = splitLines(source);
    const eventsByLine = new Map<number, EventSyntax>();
    // Index inline and standalone declarations once, without scanning the tree per diagnostic.
    const walker = new class extends ScreenplaySyntaxWalker {
        override visitEvent(event: EventSyntax): void { eventsByLine.set(event.location.line, event); }
    }();
    walker.visitApplication(original.value);
    const counts = new Map<string, number>();
    const byLine = new Map<number, QuickFixCandidate>();
    const events = new Map<number, EventSyntax>();
    let event: EventSyntax | undefined;
    for (const line of lines) {
        event = eventsByLine.get(line.number) ?? event;
        if (event !== undefined) events.set(line.number, event);
    }
    for (const diagnostic of original.diagnostics) {
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
    const optional = [...byLine.values()].filter(candidate => candidate.fix.diagnosticCode === DiagnosticCodes.LegacyOptionalSuffix);
    const syntax = verificationShape(original);
    const verified = new Map<number, QuickFix | undefined>();
    let rangeVerdicts: Map<number, QuickFix> | undefined;
    let documentChecked = false;
    let documentFix: QuickFix | undefined;
    const verify = (fix: QuickFix, change?: QuickFixCandidate['change']): QuickFix | undefined => {
        const candidate = applyQuickFixEdits(source, fix.edits);
        if (candidate === undefined) return undefined;
        const parsed = parseForAuthoring(candidate, undefined, options.placement);
        if (!parsed.success || verificationShape(parsed) !== (change === undefined ? syntax : verificationShape(original, [change]))) return undefined;
        return parsed.diagnostics.filter(diagnostic => diagnostic.code === fix.diagnosticCode).length === (counts.get(fix.diagnosticCode) ?? 0) - fix.edits.length ? fix : undefined;
    };
    return (line, diagnosticCode) => {
        const requests = typeof line === 'number' || line === undefined ? undefined : line;
        const includeDocument = requests === undefined ? diagnosticCode === undefined || diagnosticCode === DiagnosticCodes.LegacyOptionalSuffix : requests.some(request => request.diagnosticCode === DiagnosticCodes.LegacyOptionalSuffix);
        if (includeDocument && !documentChecked) {
            if (optional.length > 0) documentFix = verify({ diagnosticCode: DiagnosticCodes.LegacyOptionalSuffix, title: "Use 'optional' throughout this document", scope: 'document', edits: optional.flatMap(candidate => candidate.fix.edits) });
            documentChecked = true;
        }
        if (requests !== undefined) {
            if (rangeVerdicts === undefined) {
                rangeVerdicts = new Map();
                // Source order gives ordered, non-overlapping edits without sorting. Recipes replace
                // distinct nodes/lines; the batch must preserve every other modeled member and remove
                // exactly the expected diagnostics before any of its occurrence edits can be offered.
                const candidates = lines.flatMap(line => byLine.has(line.number) ? [byLine.get(line.number)!] : []);
                const edits = candidates.flatMap(candidate => candidate.fix.edits);
                const candidateSource = applyQuickFixEdits(source, edits);
                if (edits.length > 0 && candidateSource !== undefined) {
                    const parsed = parseForAuthoring(candidateSource, undefined, options.placement);
                    const changes = candidates.flatMap(candidate => candidate.change === undefined ? [] : [candidate.change]);
                    if (parsed.success && verificationShape(parsed) === verificationShape(original, changes)) {
                        const remaining = new Map<string, number>();
                        const removed = new Map<string, number>();
                        for (const diagnostic of parsed.diagnostics) remaining.set(diagnostic.code, (remaining.get(diagnostic.code) ?? 0) + 1);
                        for (const candidate of candidates) removed.set(candidate.fix.diagnosticCode, (removed.get(candidate.fix.diagnosticCode) ?? 0) + candidate.fix.edits.length);
                        if ([...removed].every(([code, count]) => (remaining.get(code) ?? 0) === (counts.get(code) ?? 0) - count)) {
                            for (const candidate of candidates) rangeVerdicts.set(candidate.line, candidate.fix);
                        }
                    }
                }
            }
            const selected = new Map<number, QuickFix>();
            for (const request of requests) {
                const fix = rangeVerdicts.get(request.line);
                if (fix?.diagnosticCode === request.diagnosticCode) selected.set(request.line, fix);
            }
            return [...selected.values(), ...(includeDocument && documentFix !== undefined ? [documentFix] : [])];
        }
        const occurrenceLine = typeof line === 'number' ? line : undefined;
        const selected = occurrenceLine === undefined ? undefined : byLine.get(occurrenceLine);
        let occurrenceFix: QuickFix | undefined;
        if (selected !== undefined && occurrenceLine !== undefined && (diagnosticCode === undefined || selected.fix.diagnosticCode === diagnosticCode)) {
            if (!verified.has(occurrenceLine)) {
                if (verified.size === 32) verified.delete(verified.keys().next().value!);
                verified.set(occurrenceLine, verify(selected.fix, selected.change));
            }
            occurrenceFix = verified.get(occurrenceLine);
        }
        return [occurrenceFix, includeDocument ? documentFix : undefined].filter((fix): fix is QuickFix => fix !== undefined);
    };
}

function verificationShape(parsed: ReturnType<typeof parseForAuthoring>, changes: readonly NonNullable<QuickFixCandidate['change']>[] = []): string {
    // Compare every modeled member (including trigger data), ignoring only source locations. A recipe
    // may replace only its original node; no other id or destination is normalized away.
    const replacements = new Map(changes.map(change => [change.node, change.replacement]));
    return JSON.stringify([parsed.value, parsed.triggerData], (key, value: unknown) => key === 'location' ? undefined : replacements.get(value as NonNullable<QuickFixCandidate['change']>['node']) ?? value);
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

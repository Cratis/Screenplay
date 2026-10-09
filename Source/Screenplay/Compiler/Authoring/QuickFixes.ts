// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PlayPlacement } from '../Files/PlayPlacement';
import { commentStart, splitLines } from '../Parsing/SourceLineSplitter';
import { parseForAuthoring } from '../ScreenplayCompiler';
import { EventSyntax } from '../Syntax/Declarations';
import { SliceSyntax } from '../Syntax/Structure';
import { TranslationDirection } from '../Syntax/TranslationDirection';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { pattern } from '../Text/patterns';
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
    return code === DiagnosticCodes.LegacyOptionalSuffix || code === DiagnosticCodes.LegacyComplianceMarker || code === DiagnosticCodes.RedundantEventId || code === DiagnosticCodes.PublicTranslationRequiresDirection;
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
    // PLAY0614 and the public-event boundary errors that follow from a missing direction are errors the repair itself removes,
    // so a document whose only errors are public-event boundary errors stays repairable.
    const publicEventCodes: ReadonlySet<string> = new Set(Array.from({ length: 14 }, (_, index) => `PLAY0${596 + index}`));
    const acceptable = (parsed: ReturnType<typeof parseForAuthoring>): boolean => parsed.success || parsed.diagnostics.every(diagnostic => diagnostic.severity !== 'error' || publicEventCodes.has(diagnostic.code));
    const errorCodes = (parsed: ReturnType<typeof parseForAuthoring>): Set<string> => new Set(parsed.diagnostics.filter(diagnostic => diagnostic.severity === 'error').map(diagnostic => diagnostic.code));
    if (!acceptable(original)) return () => [];
    const lines = splitLines(source);
    const eventsByLine = new Map<number, EventSyntax>();
    const slicesByLine = new Map<number, SliceSyntax>();
    // Index inline and standalone declarations once, without scanning the tree per diagnostic.
    const walker = new class extends ScreenplaySyntaxWalker {
        override visitEvent(event: EventSyntax): void { eventsByLine.set(event.location.line, event); }
        override visitSlice(slice: SliceSyntax): void { slicesByLine.set(slice.location.line, slice); super.visitSlice(slice); }
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
        } else if (diagnostic.code === DiagnosticCodes.LegacyComplianceMarker) {
            const edit = complianceMarkerEdit(line.raw, line.startOffset);
            if (edit !== undefined) byLine.set(line.number, { line: line.number, fix: { diagnosticCode: diagnostic.code, title: 'Use bare pii and secret compliance markers', scope: 'occurrence', edits: [edit] } });
        } else if (diagnostic.code === DiagnosticCodes.PublicTranslationRequiresDirection) {
            // PLAY0614 is only reported on the declaration line of a Translate slice that has no direction.
            const slice = slicesByLine.get(line.number)!;
            // Offer a direction only when exactly one is consistent with the slice: the other would add errors.
            const indent = /^\s*/.exec(line.raw)![0];
            const ending = source.includes('\r\n') ? '\r\n' : '\n';
            const candidates = [TranslationDirection.Inbound, TranslationDirection.Outbound].map(direction => ({ direction, text: `${ending}${indent}  direction ${direction === TranslationDirection.Inbound ? 'inbound' : 'outbound'}` }))
                .filter(candidate => {
                    // The insertion point is the end of an existing line, so the edit is always in bounds.
                    const edited = applyQuickFixEdits(source, [{ start: line.startOffset + line.raw.length, length: 0, text: candidate.text }])!;
                    const parsed = parseForAuthoring(edited, undefined, options.placement);
                    const originalCodes = errorCodes(original);
                    // Valid when the missing-direction error is gone and the direction introduces no kind of error the document lacked.
                    return acceptable(parsed) && ![...errorCodes(parsed)].some(code => code === DiagnosticCodes.PublicTranslationRequiresDirection || !originalCodes.has(code));
                });
            if (candidates.length === 1) {
                const [{ direction, text }] = candidates;
                byLine.set(line.number, { line: line.number, fix: { diagnosticCode: diagnostic.code, title: `Declare 'direction ${direction === TranslationDirection.Inbound ? 'inbound' : 'outbound'}'`, scope: 'occurrence', edits: [{ start: line.startOffset + line.raw.length, length: 0, text }] },
                    change: { node: slice, replacement: { ...slice, direction } as SliceSyntax } });
            }
        } else if (diagnostic.code === DiagnosticCodes.RedundantEventId && commentStart(line.raw) < 0) {
            const declaration = events.get(line.number);
            if (declaration?.id !== declaration?.name || declaration === undefined) continue;
            byLine.set(line.number, { line: line.number, fix: { diagnosticCode: diagnostic.code, title: 'Remove the redundant event id', scope: 'occurrence', edits: [{ start: line.startOffset, length: (lines[line.number]?.startOffset ?? source.length) - line.startOffset, text: '' }] },
                change: { node: declaration, replacement: { ...declaration, id: null } as EventSyntax } });
        }
    }
    const spellingCodes = [DiagnosticCodes.LegacyOptionalSuffix, DiagnosticCodes.LegacyComplianceMarker];
    const syntax = verificationShape(original);
    const verified = new Map<number, QuickFix | undefined>();
    let rangeVerdicts: Map<number, QuickFix> | undefined;
    const documentChecked = new Set<string>();
    const documentFixes = new Map<string, QuickFix>();
    const verify = (fix: QuickFix, change?: QuickFixCandidate['change']): QuickFix | undefined => {
        const candidate = applyQuickFixEdits(source, fix.edits);
        if (candidate === undefined) return undefined;
        const parsed = parseForAuthoring(candidate, undefined, options.placement);
        if (!acceptable(parsed) || verificationShape(parsed) !== (change === undefined ? syntax : verificationShape(original, [change]))) return undefined;
        return parsed.diagnostics.filter(diagnostic => diagnostic.code === fix.diagnosticCode).length === (counts.get(fix.diagnosticCode) ?? 0) - fix.edits.length ? fix : undefined;
    };
    return (line, diagnosticCode) => {
        const requests = typeof line === 'number' || line === undefined ? undefined : line;
        const selectedCodes = spellingCodes.filter(code => requests === undefined ? diagnosticCode === undefined || diagnosticCode === code : requests.some(request => request.diagnosticCode === code));
        for (const code of selectedCodes.filter(code => !documentChecked.has(code))) {
            const candidates = [...byLine.values()].filter(candidate => candidate.fix.diagnosticCode === code);
            if (candidates.length > 0) {
                const fix = verify({ diagnosticCode: code, title: code === DiagnosticCodes.LegacyOptionalSuffix ? "Use 'optional' throughout this document" : 'Use bare pii and secret throughout this document', scope: 'document', edits: candidates.flatMap(candidate => candidate.fix.edits) });
                if (fix !== undefined) documentFixes.set(code, fix);
            }
            documentChecked.add(code);
        }
        const documents = selectedCodes.flatMap(code => documentFixes.has(code) ? [documentFixes.get(code)!] : []);
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
                    if (acceptable(parsed) && verificationShape(parsed) === verificationShape(original, changes)) {
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
            return [...selected.values(), ...documents];
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
        return [...(occurrenceFix === undefined ? [] : [occurrenceFix]), ...documents];
    };
}

function complianceMarkerEdit(raw: string, startOffset: number): QuickFixEdit | undefined {
    // Restrict rewriting to the suffix or leading directive marker, never comments or quoted reasons.
    const header = pattern('^(\\s*concept\\s+\\w+\\s*:\\s*\\w+)((?:[^\\S\\r\\n]+@?\\w+)*)').exec(raw);
    if (header !== null) {
        const suffix = header[2];
        const text = suffix.replace(/@pii\b|@?sensitive\b/g, marker => marker === '@pii' ? 'pii' : 'secret');
        return text === suffix ? undefined : { start: startOffset + header[1].length, length: suffix.length, text };
    }
    const directive = pattern('^(\\s*)(@pii|@?sensitive)\\b').exec(raw);
    return directive === null ? undefined : { start: startOffset + directive[1].length, length: directive[2].length, text: directive[2] === '@pii' ? 'pii' : 'secret' };
}

function verificationShape(parsed: ReturnType<typeof parseForAuthoring>, changes: readonly NonNullable<QuickFixCandidate['change']>[] = []): string {
    // Compare every modeled member (including trigger data), ignoring only source locations. A recipe
    // may replace only its original node; no other id or destination is normalized away.
    const replacements = new Map(changes.map(change => [change.node, change.replacement]));
    return JSON.stringify([parsed.value, parsed.triggerData], (key, value: unknown) => ['location', 'usesLocation', 'targetLocation'].includes(key) ? undefined : replacements.get(value as NonNullable<QuickFixCandidate['change']>['node']) ?? value);
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

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { authoredOrderKey, sameAuthoredStep } from '../Files/AuthoredOrder';
import { compileApplication } from '../Files/PlayApplicationAssembly';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { QuickFix, QuickFixOptions } from './QuickFixes';
import { QuickFixCandidate } from './QuickFixCandidate';
import { TimelineQuickFixContext, moveTimelineText } from './TimelineQuickFixContext';
import { timelineImportFix } from './TimelineQuickFixImports';
import { SyntaxNode } from '../Syntax/SyntaxNode';

/** Owns one immutable application snapshot. No recipe is derived from diagnostic message text. */
export function prepareTimelineQuickFixes(source: string, options: QuickFixOptions): (line: number) => QuickFix[] {
    const path = options.path ?? 'application.play';
    const documents = new Map(options.documents ?? [[path, source]]);
    if (documents.get(path) !== source || (options.documents === undefined && (options.placement?.length ?? 0) > 0)) return () => [];
    const compiled = compileApplication(documents);
    if (!compiled.success || compiled.documents.some(document => document.isPlacementResolved === false)) return () => [];
    const context = new TimelineQuickFixContext(documents, compiled);
    const cache = new Map<number, QuickFix[]>();
    return line => {
        if (cache.has(line)) return cache.get(line)!;
        const findings = context.findings.filter(finding => finding.diagnostic.code === DiagnosticCodes.EventFromLaterSlice && finding.diagnostic.location.path === path && finding.diagnostic.location.line === line);
        const candidates: QuickFix[] = [];
        // Several event references on one line cannot be addressed unambiguously by this line-only API.
        if (findings.length === 1 && !findings[0].ownSubFeature) {
            const finding = findings[0];
            const consumer = context.origins.get(authoredOrderKey(finding.consumerScope));
            const producer = context.origins.get(authoredOrderKey(finding.producerScope));
            if (consumer !== undefined && producer !== undefined) {
                let common = 0;
                while (common < consumer.length && common < producer.length && sameAuthoredStep(consumer[common], producer[common])) common++;
                const left = consumer[common], right = producer[common];
                if (left !== undefined && right !== undefined && left.path === right.path) {
                    const entries = context.entries(left.path);
                    const first = context.entry(left, entries), second = context.entry(right, entries);
                    if (first !== undefined && second !== undefined && first.parent === second.parent && first.member === second.member) {
                        let candidate: QuickFixCandidate | undefined;
                        if (first.index !== second.index) {
                            for (const after of [false, true]) {
                                const moved = after ? first : second, anchor = after ? second : first;
                                const movedStep = after ? left : right, anchorStep = after ? right : left;
                                const ranks = context.move(context.block(movedStep, movedStep.node.kind === 'FileImportSyntax'), context.block(anchorStep, anchorStep.node.kind === 'FileImportSyntax'), after);
                                if (ranks === undefined || !context.safe(ranks, finding)) continue;
                                const siblings = [...(first.parent as unknown as Record<string, readonly SyntaxNode[]>)[first.member]];
                                siblings.splice(moved.index, 1);
                                siblings.splice(siblings.indexOf(anchor.node) + (after ? 1 : 0), 0, moved.node);
                                const replacement = { ...first.parent, [first.member]: siblings };
                                const original = documents.get(left.path)!;
                                candidate = { line, change: { node: first.parent, replacement }, fix: {
                                    diagnosticCode: finding.diagnostic.code, line, title: finding.readModel ? 'Draw the read-model builder before its reader' : 'Draw the event producer before its consumer', scope: 'occurrence',
                                    edits: [{ path: left.path, start: 0, length: original.length, text: moveTimelineText(original, moved.node, anchor.node, after) }],
                                } };
                                if (context.verify(candidate, left.path, ranks, finding)) break;
                                candidate = undefined;
                            }
                        }
                        if (candidate === undefined && left.node.kind === 'FileImportSyntax' && right.node.kind === 'FileImportSyntax') candidate = timelineImportFix(context, finding, left, right, first, second);
                        if (candidate !== undefined) candidates.push(options.documents === undefined ? { ...candidate.fix, edits: candidate.fix.edits.map(({ path: _path, ...edit }) => edit) } : candidate.fix);
                    }
                }
            }
        }
        if (cache.size === 32) cache.delete(cache.keys().next().value!);
        cache.set(line, candidates);
        return candidates;
    };
}

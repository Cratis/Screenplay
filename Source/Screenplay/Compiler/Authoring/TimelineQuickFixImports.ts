// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoredOrderStep } from '../Files/AuthoredOrder';
import { TimelineFinding } from '../Files/TimelineOrder';
import { matchesPlayPattern, resolvePlayPattern } from '../Files/PlayGlob';
import { FileImportSyntax } from '../Syntax/Structure';
import { commentStart, splitLines } from '../Parsing/SourceLineSplitter';
import { QuickFixCandidate } from './QuickFixCandidate';
import { TimelineEntry, TimelineQuickFixContext } from './TimelineQuickFixContext';

const hasMetacharacters = (path: string) => /[*?[\]{}]/.test(path);
const relativePath = (document: string, target: string): string => {
    const folder = document.split('/').slice(0, -1), parts = target.split('/');
    while (folder.length > 0 && folder[0] === parts[0]) { folder.shift(); parts.shift(); }
    return [...folder.map(() => '..'), ...parts].join('/');
};

export function timelineImportFix(context: TimelineQuickFixContext, finding: TimelineFinding, left: AuthoredOrderStep, right: AuthoredOrderStep, entry: TimelineEntry, other: TimelineEntry): QuickFixCandidate | undefined {
    const source = context.documents.get(left.path)!;
    const lines = splitLines(source);
    const line = lines[entry.node.location.line - 1];
    const indent = /^\s*/.exec(line.raw)![0];
    const ending = source.includes('\r\n') ? '\r\n' : '\n';
    const imports = (entry.parent as unknown as { fileImports: readonly FileImportSyntax[] }).fileImports;
    const propose = (paths: readonly string[], ranks: ReadonlyMap<string, number>, remove: number, title: string): QuickFixCandidate | undefined => {
        if (paths.some(hasMetacharacters)) return undefined;
        const nodes = paths.map(path => ({ kind: 'FileImportSyntax' as const, pattern: relativePath(left.path, path), location: entry.node.location }));
        const replacement = { ...entry.parent, fileImports: [...imports.slice(0, entry.index), ...nodes, ...imports.slice(entry.index + remove)] };
        const last = remove === 0 ? line.startOffset : lines[imports[entry.index + remove - 1].location.line]?.startOffset ?? source.length;
        // Keep comments attached to each replaced import exactly once. Pins leave the original line intact.
        const importLines = new Set(imports.slice(entry.index, entry.index + remove).map(imported => imported.location.line));
        const comments = remove === 0 ? [] : lines.filter(candidate => candidate.startOffset >= line.startOffset && candidate.startOffset < last).flatMap(candidate => {
            if (!importLines.has(candidate.number)) return [source.slice(candidate.startOffset, lines[candidate.number]?.startOffset ?? source.length)];
            const comment = commentStart(candidate.raw);
            return comment < 0 ? [] : [`${indent}${candidate.raw.slice(comment)}${ending}`];
        });
        const text = comments.join('') + nodes.map(node => `${indent}import ${JSON.stringify(node.pattern)}${ending}`).join('');
        const candidate: QuickFixCandidate = { line: finding.diagnostic.location.line, change: { node: entry.parent, replacement }, fix: {
            diagnosticCode: finding.diagnostic.code, title, scope: 'occurrence', line: finding.diagnostic.location.line,
            edits: [{ path: left.path, start: line.startOffset, length: last - line.startOffset, text }],
        } };
        return context.verify(candidate, left.path, ranks, finding) ? candidate : undefined;
    };
    if (entry.index === other.index) {
        const steps = [...new Map([...context.origins.values()].flatMap(chain => chain).filter(step => step.path === left.path && step.node.kind === 'FileImportSyntax' && step.node.location.line === left.node.location.line).map(step => [step.targetPath!, step])).values()].sort((left, right) => left.matchIndex! - right.matchIndex!);
        if (steps.length < 2) return undefined;
        const paths = steps.map(step => step.targetPath!);
        const keys = [...context.ranks].sort((left, right) => left[1] - right[1]).map(([key]) => key);
        const all = steps.flatMap(step => context.block(step));
        const position = keys.indexOf(all[0]);
        if (!all.every((key, index) => keys[position + index] === key)) return undefined;
        const ranksFor = (prefix: readonly string[]) => {
            const ordered = [...prefix, ...paths.filter(path => !prefix.includes(path))].flatMap(path => context.block(steps.find(step => step.targetPath === path)!));
            const result = [...keys];
            result.splice(position, all.length, ...ordered);
            return new Map(result.map((key, index) => [key, index]));
        };
        const single = ranksFor([right.targetPath!]);
        if (context.safe(single, finding)) return propose([right.targetPath!], single, 0, 'Pin timeline order before the glob (new files remain imported)');
        for (const after of [false, true]) {
            const desired = [...paths];
            const moved = after ? left.targetPath! : right.targetPath!, anchor = after ? right.targetPath! : left.targetPath!;
            desired.splice(desired.indexOf(moved), 1);
            desired.splice(desired.indexOf(anchor) + (after ? 1 : 0), 0, moved);
            const ranks = ranksFor(desired);
            if (!context.safe(ranks, finding)) continue;
            for (let count = 1; count <= desired.length; count++) {
                const prefix = desired.slice(0, count);
                const candidate = ranksFor(prefix);
                if ([...ranks].some(([key, rank]) => candidate.get(key) !== rank)) continue;
                return propose(prefix, candidate, 0, 'Pin timeline order before the glob (new files remain imported)');
            }
        }
        return undefined;
    }
    // Freeze only the affected adjacent import interval. Include files with no ranked declaration.
    if (entry.index > other.index) return undefined;
    const selected = imports.slice(entry.index, other.index + 1);
    if (!selected.some(imported => hasMetacharacters(imported.pattern))) return undefined;
    const paths = selected.flatMap(imported => context.compiled.documents.filter(document => document.path !== left.path && matchesPlayPattern(resolvePlayPattern(left.path, imported.pattern), document.path)).map(document => document.path).sort());
    if (new Set(paths).size !== paths.length) return undefined;
    for (const after of [false, true]) {
        const ranks = context.move(context.block(after ? left : right), context.block(after ? right : left), after);
        if (ranks === undefined || !context.safe(ranks, finding)) continue;
        const desired = [...paths];
        const moved = after ? left.targetPath! : right.targetPath!, anchor = after ? right.targetPath! : left.targetPath!;
        if (!desired.includes(moved) || !desired.includes(anchor)) continue;
        desired.splice(desired.indexOf(moved), 1);
        desired.splice(desired.indexOf(anchor) + (after ? 1 : 0), 0, moved);
        const candidate = propose(desired, ranks, selected.length, 'Replace timeline globs with ordered imports (new files need explicit imports)');
        if (candidate !== undefined) return candidate;
    }
    return undefined;
}

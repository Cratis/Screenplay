// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandSyntax, Diagnostic, parse, parseFolder, SpecificationSyntax } from '@cratis/screenplay-compiler';
import { fenceMap, indentOf } from './document-context';
import { ResponseAnalysis } from './ResponseAnalysis';

// Public editor shapes are structural: packed declarations never expose the private compiler.
export type { AnalysisLocation } from './AnalysisLocation';
export type { AnalysisDiagnostic } from './AnalysisDiagnostic';
export type { AnalysisType } from './AnalysisType';
export type { AnalysisCommand } from './AnalysisCommand';
export type { AnalysisSpecification } from './AnalysisSpecification';
export type { CommandResponseSymbol } from './CommandResponseSymbol';
export type { ResponseFieldSymbol } from './ResponseFieldSymbol';
export type { ResponseSourceSymbol } from './ResponseSourceSymbol';
export type { ResponseAnalysis } from './ResponseAnalysis';

export const responseAvailability = 'Syntax-only; execution unavailable until ESM v8 (PLAY0268). No response type is emitted.';

// A bounded revision cache shared by symbols, validation, completion and hover. Index typed nodes
// once, not by reparsing the document for each command or response field.
const revisions = new Map<string, ReturnType<typeof analyze>>();

function authoringSource(lines: string[]) {
    const fences = fenceMap(lines);
    const roots = lines.flatMap((line, index) => !fences[index] && indentOf(line) === 0 && /^\w/.test(line) ? [line.split(/\s+/)[0]] : []);
    if (roots.includes('module') || !roots.some(root => ['feature', 'slice', 'command', 'event', 'specification'].includes(root))) {
        return { source: lines.join('\n'), locations: lines.map((_, index) => ({ line: index + 1, column: 0 })) };
    }
    // Isolated slice/command fragments are a supported editor input. Global declarations stay global.
    const globals: number[] = [];
    const body: number[] = [];
    let global = false;
    lines.forEach((line, index) => {
        if (!fences[index] && indentOf(line) === 0 && /^\w/.test(line)) global = /^(?:concept|type|import|domain)\b/.test(line);
        (global ? globals : body).push(index);
    });
    const depth = roots.includes('feature') ? 1 : roots.includes('slice') ? 2 : 3;
    const headers = ['module Editor', '  feature Authoring', '    slice StateChange Fragment'].slice(0, depth);
    const source = [...globals.map(index => lines[index]), ...headers, ...body.map(index => ' '.repeat(depth * 2) + lines[index])].join('\n');
    const locations = [...globals.map(index => ({ line: index + 1, column: 0 })), ...headers.map(() => ({ line: 0, column: 0 })), ...body.map(index => ({ line: index + 1, column: depth * 2 }))];
    return { source, locations };
}

function analyze(lines: string[], otherSources: readonly string[], placement?: readonly string[]): ResponseAnalysis {
    const prepared = placement?.length ? { source: lines.join('\n'), locations: lines.map((_, index) => ({ line: index + 1, column: 0 })) } : authoringSource(lines);
    const parsed = otherSources.length === 0 ? parse(prepared.source, 'current.play', placement) : parseFolder([
        { path: 'current.play', source: prepared.source },
        ...otherSources.map((source, index) => ({ path: `other-${index}.play`, source: authoringSource(source.split('\n')).source })),
    ]);
    const commands = new Map<number, CommandSyntax>();
    const specifications = new Map<number, SpecificationSyntax>();
    const walk = (value: unknown): void => {
        if (value === null || typeof value !== 'object') return;
        if (Array.isArray(value)) { value.forEach(walk); return; }
        const node = value as Record<string, unknown>;
        const location = node.location as { path?: string; line: number; column: number } | undefined;
        if (location?.path === 'current.play') {
            const original = prepared.locations[location.line - 1];
            if (original) node.location = { ...location, line: original.line, column: Math.max(1, location.column - original.column) };
        }
        if ((node.location as { path?: string; line: number } | undefined)?.path === 'current.play') {
            if (node.kind === 'CommandSyntax') commands.set((node.location as { line: number }).line - 1, value as CommandSyntax);
            if (node.kind === 'SpecificationSyntax') specifications.set((node.location as { line: number }).line - 1, value as SpecificationSyntax);
        }
        for (const [key, child] of Object.entries(node)) if (key !== 'location') walk(child);
    };
    walk(parsed.value);
    const diagnostics: Diagnostic[] = parsed.diagnostics.filter(diagnostic => diagnostic.location.path === 'current.play').flatMap(diagnostic => {
        const original = prepared.locations[diagnostic.location.line - 1];
        return original?.line ? [{ ...diagnostic, location: { ...diagnostic.location, line: original.line, column: Math.max(1, diagnostic.location.column - original.column) } }] : [];
    });
    return { commands, specifications, diagnostics };
}

export function responseAnalysis(lines: string[], otherSources: readonly string[] = [], placement?: readonly string[]): ResponseAnalysis {
    const key = JSON.stringify([lines, otherSources, placement]);
    let analysis = revisions.get(key);
    if (analysis === undefined) {
        analysis = analyze(lines, otherSources, placement);
        if (revisions.size >= 16) revisions.delete(revisions.keys().next().value!);
        revisions.set(key, analysis);
    }
    return analysis;
}

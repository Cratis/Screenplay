// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionResolver, CommandSyntax, Diagnostic, parse, parsePlacedDocuments, SpecificationSyntax } from '@cratis/screenplay-compiler';
import { fenceMap, indentOf } from './document-context';
import { ResponseAnalysis } from './ResponseAnalysis';
import { AuthoringDocument } from './AuthoringDocument';

// Public editor shapes are structural: packed declarations never expose the private compiler.
export type { AuthoringDocument } from './AuthoringDocument';
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

function authoringSource(lines: string[], headers: readonly string[]) {
    const fences = fenceMap(lines);
    const roots = lines.flatMap((line, index) => !fences[index] && indentOf(line) === 0 && /^\w/.test(line) ? [line.split(/\s+/)[0]] : []);
    if (roots.includes('module') || !roots.some(root => ['feature', 'slice', 'command', 'event', 'operation', 'specification'].includes(root))) {
        return { source: lines.join('\n'), locations: lines.map((_, index) => ({ line: index + 1, column: 0 })) };
    }
    // Isolated slice/command fragments are a supported editor input. Global declarations stay global.
    const globals: number[] = [];
    const body: number[] = [];
    let global = false;
    lines.forEach((line, index) => {
        if (!fences[index] && indentOf(line) === 0 && /^\w/.test(line)) global = /^(?:concept|type|import|domain|system)\b/.test(line);
        (global ? globals : body).push(index);
    });
    const depth = roots.includes('feature') ? 1 : roots.includes('slice') ? 2 : 3;
    const wrappers = headers.slice(0, depth);
    const source = [...globals.map(index => lines[index]), ...wrappers, ...body.map(index => ' '.repeat(depth * 2) + lines[index])].join('\n');
    const locations = [...globals.map(index => ({ line: index + 1, column: 0 })), ...wrappers.map(() => ({ line: 0, column: 0 })), ...body.map(index => ({ line: index + 1, column: depth * 2 }))];
    return { source, locations };
}

function analyze(lines: string[], otherSources: readonly (string | AuthoringDocument)[], placement?: readonly string[], path = 'current.play'): ResponseAnalysis {
    const others = otherSources.map((document, index) => typeof document === 'string' ? { path: `other-${index}.play`, source: document } : document).filter(document => document.path !== path);
    const documents: AuthoringDocument[] = [{ path, source: lines.join('\n'), placement }, ...others];
    // Modules/features merge, but slices are real declarations: equal slice names discard later
    // documents. Share only fresh synthetic module/feature names, with a distinct synthetic slice
    // per fragment. This retains all commands for normal (including ambiguous) scope resolution,
    // without combining source texts or renaming real declarations/explicit placements. Reserve
    // source/placement names once so even a real Editor.Authoring.Fragment cannot collide.
    const names = new Set(documents.flatMap(document => [...document.source.matchAll(/\w+/g)].map(match => match[0]).concat(document.placement ?? [])));
    const fresh = (prefix: string): string => {
        let name = prefix;
        for (let suffix = 1; names.has(name); suffix++) name = `${prefix}${suffix}`;
        names.add(name);
        return name;
    };
    const module = fresh('EditorAuthoring');
    const feature = fresh('FragmentFeature');
    const preparedDocuments = documents.map((document, index) => {
        const sourceLines = index === 0 ? lines : document.source.split('\n');
        const prepared = document.placement?.length ? { source: document.source, locations: sourceLines.map((_, line) => ({ line: line + 1, column: 0 })) }
            : authoringSource(sourceLines, [`module ${module}`, `  feature ${feature}`, `    slice StateChange ${fresh(`Fragment${index}`)}`]);
        return { ...document, ...prepared, placement: document.placement ?? [] };
    });
    const prepared = preparedDocuments[0];
    // Put the buffer after its supplied context so duplicate real declarations are reported at
    // the current source, not silently dropped from its source-local diagnostic view.
    const parsed = others.length === 0 ? parse(prepared.source, path, placement) : parsePlacedDocuments([...preparedDocuments.slice(1), prepared]);
    const commands = new Map<number, CommandSyntax>();
    const specifications = new Map<number, SpecificationSyntax>();
    const walk = (value: unknown): void => {
        if (value === null || typeof value !== 'object') return;
        if (Array.isArray(value)) { value.forEach(walk); return; }
        const node = value as Record<string, unknown>;
        const location = node.location as { path?: string; line: number; column: number } | undefined;
        if (location?.path === path) {
            const original = prepared.locations[location.line - 1];
            if (original) node.location = { ...location, line: original.line, column: Math.max(1, location.column - original.column) };
        }
        if ((node.location as { path?: string; line: number } | undefined)?.path === path) {
            if (node.kind === 'CommandSyntax') commands.set((node.location as { line: number }).line - 1, value as CommandSyntax);
            if (node.kind === 'SpecificationSyntax') specifications.set((node.location as { line: number }).line - 1, value as SpecificationSyntax);
        }
        for (const [key, child] of Object.entries(node)) if (key !== 'location') walk(child);
    };
    walk(parsed.value);
    const diagnostics: Diagnostic[] = parsed.diagnostics.filter(diagnostic => diagnostic.location.path === path).flatMap(diagnostic => {
        const original = prepared.locations[diagnostic.location.line - 1];
        return original?.line ? [{ ...diagnostic, location: { ...diagnostic.location, line: original.line, column: Math.max(1, diagnostic.location.column - original.column) } }] : [];
    });
    const productions = new AuthoringProductionResolver(parsed.value);
    const operationProductionLines = new Set(productions.slices.flatMap(({ slice }) => slice.commands.flatMap(command => command.produces)
        .filter(production => production.location.path === path && !productions.isEventProduction(production, slice))
        .map(production => production.location.line - 1)));
    return { commands, specifications, diagnostics, operationProductionLines };
}

export function responseAnalysis(lines: string[], otherSources: readonly (string | AuthoringDocument)[] = [], placement?: readonly string[], path = 'current.play'): ResponseAnalysis {
    const key = JSON.stringify([lines, otherSources, placement, path]);
    let analysis = revisions.get(key);
    if (analysis === undefined) {
        analysis = analyze(lines, otherSources, placement, path);
        if (revisions.size >= 16) revisions.delete(revisions.keys().next().value!);
        revisions.set(key, analysis);
    }
    return analysis;
}

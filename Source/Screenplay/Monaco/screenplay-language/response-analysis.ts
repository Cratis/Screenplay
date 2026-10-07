// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionKind, AuthoringProductionResolver, CommandSyntax, Diagnostic, EventSourceReadConfidence, OperationSyntax, parsePlacedDocuments, SpecificationEventSyntax, SpecificationSyntax } from '@cratis/screenplay-compiler';
import { fenceMap, indentOf, withoutComment } from './document-context';
import { ResponseAnalysis } from './ResponseAnalysis';
import { EventSourceAnalysis } from './EventSourceAnalysis';
import { AuthoredCommandRoute } from './AuthoredCommandRoute';
import { AuthoringDocument } from './AuthoringDocument';
import { OperationAnalysis, OperationDeclaration, OperationReference } from './OperationAnalysis';

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

export const responseAvailability = 'Executable as ESM v7. Generated values require fixtures in reference execution; other unadmitted constructs still prevent binding.';

// A bounded revision cache shared by symbols, validation, completion and hover. Index typed nodes
// once, not by reparsing the document for each command or response field.
const revisions = new Map<string, ReturnType<typeof analyze>>();

function authoringSource(lines: string[], headers: readonly string[]) {
    const fences = fenceMap(lines);
    const roots = lines.flatMap((line, index) => !fences[index] && indentOf(line) === 0 && /^\w/.test(line) ? [line.split(/\s+/)[0]] : []);
    if (roots.includes('module') || !roots.some(root => ['feature', 'slice', 'command', 'event', 'operation', 'reaction', 'specification'].includes(root))) {
        return { source: lines.join('\n'), locations: lines.map((_, index) => ({ line: index + 1, column: 0 })) };
    }
    // Isolated slice/command fragments are a supported editor input. Global declarations stay global.
    const globals: number[] = [];
    const body: number[] = [];
    let global = false;
    lines.forEach((line, index) => {
        if (!fences[index] && indentOf(line) === 0 && /^\w/.test(line)) global = /^(?:concept|type|import|domain|system|eventsource)\b/.test(line);
        (global ? globals : body).push(index);
    });
    const depth = roots.includes('feature') ? 1 : roots.includes('slice') ? 2 : 3;
    const wrappers = headers.slice(0, depth);
    const source = [...globals.map(index => lines[index]), ...wrappers, ...body.map(index => ' '.repeat(depth * 2) + lines[index])].join('\n');
    const locations = [...globals.map(index => ({ line: index + 1, column: 0 })), ...wrappers.map(() => ({ line: 0, column: 0 })), ...body.map(index => ({ line: index + 1, column: depth * 2 }))];
    return { source, locations };
}

function analyze(lines: string[], otherSources: readonly (string | AuthoringDocument)[], placement?: readonly string[], path = 'current.play', isPlacementResolved = true): ResponseAnalysis & { readonly operations: OperationAnalysis; readonly eventSources: EventSourceAnalysis } {
    const others = otherSources.map((document, index) => typeof document === 'string' ? { path: `other-${index}.play`, source: document } : document).filter(document => document.path !== path);
    const documents: AuthoringDocument[] = [{ path, source: lines.join('\n'), placement, isPlacementResolved }, ...others];
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
    const syntheticScopes = new Set([module, feature]);
    const preparedDocuments = documents.map((document, index) => {
        const sourceLines = index === 0 ? lines : document.source.split('\n');
        const fragment = fresh(`Fragment${index}`);
        syntheticScopes.add(fragment);
        const prepared = document.placement?.length ? { source: document.source, locations: sourceLines.map((_, line) => ({ line: line + 1, column: 0 })) }
            : authoringSource(sourceLines, [`module ${module}`, `  feature ${feature}`, `    slice StateChange ${fragment}`]);
        return { ...document, ...prepared, placement: document.placement ?? [] };
    });
    const prepared = preparedDocuments[0];
    const locations = new Map(preparedDocuments.map(document => [document.path, document.locations]));
    // Put the buffer after its supplied context so duplicate real declarations are reported at
    // the current source, not silently dropped from its source-local diagnostic view.
    const parsed = parsePlacedDocuments([...preparedDocuments.slice(1), prepared]);
    const commands = new Map<number, CommandSyntax>();
    const specifications = new Map<number, SpecificationSyntax>();
    const visited = new WeakSet<object>();
    const walk = (value: unknown): void => {
        if (value === null || typeof value !== 'object' || visited.has(value)) return;
        visited.add(value);
        if (Array.isArray(value)) { value.forEach(walk); return; }
        const node = value as Record<string, unknown>;
        // Source locations also occur as standalone members (usesLocation, targetLocation).
        // Mutate each original identity once: shared locations must not be shifted twice.
        if (typeof node.path === 'string' && typeof node.line === 'number' && typeof node.column === 'number') {
            const original = locations.get(node.path)?.[node.line - 1];
            if (original) { node.line = original.line; node.column = Math.max(1, node.column - original.column); }
            return;
        }
        for (const child of Object.values(node)) walk(child);
        if ((node.location as { path?: string; line: number } | undefined)?.path === path) {
            if (node.kind === 'CommandSyntax') commands.set((node.location as { line: number }).line - 1, value as CommandSyntax);
            if (node.kind === 'SpecificationSyntax') specifications.set((node.location as { line: number }).line - 1, value as SpecificationSyntax);
        }
    };
    walk(parsed.value);
    walk(parsed.physicalEventSources);
    // Fragment wrappers must not turn an unread original root into confidence evidence.
    // Retain the parser's unknown-owner diagnostic at its original physical root location.
    for (const diagnostic of parsed.diagnostics) walk(diagnostic.location);
    const unknownFragmentExtent = parsed.diagnostics.some(diagnostic => {
        if (!['PLAY0022', 'PLAY0024', 'PLAY0029'].includes(diagnostic.code)) return false;
        const document = preparedDocuments.find(document => document.path === diagnostic.location.path);
        const original = documents.find(document => document.path === diagnostic.location.path)?.source.split('\n')[diagnostic.location.line - 1];
        return document?.locations.some(location => location.line === 0) && original !== undefined && indentOf(original) === 0;
    });
    const diagnostics: Diagnostic[] = parsed.diagnostics.filter(diagnostic => diagnostic.location.path === path).flatMap(diagnostic => {
        walk(diagnostic.location);
        return diagnostic.location.line > 0 ? [diagnostic] : [];
    });
    const productions = new AuthoringProductionResolver(parsed.value);
    const operationProductionLines = new Set(productions.slices.flatMap(({ slice }) => [...slice.commands.flatMap(command => command.produces), ...slice.reactions.flatMap(reaction => reaction.triggers).flatMap(trigger => trigger.produces)]
        .filter(production => production.location.path === path && !productions.isEventProduction(production, slice))
        .map(production => production.location.line - 1)));
    // Rejected inline operation declarations have no syntax node, but the core parser still owns
    // the command-only kind fact. Retain it rather than suggesting an event declaration repair.
    for (const diagnostic of diagnostics) if (diagnostic.code === 'PLAY0499') operationProductionLines.add(diagnostic.location.line - 1);
    const declarations: OperationDeclaration[] = productions.declarations.filter(declaration => declaration.kind === AuthoringProductionKind.Operation)
        .map(declaration => ({ ...declaration.node as OperationSyntax, scope: declaration.scope.filter(segment => !syntheticScopes.has(segment)) }));
    const contexts = new Map<number, { scope: readonly string[]; commandLine?: number; operation?: OperationDeclaration; production?: OperationReference }>();
    // Index source ownership once per revision. Typed headers, not keyword-shaped properties,
    // define the ranges; greater-indented legacy properties remain leaves.
    const fences = fenceMap(lines);
    const range = (start: number): number[] => {
        const result: number[] = [];
        const indent = indentOf(lines[start]);
        for (let line = start; line < lines.length; line++) {
            if (line > start && !fences[line] && withoutComment(lines[line]).trim() && indentOf(lines[line]) <= indent) break;
            result.push(line);
        }
        return result;
    };
    for (const { slice, scope } of productions.slices) {
        if (slice.location.path === path && slice.location.line > 0) for (const line of range(slice.location.line - 1)) contexts.set(line, { scope });
        for (const specification of slice.specifications.filter(specification => specification.location.path === path && specification.location.line > 0)) {
            for (const line of range(specification.location.line - 1)) contexts.set(line, { scope });
        }
        for (const command of slice.commands.filter(command => command.location.path === path && command.location.line > 0)) {
            for (const line of range(command.location.line - 1)) contexts.set(line, { scope, commandLine: command.location.line - 1 });
        }
    }
    const declarationScopes = new Map(productions.declarations.map(declaration => [declaration.node.location, declaration.scope]));
    for (const operation of declarations.filter(operation => operation.location.path === path)) {
        const scope = declarationScopes.get(operation.location)!;
        for (const line of range(operation.location.line - 1)) contexts.set(line, { ...contexts.get(line), scope, operation });
    }
    const declarationsByLocation = new Map(declarations.map(declaration => [declaration.location, declaration]));
    const referenceLocation = (location: { path?: string; line: number; column: number }, name: string) => {
        const source = withoutComment(lines[location.line - 1] ?? '');
        const prefix = source.slice(location.column - 1).match(/^(?:given\s+operation|then\s+(?:operation|compensated))\s+/)?.[0];
        return prefix && source.slice(location.column - 1 + prefix.length, location.column - 1 + prefix.length + name.length) === name
            ? { ...location, column: location.column + prefix.length } : location;
    };
    const references: OperationReference[] = [];
    for (const { slice } of productions.slices) {
        for (const command of slice.commands) for (const production of command.produces) {
            if (production.location.path !== path) continue;
            const resolved = productions.resolve(production.event, slice);
            const declaration = resolved.declaration ? declarationsByLocation.get(resolved.declaration.node.location) ?? null : null;
            const reference = { name: production.event, location: production.location, targetLocation: production.targetLocation ?? production.location, kind: resolved.kind, declaration, mappings: production.mappings };
            references.push(reference);
            if (resolved.kind === AuthoringProductionKind.Operation) {
                for (const line of range(production.location.line - 1)) {
                    const context = contexts.get(line);
                    if (context) contexts.set(line, { ...context, production: reference });
                }
            }
        }
        for (const specification of slice.specifications) {
            for (const step of [...specification.givenOperationFailures ?? [], ...specification.thenOperations ?? [], ...specification.thenCompensated ?? []]) {
                if (step.location.path !== path) continue;
                const resolved = productions.resolve(step.operation, slice);
                references.push({ name: step.operation, location: step.location, targetLocation: referenceLocation(step.location, step.operation), kind: resolved.kind,
                    declaration: resolved.declaration ? declarationsByLocation.get(resolved.declaration.node.location) ?? null : null,
                    mappings: [] });
            }
        }
    }
    const operations: OperationAnalysis = { declarations, systems: parsed.value.systems ?? [], references, contexts, types: parsed.value.types, concepts: parsed.value.concepts,
        targets: scope => {
            const slice = productions.slices.find(entry => entry.scope.length === scope.length && entry.scope.every((part, index) => part === scope[index]))?.slice;
            if (!slice) return [];
            return declarations.flatMap(declaration => {
                const name = productions.resolve(declaration.name, slice).declaration?.node.location === declaration.location
                    ? declaration.name : [...declaration.scope, declaration.name].join('.');
                return productions.resolve(name, slice).kind === AuthoringProductionKind.Operation &&
                    productions.resolve(name, slice).declaration?.node.location === declaration.location ? [{ name, declaration }] : [];
            });
        }
    };
    const sourceDeclarations = parsed.physicalEventSources.map(entry => entry.source);
    const sourceCatalog = new EventSourceReadConfidence(parsed.physicalEventSources, parsed.sourceInventoryComplete && !unknownFragmentExtent);
    const resolutions = new Map<string, ReturnType<EventSourceReadConfidence['resolve']>>();
    const resolve = (source: string, stream?: string) => {
        const key = JSON.stringify([source, stream]);
        let result = resolutions.get(key);
        if (!result) { result = sourceCatalog.resolve(source, stream); resolutions.set(key, result); }
        return { ...result, source: result.state === 'unique' ? result.sources[0] : undefined, stream: result.state === 'unique' ? result.streams[0] : undefined };
    };
    const importedTypeReferences = new Set(parsed.value.imports.map(imported => imported.qualifiedName));
    const targets = sourceDeclarations.flatMap(source => {
        return source.streams.filter(stream => resolve(source.name, stream.name).state === 'unique' && !importedTypeReferences.has(`${source.name}.${stream.name}`)).map(stream => ({ name: `${source.name}.${stream.name}`, source, stream }));
    });
    const sourceContexts = new Map<number, { command?: CommandSyntax; event?: SpecificationEventSyntax; expectation?: boolean; route?: AuthoredCommandRoute; source?: typeof sourceDeclarations[number]; stream?: typeof sourceDeclarations[number]['streams'][number] }>();
    const routes: AuthoredCommandRoute[] = [];
    for (const command of commands.values()) {
        for (const line of range(command.location.line - 1)) sourceContexts.set(line, { command });
        for (const member of [...command.produces, ...command.validations, ...(command.handler ? [command.handler] : [])])
            for (const line of range(member.location.line - 1)) sourceContexts.delete(line);
        // Retained ambiguity candidates are not selected authored routes.
        if (command.stream) {
            routes.push(command.stream);
            for (const line of range(command.stream.location.line - 1)) sourceContexts.set(line, { command, route: command.stream });
        }
    }
    for (const specification of specifications.values()) {
        for (const event of [...specification.given, ...(specification.whenAppended ? [specification.whenAppended] : []), ...specification.thenEvents]) {
            const context = { event, expectation: specification.thenEvents.includes(event) };
            for (const line of range(event.location.line - 1)) sourceContexts.set(line, context);
            if (event.stream) {
                routes.push(event.stream);
                for (const line of range(event.stream.location.line - 1)) sourceContexts.set(line, { ...context, route: event.stream });
            }
        }
    }
    for (const source of sourceDeclarations.filter(source => source.location.path === path)) {
        for (const line of range(source.location.line - 1)) sourceContexts.set(line, { source });
        for (const stream of source.streams) for (const line of range(stream.location.line - 1)) sourceContexts.set(line, { source, stream });
    }
    const ambiguousCandidates = [...commands.values()].flatMap(command => command.streamCandidates ?? []).filter(candidate => candidate.propertyCandidate !== null);
    const eventSources: EventSourceAnalysis = { declarations: sourceDeclarations, routes, ambiguousCandidates, contexts: sourceContexts, targets, resolve };
    return { commands, specifications, diagnostics, operationProductionLines, operations, eventSources };
}

export function responseAnalysis(lines: string[], otherSources: readonly (string | AuthoringDocument)[] = [], placement?: readonly string[], path = 'current.play', isPlacementResolved = true): ResponseAnalysis & { readonly operations: OperationAnalysis; readonly eventSources: EventSourceAnalysis } {
    const key = JSON.stringify([lines, otherSources, placement, path, isPlacementResolved]);
    let analysis = revisions.get(key);
    if (analysis === undefined) {
        analysis = analyze(lines, otherSources, placement, path, isPlacementResolved);
        if (revisions.size >= 16) revisions.delete(revisions.keys().next().value!);
        revisions.set(key, analysis);
    }
    return analysis;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { validateInlineEvents } from '../Parsing/InlineEventValidator';
import { LineReader } from '../Parsing/LineReader';
import { CommandStreamCandidates } from '../Parsing/CommandStreamCandidates';
import { validateEventSources } from '../Parsing/EventSourceValidator';
import { splitLines } from '../Parsing/SourceLineSplitter';
import { ParserContext } from '../Parsing/ParserContext';
import { validateResponses } from '../Parsing/ResponseValidator';
import { validateOperations } from '../Parsing/OperationValidator';
import { CompilationResult, parseForAuthoring } from '../ScreenplayCompiler';
import { ApplicationSyntax } from '../Syntax/Structure';
import { EventSourceSyntax } from '../Syntax/EventSources';
import { EventSourceReadConfidence } from '../Syntax/EventSourceReadConfidence';
import { mergeDocuments } from './PlayFolderMerge';
import { inMemoryDocumentSource, PlacedPlayDocument, PlayDocumentSource } from './PlayDocumentSource';
import { normalizePlayPath } from './PlayGlob';
import { resolveImports } from './PlayImports';

// One .play document of a folder: its path relative to the folder, and its text.
export interface PlayFileSource {
    readonly path: string;
    readonly source: string;
}

// One application compiled from documents: the documents that make it up, each with where its top level
// belongs, and the merged syntax with every diagnostic - resolving, parsing and merging.
export interface ApplicationCompilation extends CompilationResult<ApplicationSyntax> {
    readonly documents: readonly PlacedPlayDocument[];
}

// Assembles one application from documents - following their imports from the roots, parsing each where it is
// placed, and merging the lot. The port of the C# PlayApplicationAssembly.Compile.
export function assembleApplication(roots: Iterable<string>, source: PlayDocumentSource, languages?: ReadonlySet<string>): ApplicationCompilation {
    const { documents, diagnostics } = resolveImports(roots, source, languages);
    const merged = parsePlacedDocuments(documents, languages);
    const all = [...diagnostics, ...merged.diagnostics];
    return { ...merged, documents, diagnostics: all, success: !all.some(diagnostic => diagnostic.severity === 'error') };
}

// Parses documents whose source identities and placements are already known (for example unsaved
// editor buffers), then validates contracts against the merged declaration inventory.
export function parsePlacedDocuments(documents: readonly PlacedPlayDocument[], languages?: ReadonlySet<string>): CompilationResult<ApplicationSyntax> & {
    readonly physicalEventSources: readonly { source: EventSourceSyntax; placementResolved: boolean }[];
    readonly sourceInventoryComplete: boolean;
} {
    const candidates = CommandStreamCandidates.capturePlaced(documents.filter(document => document.isPlacementResolved !== false).map(document => ({ lines: splitLines(document.source, false, document.path), placement: document.placement })), languages);
    const physical = documents.map(document => parseForAuthoring(document.source, document.path, document.isPlacementResolved === false ? [] : document.placement, false, candidates, languages));
    const physicalEventSources = physical.flatMap((result, index) => (result.value.eventSources ?? []).map(source => ({ source, placementResolved: documents[index].isPlacementResolved !== false })));
    const sourceInventoryComplete = documents.every(document => document.isPlacementResolved !== false) && physical.every(result => !EventSourceReadConfidence.hasUnknownExtent(result.diagnostics));
    const parsed = physical.map((result, index) => documents[index].isPlacementResolved === false ? { ...result, value: { ...result.value, eventSources: [] } } : result);
    const merged = mergeDocuments(parsed);
    const context = new ParserContext(new LineReader([]), undefined, languages);
    if (merged.value.sourceOptions !== undefined) context.sourceOptions = merged.value.sourceOptions;
    validateEventSources(merged.value, context);
    validateOperations(merged.value, context);
    validateInlineEvents(merged.value, context);
    validateResponses(merged.value, context, parsed.flatMap(document => document.inputUses));
    const existing = merged.diagnostics;
    const reported = new Set(existing.map(diagnosticKey));
    const all = [...existing, ...context.diagnostics.filter(diagnostic => !reported.has(diagnosticKey(diagnostic)))];
    return { ...merged, diagnostics: all, success: !all.some(diagnostic => diagnostic.severity === 'error'), physicalEventSources, sourceInventoryComplete };
}

function diagnosticKey(diagnostic: Diagnostic): string {
    return JSON.stringify([diagnostic.code, diagnostic.location.path, diagnostic.location.line, diagnostic.location.column]);
}

// Compiles documents held in memory, keyed by portable path, as one application. Without roots every
// document is a root, in ordinal order of its path - a folder compiled as one application, where a file an
// import places in a module or feature is placed there. With roots, only those documents and what they import
// make up the application - a single file compiled with its imports followed. The default sort compares
// UTF-16 code units, which is the C# ordinal order.
export function compileApplication(documents: ReadonlyMap<string, string>, roots?: readonly string[], languages?: ReadonlySet<string>): ApplicationCompilation {
    const normalized = new Map([...documents].map(([path, source]) => [normalizePlayPath(path), source]));
    return assembleApplication(roots ?? [...normalized.keys()].sort(), inMemoryDocumentSource(normalized), languages);
}

// Compiles the documents of a folder as one application - the counterpart of the C# CompileFolder. Every
// document is a root, read in ordinal order of its relative path as the C# compiler reads them, so a merge
// keeps the same first declaration in either language; one an import places in a module or feature is
// placed there.
export function parseFolder(files: readonly PlayFileSource[], languages?: ReadonlySet<string>): ApplicationCompilation {
    return compileApplication(new Map(files.map(file => [file.path, file.source])), undefined, languages);
}


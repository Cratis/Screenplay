// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from './Diagnostics/Diagnostic';
import { validateDependencyDeclarations } from './Dependencies/DeclaredDependencyTargets';
import { documentPlacement, PlayPlacement } from './Files/PlayPlacement';
import { DiscoveredImport, discoverImports as discoverImportsIn } from './Parsing/ImportDiscovery';
import { InputUse } from './Parsing/InputUses';
import { validateResponses } from './Parsing/ResponseValidator';
import { sourceContext } from './Parsing/SourceOptionsParser';
import { validateInlineEvents } from './Parsing/InlineEventValidator';
import { validateOperations } from './Parsing/OperationValidator';
import { CommandStreamCandidates } from './Parsing/CommandStreamCandidates';
import { validateEventSources } from './Parsing/EventSourceValidator';
import { validateProjectionTargets } from './Parsing/ProjectionTargetValidator';
import { validateIdentifierCompliance } from './Parsing/IdentifierComplianceValidator';
import { parseApplication } from './Parsing/ScreenplayParser';
import { splitLines } from './Parsing/SourceLineSplitter';
import { PropertySyntax } from './Syntax/Declarations';
import { ApplicationSyntax } from './Syntax/Structure';
import { ApplicationSyntaxVisitor } from './Syntax/Visitors';
import { ProjectionSyntax } from './Syntax/Projections';
import { CaptureSyntax } from './Syntax/Captures';
import { SpecificationExampleSyntax, SpecificationSyntax } from './Syntax/Specifications';
import { SyntaxNode } from './Syntax/SyntaxNode';
import { ParserContext } from './Parsing/ParserContext';
import { parseProjection } from './Parsing/ProjectionParser';
import { parseCapture } from './Parsing/CaptureParser';
import { parseSpecification } from './Parsing/SpecificationParser';
import { parseExample } from './Parsing/SpecificationExampleParser';
import { firstWord } from './Parsing/LineText';
import { SourceLine, locationOf } from './Parsing/SourceLine';
import { DiagnosticCodes } from './Diagnostics/DiagnosticCodes';
import { timelineOrderDiagnostics } from './Files/TimelineOrder';

// What compiling produced: the syntax tree, and the diagnostics found on the way. A tree is always
// produced, so a document with errors still shows everything that could be read.
export interface CompilationResult<T> {
    readonly value: T;
    readonly diagnostics: readonly Diagnostic[];
    readonly success: boolean;
}

// Parses one .play document - the TypeScript counterpart of the C# ScreenplayCompiler.Parse. The path, when
// given, is carried on every source location so a folder of documents can be merged and still point back
// at the file each node came from. A placement says the document's top level belongs to the module or
// feature an import placed it in: that module and feature are in the tree, marked as placements, holding what
// the document declares at its top level.
export function parse(source: string, path?: string, placement: PlayPlacement = documentPlacement): CompilationResult<ApplicationSyntax> {
    const { value, diagnostics, success } = parseForAuthoring(source, path, placement);
    return { value, diagnostics, success };
}

// Additive authoring view: do not widen TypeRefSyntax or the cross-compiler SyntaxJson projection.
export function parseForAuthoring(source: string, path?: string, placement: PlayPlacement = documentPlacement, validateResponseContracts = true, streamCandidates?: CommandStreamCandidates, languages?: ReadonlySet<string>): CompilationResult<ApplicationSyntax> & { readonly triggerData: readonly PropertySyntax[]; readonly inputUses: readonly InputUse[] } {
    const lines = splitLines(source, false, path);
    const context = sourceContext(lines, path, languages);
    context.scope = placement;
    context.streamCandidates = streamCandidates ?? CommandStreamCandidates.capture([lines], placement, languages);
    let value = parseApplication(context, lines, placement);
    // Folder assembly validates declaration-dependent contracts once against the merged inventory.
    if (validateResponseContracts) {
        validateInlineEvents(value, context);
        validateOperations(value, context);
        validateResponses(value, context);
        validateEventSources(value, context);
        validateProjectionTargets(value, context);
        validateIdentifierCompliance(value, context);
        value = validateDependencyDeclarations(value, context);
        for (const diagnostic of timelineOrderDiagnostics(value)) context.information(diagnostic.code, diagnostic.message, diagnostic.location);
    }
    return {
        value,
        diagnostics: context.diagnostics,
        triggerData: context.triggerData,
        inputUses: context.inputUses,
        success: !context.diagnostics.some(diagnostic => diagnostic.severity === 'error'),
    };
}

// Parses a document and hands its syntax tree to a visitor - the counterpart of the C#
// ScreenplayCompiler.Compile<T>(source, visitor). The visitor runs even when there are errors, over
// everything that could be read.
export function compile<T>(source: string, visitor: ApplicationSyntaxVisitor<T>, path?: string): CompilationResult<T> {
    const parsed = parse(source, path);
    return { value: visitor.visit(parsed.value), diagnostics: parsed.diagnostics, success: parsed.success };
}

// Finds the files a document imports and where in it each import is written - each import with the module
// and feature names around it, outermost first. Diagnostics are not collected; parsing the document reports them.
export function discoverImports(source: string, path?: string, languages?: ReadonlySet<string>): DiscoveredImport[] {
    const lines = splitLines(source, false, path);
    return discoverImportsIn(sourceContext(lines, path, languages));
}

// Additive entry points preserve the caller's registry; none can select numeric mode externally.
export function parseWithLanguages(source: string, languages: ReadonlySet<string>, path?: string, placement: PlayPlacement = documentPlacement): CompilationResult<ApplicationSyntax> {
    const { value, diagnostics, success } = parseForAuthoring(source, path, placement, true, undefined, languages);
    return { value, diagnostics, success };
}

export function parseProjectionSource(source: string, path?: string, languages?: ReadonlySet<string>): CompilationResult<readonly ProjectionSyntax[]> {
    return parseSourceFamily(source, 'projection', parseProjection, path, languages);
}

export function parseCaptureSource(source: string, path?: string, languages?: ReadonlySet<string>): CompilationResult<readonly CaptureSyntax[]> {
    return parseSourceFamily(source, 'capture', parseCapture, path, languages);
}

export function parseSpecificationSource(source: string, path?: string, languages?: ReadonlySet<string>): CompilationResult<readonly SpecificationSyntax[]> {
    return parseSourceFamily(source, 'specification', parseSpecification, path, languages);
}

function parseSourceFamily<T extends SyntaxNode>(source: string, keyword: string, read: (context: ParserContext, line: SourceLine) => T, path?: string, languages?: ReadonlySet<string>): CompilationResult<readonly T[]> {
    const context = sourceContext(splitLines(source, true, path), path, languages, true);
    const value: T[] = [];
    const examples: SpecificationExampleSyntax[] = [];
    for (let line = context.reader.peekSignificant(); line !== undefined; line = context.reader.peekSignificant()) {
        context.reader.takeSignificant();
        if (keyword === 'specification' && firstWord(line.content) === 'example') examples.push(parseExample(context, line));
        else if (line.content.startsWith(keyword)) value.push(read(context, line));
        else {
            context.error(DiagnosticCodes.UnknownTopLevelConstruct, `Expected '${keyword}', got '${line.content}'`, locationOf(line));
            context.skipOpaqueBlock(line.indent);
        }
    }
    if (context.sourceOptions.numericMode === 'exact' && value.length === 0 && context.diagnostics.length === 0) {
        const code = keyword === 'projection' ? DiagnosticCodes.ProjectionDocumentWithoutProjection : keyword === 'capture' ? DiagnosticCodes.CaptureDocumentWithoutCapture : DiagnosticCodes.SpecificationDocumentWithoutSpecification;
        context.error(code, `Document must contain at least one ${keyword}`, context.start);
    }
    const roots = keyword === 'specification' && examples.length > 0 ? value.map(node => ({ ...node, examples })) : value;
    return { value: roots, diagnostics: context.diagnostics, success: !context.diagnostics.some(diagnostic => diagnostic.severity === 'error') };
}

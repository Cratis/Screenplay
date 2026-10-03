// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from './Diagnostics/Diagnostic';
import { documentPlacement, PlayPlacement } from './Files/PlayPlacement';
import { DiscoveredImport, discoverImports as discoverImportsIn } from './Parsing/ImportDiscovery';
import { InputUse } from './Parsing/InputUses';
import { validateResponses } from './Parsing/ResponseValidator';
import { LineReader } from './Parsing/LineReader';
import { validateInlineEvents } from './Parsing/InlineEventValidator';
import { validateOperations } from './Parsing/OperationValidator';
import { ParserContext } from './Parsing/ParserContext';
import { parseApplication } from './Parsing/ScreenplayParser';
import { splitLines } from './Parsing/SourceLineSplitter';
import { PropertySyntax } from './Syntax/Declarations';
import { ApplicationSyntax } from './Syntax/Structure';
import { ApplicationSyntaxVisitor } from './Syntax/Visitors';

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
export function parseForAuthoring(source: string, path?: string, placement: PlayPlacement = documentPlacement, validateResponseContracts = true): CompilationResult<ApplicationSyntax> & { readonly triggerData: readonly PropertySyntax[]; readonly inputUses: readonly InputUse[] } {
    const lines = splitLines(source, false, path);
    const context = new ParserContext(new LineReader(lines), path);
    context.scope = placement;
    const value = parseApplication(context, lines, placement);
    validateInlineEvents(value, context);
    // Folder assembly validates response references once against the merged declaration inventory.
    if (validateResponseContracts) {
        validateOperations(value, context);
        validateResponses(value, context);
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
export function discoverImports(source: string, path?: string): DiscoveredImport[] {
    const lines = splitLines(source, false, path);
    return discoverImportsIn(new ParserContext(new LineReader(lines), path));
}

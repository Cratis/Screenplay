// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from './Diagnostics/Diagnostic';
import { LineReader } from './Parsing/LineReader';
import { ParserContext } from './Parsing/ParserContext';
import { parseApplication } from './Parsing/ScreenplayParser';
import { splitLines } from './Parsing/SourceLineSplitter';
import { ApplicationSyntax } from './Syntax/Structure';

// What compiling produced: the syntax tree, and the diagnostics found on the way. A tree is always
// produced, so a document with errors still shows everything that could be read.
export interface CompilationResult<T> {
    readonly value: T;
    readonly diagnostics: readonly Diagnostic[];
    readonly success: boolean;
}

// Parses one .play document - the TypeScript counterpart of the C# ScreenplayCompiler.Parse. The path, when
// given, is carried on every source location so a folder of documents can be merged and still point back
// at the file each node came from.
export function parse(source: string, path?: string): CompilationResult<ApplicationSyntax> {
    const lines = splitLines(source, false, path);
    const context = new ParserContext(new LineReader(lines), path);
    const value = parseApplication(context, lines);
    return {
        value,
        diagnostics: context.diagnostics,
        success: !context.diagnostics.some(diagnostic => diagnostic.severity === 'error'),
    };
}

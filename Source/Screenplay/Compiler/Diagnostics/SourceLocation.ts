// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Where a syntax node or a diagnostic came from: a one-based line and column, and the file when the
// document was compiled as part of a folder. Mirrors the C# SourceLocation record.
export interface SourceLocation {
    readonly line: number;
    readonly column: number;
    readonly path?: string;
}

export const sourceLocation = (line: number, column: number, path?: string): SourceLocation =>
    path === undefined ? { line, column } : { line, column, path };

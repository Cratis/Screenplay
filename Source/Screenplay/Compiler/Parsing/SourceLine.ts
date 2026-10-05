// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation, sourceLocation } from '../Diagnostics/SourceLocation';

// One physical line of a document: its raw text, how far it is indented and what is left once comments and
// trailing whitespace are gone. The offside rule reads nesting from the indent alone.
export interface SourceLine {
    readonly number: number;
    readonly raw: string;
    readonly indent: number;
    readonly content: string;
    readonly contentOffset?: number;
    readonly path?: string;
    readonly startOffset: number;
}

export const isBlank = (line: SourceLine): boolean => line.content.length === 0;

// The location of the line's first significant character.
export const locationOf = (line: SourceLine): SourceLocation => sourceLocation(line.number, line.indent + (line.contentOffset ?? 0) + 1, line.path);

// The location of the line's first column, whatever its indent.
export const startOf = (line: SourceLine): SourceLocation => sourceLocation(line.number, 1, line.path);

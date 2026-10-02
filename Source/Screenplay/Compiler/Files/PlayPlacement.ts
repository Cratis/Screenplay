// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Where the top level of a .play file belongs: the module name followed by the names of the nested features,
// outermost first, and empty for the application itself - the port of the C# PlayPlacement. A file is placed
// by the import that brings it in: written inside 'module Ordering' and 'feature Orders', it places the
// imported file at Ordering.Orders, so what the file declares at its top level is the body of that feature.
export type PlayPlacement = readonly string[];

// The placement of a whole document - its top level is the application's.
export const documentPlacement: PlayPlacement = [];

// Whether a file is a whole document rather than placed in a module or feature.
export const isDocumentPlacement = (placement: PlayPlacement): boolean => placement.length === 0;

// The placement as it reads in a diagnostic, such as module 'Ordering' or feature 'Ordering.Orders'.
export function describePlacement(placement: PlayPlacement): string {
    if (placement.length === 0) return 'the application';
    if (placement.length === 1) return `module '${placement[0]}'`;
    return `feature '${placement.join('.')}'`;
}

// Whether one placement lies inside another, or is the same - the other's scope is a prefix of this one.
export const isWithinOrSame = (placement: PlayPlacement, other: PlayPlacement): boolean =>
    other.length <= placement.length && other.every((name, index) => name === placement[index]);

export const samePlacement = (left: PlayPlacement, right: PlayPlacement): boolean =>
    left.length === right.length && isWithinOrSame(left, right);

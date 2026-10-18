// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Occurrence-preserving AST for admitted patterns, separate from execution normalization. */
export interface MatchAmbiguityNode {
    kind: string;
    offset: number;
    length: number;
    children: MatchAmbiguityNode[];
    minimum: number;
    maximum: number | null;
    characters: [number, number][];
}

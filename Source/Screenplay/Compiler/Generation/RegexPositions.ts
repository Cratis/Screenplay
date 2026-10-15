// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface RegexPositions { readonly nullable: boolean; readonly first: Set<number>; readonly last: Set<number>; }
export function emptyRegexPositions(nullable = true): RegexPositions { return { nullable, first: new Set(), last: new Set() }; }

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A declaration's position in its physical text, not the placement scaffolding of the merged tree.
export interface AuthoredDeclaration {
    readonly scope: readonly string[];
    readonly line: number;
    readonly column: number;
    readonly implicit: boolean;
}

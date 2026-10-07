// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface DependencyHighlights {
    readonly selected: readonly string[];
    readonly dependencies: readonly string[];
    readonly dependants: readonly string[];
    readonly edges: readonly { readonly source: string; readonly target: string; readonly crossing: boolean }[];
}

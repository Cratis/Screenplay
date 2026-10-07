// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** A directed route and bounded, centered count label. */
export interface DependencyMapRoute {
    readonly id: string;
    readonly path: string;
    readonly labelX: number;
    readonly labelY: number;
    readonly labelWidth: number;
}

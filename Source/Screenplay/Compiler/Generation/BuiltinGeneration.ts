// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Pure built-in shape descriptor, matching the existing native generation members. */
export interface BuiltinGeneration {
    readonly kind: string;
    readonly prefix?: string;
    readonly case?: string;
    readonly size?: number;
    readonly alphabet?: string;
    readonly count?: number;
    readonly encoding?: string;
}

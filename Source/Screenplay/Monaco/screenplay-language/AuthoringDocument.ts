// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Structural public editor input, independent of the private compiler package.
export interface AuthoringDocument {
    readonly path: string;
    readonly source: string;
    readonly placement?: readonly string[];
}

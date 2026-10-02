// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// What the board needs to know of where the host shows it: how it is displayed and how much room it has.
export interface HostContainer {
    readonly displayMode?: string;
    readonly containerDimensions?: { readonly height?: number; readonly maxHeight?: number };
}

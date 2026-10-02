// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A .play document as the server sends it. A change without source takes the document away.
export interface VisualizedDocument {
    readonly path: string;
    readonly source?: string;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export class InvalidGenerationPattern extends Error {
    constructor(reason: string, readonly offset: number) { super(`${reason} at pattern offset ${offset}.`); }
}

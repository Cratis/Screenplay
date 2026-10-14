// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export class OutsideRegexFragment extends Error {
    constructor(reason: string) { super(`outside fragment R: ${reason}`); }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface SharedReference {
    readonly kind: 'type' | 'policy';
    readonly name: string;
}

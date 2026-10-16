// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** An equivalent ordinal text comparison, using semantic rather than syntax kind names. */
export interface PortableMatchReplacement {
    kind: 'Equal' | 'NotEqual' | 'Contains' | 'NotContains' | 'StartsWith' | 'EndsWith';
    value: string;
}

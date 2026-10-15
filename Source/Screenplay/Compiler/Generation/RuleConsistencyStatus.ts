// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Proof status is tooling metadata, never executable-model bytes. */
export enum RuleConsistencyStatus {
    Proven = 'Proven',
    Refuted = 'Refuted',
    Unproven = 'Unproven'
}

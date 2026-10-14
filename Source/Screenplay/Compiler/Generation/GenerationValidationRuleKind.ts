// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Rule names for the pure proof API, mirroring native semantic kinds without introducing an executable binder. */
export enum GenerationValidationRuleKind {
    Unknown = 'Unknown',
    NotEmpty = 'NotEmpty',
    Maximum = 'Maximum',
    Minimum = 'Minimum',
    Equal = 'Equal',
    NotEqual = 'NotEqual',
    GreaterThan = 'GreaterThan',
    GreaterThanOrEqual = 'GreaterThanOrEqual',
    LessThan = 'LessThan',
    LessThanOrEqual = 'LessThanOrEqual',
    Length = 'Length',
    AllGreaterThan = 'AllGreaterThan',
    AllGreaterThanOrEqual = 'AllGreaterThanOrEqual',
    Matches = 'Matches',
    RulePredicate = 'RulePredicate',
    CodeValidation = 'CodeValidation',
    Checksum = 'Checksum'
}

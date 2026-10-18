// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface MatchAmbiguityPositions {
    nullable: number;
    first: Map<number, number>;
    last: Map<number, number>;
}

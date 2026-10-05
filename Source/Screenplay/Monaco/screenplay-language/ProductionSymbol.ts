// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface ProductionSymbol {
    name: string;
    inline: boolean;
    conditional?: boolean;
    line: number;
    target?: string;
    mappings: { name: string; source: string; line: number }[];
}

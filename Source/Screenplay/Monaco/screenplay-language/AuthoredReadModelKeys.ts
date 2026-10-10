// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TypeReferenceSymbol } from './TypeReferenceSymbol';

// Public editor evidence is structural; packed declarations never depend on the private compiler.
export interface AuthoredReadModelKeys {
    readonly scope: readonly string[];
    readonly model: {
        readonly name: string;
        readonly properties: readonly {
            readonly name: string;
            readonly isKey?: boolean;
            readonly type: TypeReferenceSymbol;
        }[];
    };
}

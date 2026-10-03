// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionKind } from './AuthoringProductionKind';
import { EventSyntax } from './Declarations';
import { OperationSyntax } from './OperationSyntax';

export interface AuthoringProductionDeclaration {
    readonly kind: AuthoringProductionKind.Event | AuthoringProductionKind.Operation;
    readonly name: string;
    readonly scope: readonly string[];
    readonly node: EventSyntax | OperationSyntax;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OperationSyntax } from './OperationSyntax';
import { SliceSyntax } from './Structure';

export type { SystemSyntax } from './SystemSyntax';
export type { OperationSyntax } from './OperationSyntax';
export type { OperationPhaseSyntax } from './OperationPhaseSyntax';

export function operationDeclarations(slice: SliceSyntax): readonly OperationSyntax[] {
    return [...slice.operations ?? [], ...slice.commands.flatMap(command => command.produces).flatMap(production => production.inlineOperation == null ? [] : [production.inlineOperation])];
}

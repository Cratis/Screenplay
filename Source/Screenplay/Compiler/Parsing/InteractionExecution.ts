// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InteractionArgumentSyntax } from '../Syntax/Screens';

// Command inputs in C# action/continuation traversal order, not serialized syntax.
export interface InteractionExecution {
    readonly command: string;
    readonly arguments: readonly InteractionArgumentSyntax[];
}

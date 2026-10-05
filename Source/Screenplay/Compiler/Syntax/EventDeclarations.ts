// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventSyntax } from './Declarations';
import { SliceSyntax } from './Structure';

// An inline event belongs to its slice, not its command. Keep the authored tree intact.
export function eventDeclarations(slice: SliceSyntax): readonly EventSyntax[] {
    return [...slice.events, ...slice.commands.flatMap(command => command.produces).flatMap(produced => produced.inlineEvent === null ? [] : [produced.inlineEvent])];
}

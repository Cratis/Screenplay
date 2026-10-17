// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ProjectionBlockSyntax } from '../Syntax/Projections';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ParserContext } from './ParserContext';

// Chronicle tracks authored names ordinally, with separate event and join sets at each level.
export function validateProjectionEventScopes(context: ParserContext, projection: string, blocks: readonly ProjectionBlockSyntax[], scope = 'the root level'): void {
    const levelEvents = new Set<string>();
    const joinEvents = new Set<string>();
    const check = (name: string, location: SourceLocation, seen: Set<string>): void => {
        if (seen.has(name)) {
            context.error(DiagnosticCodes.DuplicateEventInProjectionScope, `Event '${name}' is used more than once in projection '${projection}' at ${scope} (Chronicle allows one use of an event type per projection level)`, location);
        }
        seen.add(name);
    };
    for (const block of blocks) {
        switch (block.kind) {
            case 'FromSyntax':
                block.events.forEach(reference => check(reference.event, reference.location, levelEvents));
                break;
            case 'JoinSyntax':
                block.events.forEach(reference => check(reference.event, reference.location, joinEvents));
                break;
            case 'RemoveWithSyntax':
            case 'RemoveViaJoinSyntax':
            case 'ClearWithSyntax':
                check(block.event, block.location, levelEvents);
                break;
            case 'ChildrenSyntax':
                validateProjectionEventScopes(context, projection, block.blocks, `children '${block.property}'`);
                break;
            case 'NestedSyntax':
                validateProjectionEventScopes(context, projection, block.blocks, `nested '${block.property}'`);
                break;
            case 'ProjectionVariantSyntax':
                validateProjectionEventScopes(context, projection, block.blocks, `variant '${block.name}'`);
                break;
        }
    }
}

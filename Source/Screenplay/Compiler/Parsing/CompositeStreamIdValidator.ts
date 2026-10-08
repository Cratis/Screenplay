// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from '../Diagnostics/SourceLocation';
import { TypeRefSyntax } from '../Syntax/Declarations';
import { EventStreamSyntax } from '../Syntax/EventSources';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { ParserContext } from './ParserContext';

export function validateStreamIdParts(stream: EventStreamSyntax, mappings: readonly PropertyMappingSyntax[], scalar: boolean, location: SourceLocation, code: string, context: ParserContext, validate: (mapping: PropertyMappingSyntax, target: TypeRefSyntax) => void): void {
    if (scalar) {
        context.error(code, 'A composite stream requires a streamId part block, not a scalar mapping.', location);
        return;
    }
    const parts = stream.streamIdParts;
    for (const part of parts.filter(part => !mappings.some(mapping => mapping.property === part.name)))
        context.error(code, `Stream id part '${part.name}' requires a mapping.`, location);
    const names = new Set<string>();
    for (const mapping of mappings) {
        const part = parts.find(part => part.name === mapping.property);
        if (part === undefined) context.error(code, `Unknown stream id part '${mapping.property}'.`, mapping.location);
        else if (names.has(mapping.property)) context.error(code, `Stream id part '${mapping.property}' is mapped more than once.`, mapping.location);
        else {
            names.add(mapping.property);
            validate(mapping, part.type);
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SlicePlacementSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';

const placementPattern = /^shows\s+(event|readmodel)\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$/;

// 'shows event <Name>' or 'shows readmodel <Name>' - a linked copy of an element declared elsewhere (PLAY0953).
export function parseSlicePlacement(context: ParserContext, line: SourceLine): SlicePlacementSyntax | undefined {
    const match = placementPattern.exec(line.content);
    if (match === null) {
        context.skipBlock(line.indent);
        context.error(DiagnosticCodes.InvalidSlicePlacement, `Invalid placement '${line.content}' - expected 'shows event <Name>' or 'shows readmodel <Name>'`, locationOf(line));
        return undefined;
    }
    return { kind: 'SlicePlacementSyntax', syntaxKind: match[1] === 'readmodel' ? 'ReadModel' : 'Event', name: match[2], location: locationOf(line) };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CommandSyntax } from '../Syntax/Commands';
import { ConstraintSyntax } from '../Syntax/Constraints';
import { EventSyntax, ReadModelSyntax } from '../Syntax/Declarations';
import { ProjectionSyntax } from '../Syntax/Projections';
import { QuerySyntax } from '../Syntax/Queries';
import { ReactionSyntax } from '../Syntax/Reactions';
import { SliceSyntax, SliceType, sliceTypes } from '../Syntax/Structure';
import { pattern } from '../Text/patterns';
import { parseCommand } from './CommandParser';
import { parseConstraint } from './ConstraintParser';
import { parseEvent, parseReadModel } from './DeclarationParsers';
import { parseDescription } from './DescriptionParser';
import { isFileDirective } from './FileReferences';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseProjection } from './ProjectionParser';
import { parseQuery } from './QueryParser';
import { parseReaction } from './ReactionParser';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^slice\\s+([A-Za-z]\\w*)\\s+([A-Za-z_]\\w*)$');

// Slice members the C# compiler knows that this compiler does not model yet. They are skipped whole rather
// than reported - the C# compiler and the editor diagnostics remain the authority on whether they are valid.
const opaqueMembers = new Set(['capture', 'screen', 'specification', 'reducer']);

export function parseSlice(context: ParserContext, line: SourceLine): SliceSyntax {
    const match = header.exec(line.content);
    let type: SliceType = 'StateChange';
    let name = '';
    if (match === null) {
        context.error(DiagnosticCodes.InvalidSliceDeclaration, `Invalid slice declaration '${line.content}' - expected 'slice <Type> <Name>'`, locationOf(line));
    } else {
        name = match[2];
        if ((sliceTypes as readonly string[]).includes(match[1])) {
            type = match[1] as SliceType;
        } else {
            context.error(DiagnosticCodes.UnknownSliceType, `Unknown slice type '${match[1]}' - expected StateChange, StateView, Automation or Translate`, locationOf(line));
        }
    }
    let description: string | null = null;
    const events: EventSyntax[] = [];
    const commands: CommandSyntax[] = [];
    const queries: QuerySyntax[] = [];
    const projections: ProjectionSyntax[] = [];
    const reactions: ReactionSyntax[] = [];
    const constraints: ConstraintSyntax[] = [];
    const readModels: ReadModelSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (isFileDirective(child)) {
            continue;
        }
        const keyword = firstWord(child.content);
        if (keyword === 'description') {
            description = parseDescription(context, child, description, `Slice '${name}'`);
        } else if (keyword === 'event') {
            events.push(parseEvent(context, child));
        } else if (keyword === 'command') {
            commands.push(parseCommand(context, child));
        } else if (keyword === 'query') {
            queries.push(parseQuery(context, child));
        } else if (keyword === 'projection') {
            projections.push(parseProjection(context, child));
        } else if (keyword === 'reaction') {
            reactions.push(parseReaction(context, child));
        } else if (keyword === 'constraint') {
            constraints.push(parseConstraint(context, child));
        } else if (keyword === 'readmodel') {
            readModels.push(parseReadModel(context, child));
        } else if (opaqueMembers.has(keyword)) {
            context.skipOpaqueBlock(child.indent);
        } else {
            context.warning(DiagnosticCodes.UnknownSliceDirective, `Unknown construct '${keyword}' in slice '${name}'`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    return {
        kind: 'SliceSyntax', type, name, description, events, commands, queries, projections, reactions, constraints, readModels,
        location: locationOf(line),
    };
}

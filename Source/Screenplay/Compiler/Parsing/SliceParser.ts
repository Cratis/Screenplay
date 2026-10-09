// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CaptureSyntax } from '../Syntax/Captures';
import { CommandSyntax } from '../Syntax/Commands';
import { ConstraintSyntax } from '../Syntax/Constraints';
import { EventSyntax, ReadModelSyntax } from '../Syntax/Declarations';
import { ProjectionSyntax } from '../Syntax/Projections';
import { QuerySyntax } from '../Syntax/Queries';
import { ReactionSyntax } from '../Syntax/Reactions';
import { ScreenSyntax } from '../Syntax/Screens';
import { SpecificationExampleSyntax, SpecificationSyntax } from '../Syntax/Specifications';
import { SliceSyntax, SliceType, sliceTypes } from '../Syntax/Structure';
import { pattern } from '../Text/patterns';
import { TranslationDirection } from '../Syntax/TranslationDirection';
import { parseCapture } from './CaptureParser';
import { parseCommand } from './CommandParser';
import { dependencySources, ReducerSyntax } from '../Syntax/DependencySources';
import { captureReducer } from './DependencySourceParser';
import { parseConstraint } from './ConstraintParser';
import { parseEvent, parseReadModel } from './DeclarationParsers';
import { parseDescription } from './DescriptionParser';
import { PurposeReferenceSyntax } from '../Syntax/Purposes';
import { parsePurposeReference } from './PurposeParser';
import { parseDocumentation } from './DocumentationParser';
import { isFileDirective } from './FileReferences';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { parseOperation } from './OperationParser';
import { OperationSyntax } from '../Syntax/Operations';
import { parseProjection } from './ProjectionParser';
import { parseQuery } from './QueryParser';
import { parseReaction } from './ReactionParser';
import { parseScreen } from './ScreenParser';
import { parseExample } from './SpecificationExampleParser';
import { parseSpecification } from './SpecificationParser';
import { locationOf, SourceLine } from './SourceLine';

const header = pattern('^slice\\s+([A-Za-z]\\w*)\\s+([A-Za-z_]\\w*)$');

// Slice members the C# compiler knows that this compiler does not model yet. They are skipped whole rather
// than reported - the C# compiler and the editor diagnostics remain the authority on whether they are valid.
const opaqueMembers = new Set(['reducer']);

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
    const previous = context.scope;
    context.scope = [...previous, name];
    let direction: TranslationDirection | null = null;
    let hasDirection = false;
    let description: string | null = null;
    let documentation: string | null = null;
    const events: EventSyntax[] = [];
    const operations: OperationSyntax[] = [];
    const commands: CommandSyntax[] = [];
    const queries: QuerySyntax[] = [];
    const projections: ProjectionSyntax[] = [];
    const reactions: ReactionSyntax[] = [];
    const captures: CaptureSyntax[] = [];
    const constraints: ConstraintSyntax[] = [];
    const specifications: SpecificationSyntax[] = [];
    const examples: SpecificationExampleSyntax[] = [];
    const purposes: PurposeReferenceSyntax[] = [];
    const readModels: ReadModelSyntax[] = [];
    const screens: ScreenSyntax[] = [];
    const reducers: ReducerSyntax[] = [];
    for (let child = context.peekChild(line.indent); child !== undefined; child = context.peekChild(line.indent)) {
        context.reader.takeSignificant();
        if (isFileDirective(child)) {
            continue;
        }
        const metadataWord = child.content.split(/[ \t]/, 1)[0];
        const keyword = metadataWord === 'public' || metadataWord === 'direction' ? metadataWord : firstWord(child.content);
        if (keyword === 'description') {
            description = parseDescription(context, child, description, `Slice '${name}'`);
        } else if (keyword === 'documentation') {
            documentation = parseDocumentation(context, child, documentation, `Slice '${name}'`);
        } else if (keyword === 'purpose') {
            parsePurposeReference(context, child, purposes);
        } else if (keyword === 'operation') {
            operations.push(parseOperation(context, child).operation);
        } else if (keyword === 'direction') {
            const directionMatch = /^direction\s+(inbound|outbound)$/.exec(child.content);
            if (hasDirection || type !== 'Translate' || directionMatch === null) {
                context.error(DiagnosticCodes.InvalidSliceDeclaration, 'Declare direction inbound or direction outbound once, inside a Translate slice only', locationOf(child));
            } else {
                direction = directionMatch[1] === 'inbound' ? TranslationDirection.Inbound : TranslationDirection.Outbound;
            }
            hasDirection = true;
        } else if (keyword === 'event' || keyword === 'public') {
            events.push(parseEvent(context, child));
        } else if (keyword === 'command') {
            commands.push(parseCommand(context, child));
        } else if (keyword === 'query') {
            queries.push(parseQuery(context, child));
        } else if (keyword === 'projection') {
            projections.push(parseProjection(context, child));
        } else if (keyword === 'capture') {
            captures.push(parseCapture(context, child));
        } else if (keyword === 'reaction') {
            reactions.push(parseReaction(context, child));
        } else if (keyword === 'constraint') {
            constraints.push(parseConstraint(context, child));
        } else if (keyword === 'example') {
            examples.push(parseExample(context, child));
        } else if (keyword === 'specification') {
            specifications.push(parseSpecification(context, child));
        } else if (keyword === 'readmodel') {
            readModels.push(parseReadModel(context, child));
        } else if (keyword === 'screen') {
            screens.push(parseScreen(context, child));
        } else if (opaqueMembers.has(keyword)) {
            reducers.push(captureReducer(context, child));
            context.skipOpaqueBlock(child.indent);
        } else {
            context.warning(DiagnosticCodes.UnknownSliceDirective, `Unknown construct '${keyword}' in slice '${name}'`, locationOf(child));
            context.skipBlock(child.indent);
        }
    }
    context.scope = previous;
    const syntax: SliceSyntax = {
        kind: 'SliceSyntax', type, name, direction, description, documentation, examples, purposes, events, operations, commands, queries, projections, captures, reactions, constraints, specifications, readModels, screens,
        location: locationOf(line),
    };
    dependencySources.set(syntax, { reducers });
    return syntax;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ConditionSyntax } from '../Syntax/Conditions';
import { LifecycleSpecificationSyntax, StateEntrySyntax, StateMachineSyntax, StateSyntax, StateTransitionSyntax } from '../Syntax/Lifecycles';
import { pattern } from '../Text/patterns';
import { parseCondition } from './ConditionParser';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { rejectOperationChildren } from './OperationParser';
import { ParserContext } from './ParserContext';
import { locationOf, SourceLine } from './SourceLine';
import { parseLifecycleSpecification } from './SpecificationParser';

const headerPattern = pattern('^statemachine\\s+([A-Za-z_]\\w*)$');
const followsPattern = pattern('^follows\\s+([A-Za-z_]\\w*)$');
const statePattern = pattern('^state\\s+([A-Za-z_]\\w*)((?:\\s+(?:initial|final))*)$');
const enteringPattern = pattern('^entering\\s+(invokes|starts|send|start)\\s+([A-Za-z_]\\w*)$');
const transitionPattern = pattern('^on\\s+([A-Za-z_][\\w.]*)(?:\\s+when\\s+(.+?))?\\s*->\\s*([A-Za-z_]\\w*)$');

// The port of C# StateMachineParser.
export function parseStateMachine(context: ParserContext, header: SourceLine): StateMachineSyntax {
    const match = headerPattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `Invalid state machine declaration '${header.content}' - expected 'statemachine <Name>'.`, locationOf(header));
        context.skipBlock(header.indent);
        return { kind: 'StateMachineSyntax', name: firstWord(header.content), follows: '', description: null, states: [], specifications: [], location: locationOf(header) };
    }
    const name = match[1];
    let description: string | null = null;
    let follows: string | null = null;
    const states: StateSyntax[] = [];
    const specifications: LifecycleSpecificationSyntax[] = [];
    const directiveLocations: Record<string, SourceLocation> = {};
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        switch (firstWord(line.content)) {
            case 'description':
                description = parseDescription(context, line, description, `State machine '${name}'`);
                directiveLocations.description ??= locationOf(line);
                break;
            case 'follows': {
                const followed = followsPattern.exec(line.content);
                if (followed === null) context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `Invalid follows '${line.content}' - expected 'follows <ReadModel>'.`, locationOf(line));
                else if (follows !== null) context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `State machine '${name}' follows more than one read model.`, locationOf(line));
                else {
                    follows = followed[1];
                    directiveLocations.follows = locationOf(line);
                }
                rejectOperationChildren(context, line, DiagnosticCodes.InvalidStateMachineDeclaration, 'A follows line cannot have children.');
                break;
            }
            case 'state':
                states.push(parseState(context, line, name));
                break;
            case 'specification':
                specifications.push(parseLifecycleSpecification(context, line, DiagnosticCodes.InvalidStateMachineDeclaration));
                break;
            default:
                context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `Unexpected '${line.content}' in state machine '${name}' - expected description, follows, state or specification.`, locationOf(line));
                context.skipBlock(line.indent);
        }
    }
    if (follows === null) context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `State machine '${name}' must follow a read model: 'follows <ReadModel>'.`, locationOf(header));
    const seen = new Set<string>();
    for (const state of states) {
        if (seen.has(state.name)) context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `State machine '${name}' declares state '${state.name}' more than once.`, state.location);
        seen.add(state.name);
    }
    return { kind: 'StateMachineSyntax', name, follows: follows ?? '', description, states, specifications, location: locationOf(header), directiveLocations };
}

function parseState(context: ParserContext, header: SourceLine, machine: string): StateSyntax {
    const match = statePattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `Invalid state '${header.content}' - expected 'state <Name> [initial] [final]'.`, locationOf(header));
        context.skipBlock(header.indent);
        return { kind: 'StateSyntax', name: '', isInitial: false, isFinal: false, entering: [], transitions: [], location: locationOf(header) };
    }
    const modifiers = match[2].split(' ').filter(word => word.length > 0);
    if (modifiers.length !== new Set(modifiers).size) context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `State '${match[1]}' repeats a modifier.`, locationOf(header));
    const entering: StateEntrySyntax[] = [];
    const transitions: StateTransitionSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        switch (firstWord(line.content)) {
            case 'entering': {
                const entry = enteringPattern.exec(line.content);
                if (entry === null) context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `Invalid entering '${line.content}' - expected 'entering invokes <Command>' or 'entering starts <Workflow>'.`, locationOf(line));
                else {
                    const verb = entry[1];
                    if (verb === 'send' || verb === 'start') context.warning(DiagnosticCodes.LegacyInlineCodeFence, `'entering ${verb}' is deprecated - use 'entering ${verb === 'send' ? 'invokes' : 'starts'}'.`, locationOf(line));
                    entering.push({ kind: 'StateEntrySyntax', entryKind: verb === 'send' || verb === 'invokes' ? 'Send' : 'Start', target: entry[2], location: locationOf(line) });
                }
                rejectOperationChildren(context, line, DiagnosticCodes.InvalidStateMachineDeclaration, 'An entering line cannot have children.');
                break;
            }
            case 'on': {
                const transition = transitionPattern.exec(line.content);
                if (transition === null) context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `Invalid transition '${line.content}' - expected 'on <Event> [when <condition>] -> <State>'.`, locationOf(line));
                else {
                    let when: ConditionSyntax | null = null;
                    let failed = false;
                    if (transition[2] !== undefined) {
                        when = parseCondition(context, transition[2], locationOf(line));
                        failed = when === null;
                    }
                    if (!failed) transitions.push({ kind: 'StateTransitionSyntax', event: transition[1], when, target: transition[3], location: locationOf(line) });
                }
                rejectOperationChildren(context, line, DiagnosticCodes.InvalidStateMachineDeclaration, 'A transition cannot have children.');
                break;
            }
            default:
                context.error(DiagnosticCodes.InvalidStateMachineDeclaration, `Unexpected '${line.content}' in state '${match[1]}' of '${machine}' - expected entering or on.`, locationOf(line));
                context.skipBlock(line.indent);
        }
    }
    return { kind: 'StateSyntax', name: match[1], isInitial: modifiers.includes('initial'), isFinal: modifiers.includes('final'), entering, transitions, location: locationOf(header) };
}

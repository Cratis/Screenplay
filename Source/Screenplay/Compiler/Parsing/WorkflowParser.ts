// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { IntervalUnit } from '../Syntax/Reactions';
import { LifecycleSpecificationSyntax, WorkflowDurationSyntax, WorkflowEdgeSyntax, WorkflowNodeKind, WorkflowNodeSyntax, WorkflowSyntax } from '../Syntax/Lifecycles';
import { pattern } from '../Text/patterns';
import { parseCondition } from './ConditionParser';
import { parseDescription } from './DescriptionParser';
import { firstWord } from './LineText';
import { rejectOperationChildren } from './OperationParser';
import { ParserContext } from './ParserContext';
import { parseTriggerSource } from './ReactionParser';
import { locationOf, SourceLine } from './SourceLine';
import { parseLifecycleSpecification } from './SpecificationParser';

const unit = '(seconds?|minutes?|hours?|days?)';
const headerPattern = pattern('^workflow\\s+([A-Za-z_]\\w*)$');
const forPattern = pattern('^for\\s+([A-Za-z_]\\w*)$');
const shorthandEdge = pattern('^->\\s*([A-Za-z_]\\w*)$');
const edgePattern = pattern('^(?:"([^"]+)"|([A-Za-z_]\\w*))(?:\\s*->\\s*([A-Za-z_]\\w*))?$');
const triggerPattern = pattern('^trigger(?:\\s+(?!(?:on|when|every|at|manual)\\b)([A-Za-z_]\\w*))?\\s+(.+)$');
const onEventPattern = pattern('^on\\s+([A-Za-z_][\\w.]*)$');
const entersPattern = pattern('^when\\s+([A-Za-z_]\\w*)\\s+enters\\s+([A-Za-z_]\\w*)$');
const actionPattern = pattern('^action\\s+([A-Za-z_]\\w*)\\s+sends\\s+([A-Za-z_]\\w*)((?:\\s+(?:attempts\\s+\\d+|timeout\\s+\\d+\\s+\\w+|retry\\s+after\\s+\\d+\\s+\\w+))*)$');
const actionOption = /attempts\s+(\d+)|timeout\s+(\d+)\s+(\w+)|retry\s+after\s+(\d+)\s+(\w+)/g;
const decidePattern = pattern('^decide\\s+([A-Za-z_]\\w*)\\s+when\\s+(.+)$');
const waitPattern = pattern(`^wait\\s+([A-Za-z_]\\w*)\\s+for\\s+(?:(\\d+)\\s+${unit}|([A-Za-z_][\\w.]*)(?:\\s+expires\\s+after\\s+(\\d+)\\s+${unit})?)$`);
const approvalPattern = pattern(`^approval\\s+([A-Za-z_]\\w*)\\s+by\\s+([A-Za-z_]\\w*)(?:\\s+expires\\s+after\\s+(\\d+)\\s+${unit})?$`);
const forEachPattern = pattern('^foreach\\s+([A-Za-z_]\\w*)\\s+over\\s+([A-Za-z_][\\w.]*)(?:\\s+parallel\\s+(\\d+))?$');
const joinPattern = pattern('^join\\s+([A-Za-z_]\\w*)\\s+(all|any|at\\s+least\\s+(\\d+))$');

type NodeParts = Partial<Omit<WorkflowNodeSyntax, 'kind' | 'nodeKind' | 'name' | 'edges' | 'location'>>;

const empty = (nodeKind: WorkflowNodeKind, name: string, location: SourceLocation, parts: NodeParts): WorkflowNodeSyntax => ({
    kind: 'WorkflowNodeSyntax', nodeKind, name, edges: [], location,
    triggerKind: null, event: null, stateMachine: null, state: null, schedule: null, command: null, attempts: null, timeout: null, retryAfter: null,
    condition: null, duration: null, expiresAfter: null, by: null, items: null, parallel: null, joinMode: null, joinCount: null,
    ...parts,
});

// The port of C# WorkflowParser.
export function parseWorkflow(context: ParserContext, header: SourceLine): WorkflowSyntax {
    const match = headerPattern.exec(header.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid workflow declaration '${header.content}' - expected 'workflow <Name>'.`, locationOf(header));
        context.skipBlock(header.indent);
        return { kind: 'WorkflowSyntax', name: firstWord(header.content), subject: '', description: null, nodes: [], specifications: [], location: locationOf(header) };
    }
    const name = match[1];
    let description: string | null = null;
    let subject: string | null = null;
    const nodes: WorkflowNodeSyntax[] = [];
    const specifications: LifecycleSpecificationSyntax[] = [];
    const directiveLocations: Record<string, SourceLocation> = {};
    let triggers = 0;
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const keyword = firstWord(line.content);
        if (keyword === 'description') {
            description = parseDescription(context, line, description, `Workflow '${name}'`);
            directiveLocations.description ??= locationOf(line);
        } else if (keyword === 'specification') {
            specifications.push(parseLifecycleSpecification(context, line, DiagnosticCodes.InvalidWorkflowDeclaration));
        } else if (keyword === 'for') {
            const found = forPattern.exec(line.content);
            if (found === null) context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid subject '${line.content}' - expected 'for <ReadModel>'.`, locationOf(line));
            else if (subject !== null) context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Workflow '${name}' declares its subject more than once.`, locationOf(line));
            else {
                subject = found[1];
                directiveLocations.for = locationOf(line);
            }
            rejectOperationChildren(context, line, DiagnosticCodes.InvalidWorkflowDeclaration, 'A for line cannot have children.');
        } else {
            const node = parseNode(context, line, name, keyword === 'trigger' ? ++triggers : triggers);
            if (node !== undefined) nodes.push(node);
        }
    }
    if (subject === null) context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Workflow '${name}' must name its subject: 'for <ReadModel>'.`, locationOf(header));
    const seen = new Set<string>();
    for (const node of nodes) {
        if (seen.has(node.name)) context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Workflow '${name}' declares node '${node.name}' more than once.`, node.location);
        seen.add(node.name);
    }
    return { kind: 'WorkflowSyntax', name, subject: subject ?? '', description, nodes, specifications, location: locationOf(header), directiveLocations };
}

function parseNode(context: ParserContext, line: SourceLine, workflow: string, ordinal: number): WorkflowNodeSyntax | undefined {
    const keyword = firstWord(line.content);
    let node: WorkflowNodeSyntax | undefined;
    switch (keyword) {
        case 'trigger': node = parseTrigger(context, line, ordinal); break;
        case 'action': node = parseAction(context, line); break;
        case 'decide': node = parseDecide(context, line); break;
        case 'wait': node = parseWait(context, line); break;
        case 'approval': node = parseApproval(context, line); break;
        case 'foreach': node = parseForEach(context, line); break;
        case 'join': node = parseJoin(context, line); break;
        default:
            context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Unexpected '${line.content}' in workflow '${workflow}' - expected for, description, specification or a node: trigger, action, decide, wait, approval, foreach or join.`, locationOf(line));
    }
    if (node === undefined) {
        context.skipBlock(line.indent);
        return undefined;
    }
    return { ...node, edges: parseEdges(context, line) };
}

function parseEdges(context: ParserContext, header: SourceLine): WorkflowEdgeSyntax[] {
    const edges: WorkflowEdgeSyntax[] = [];
    for (let line = context.peekChild(header.indent); line !== undefined; line = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const shorthand = shorthandEdge.exec(line.content);
        const edge = edgePattern.exec(line.content);
        if (shorthand !== null) edges.push({ kind: 'WorkflowEdgeSyntax', outlet: 'next', target: shorthand[1], location: locationOf(line) });
        else if (edge !== null) edges.push({ kind: 'WorkflowEdgeSyntax', outlet: edge[1] ?? edge[2], target: edge[3] ?? '', location: locationOf(line) });
        else {
            context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid edge '${line.content}' - expected '<outlet> -> <Node>'.`, locationOf(line));
            context.skipBlock(line.indent);
            continue;
        }
        rejectOperationChildren(context, line, DiagnosticCodes.InvalidWorkflowDeclaration, 'An edge cannot have children.');
    }
    return edges;
}

function parseTrigger(context: ParserContext, line: SourceLine, ordinal: number): WorkflowNodeSyntax | undefined {
    const match = triggerPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid trigger '${line.content}' - expected 'trigger [<Name>] on <Event>', 'when <StateMachine> enters <State>', 'every <n> <unit>', 'at <HH:mm>' or 'manual'.`, locationOf(line));
        return undefined;
    }
    const name = match[1] ?? (ordinal === 1 ? 'Trigger' : `Trigger${ordinal}`);
    const rest = match[2];
    const location = locationOf(line);
    if (rest === 'manual') return empty('Trigger', name, location, { triggerKind: 'Manual' });
    const onEvent = onEventPattern.exec(rest);
    if (onEvent !== null) return empty('Trigger', name, location, { triggerKind: 'Event', event: onEvent[1] });
    const enters = entersPattern.exec(rest);
    if (enters !== null) return empty('Trigger', name, location, { triggerKind: 'StateEntered', stateMachine: enters[1], state: enters[2] });
    const scheduled = rest.startsWith('every ') || rest.startsWith('at ');
    if (scheduled) {
        const schedule = parseTriggerSource(context, { ...line, content: rest });
        return schedule === undefined ? undefined : empty('Trigger', name, location, { triggerKind: 'Schedule', schedule });
    }
    context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid trigger '${line.content}' - expected 'on <Event>', 'when <StateMachine> enters <State>', 'every <n> <unit>', 'at <HH:mm>' or 'manual'.`, locationOf(line));
    return undefined;
}

function parseAction(context: ParserContext, line: SourceLine): WorkflowNodeSyntax | undefined {
    const match = actionPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid action '${line.content}' - expected 'action <Name> sends <Command> [attempts <n>] [timeout <n> <unit>] [retry after <n> <unit>]'.`, locationOf(line));
        return undefined;
    }
    let attempts: number | null = null;
    let timeout: WorkflowDurationSyntax | null = null;
    let retryAfter: WorkflowDurationSyntax | null = null;
    for (const option of match[3].matchAll(actionOption)) {
        if (option[1] !== undefined) {
            const count = Number(option[1]);
            if (attempts !== null || !Number.isInteger(count) || count < 1) context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid or repeated attempts in '${line.content}'.`, locationOf(line));
            else attempts = count;
        } else if (option[2] !== undefined) {
            const parsed = duration(context, option[2], option[3], line);
            if (timeout !== null || parsed === undefined) context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid or repeated timeout in '${line.content}'.`, locationOf(line));
            else timeout = parsed;
        } else {
            const parsed = duration(context, option[4], option[5], line);
            if (retryAfter !== null || parsed === undefined) context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid or repeated retry in '${line.content}'.`, locationOf(line));
            else retryAfter = parsed;
        }
    }
    return empty('Action', match[1], locationOf(line), { command: match[2], attempts, timeout, retryAfter });
}

function parseDecide(context: ParserContext, line: SourceLine): WorkflowNodeSyntax | undefined {
    const match = decidePattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid decide '${line.content}' - expected 'decide <Name> when <condition>'.`, locationOf(line));
        return undefined;
    }
    const condition = parseCondition(context, match[2], locationOf(line));
    return condition === null ? undefined : empty('Decide', match[1], locationOf(line), { condition });
}

function parseWait(context: ParserContext, line: SourceLine): WorkflowNodeSyntax | undefined {
    const match = waitPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid wait '${line.content}' - expected 'wait <Name> for <Event> [expires after <n> <unit>]' or 'wait <Name> for <n> <unit>'.`, locationOf(line));
        return undefined;
    }
    if (match[2] !== undefined) {
        const parsed = duration(context, match[2], match[3], line);
        return parsed === undefined ? undefined : empty('Wait', match[1], locationOf(line), { duration: parsed });
    }
    let expiresAfter: WorkflowDurationSyntax | null = null;
    if (match[5] !== undefined) {
        const parsed = duration(context, match[5], match[6], line);
        if (parsed === undefined) return undefined;
        expiresAfter = parsed;
    }
    return empty('Wait', match[1], locationOf(line), { event: match[4], expiresAfter });
}

function parseApproval(context: ParserContext, line: SourceLine): WorkflowNodeSyntax | undefined {
    const match = approvalPattern.exec(line.content);
    if (match === null) {
        context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid approval '${line.content}' - expected 'approval <Name> by <Persona or Policy> [expires after <n> <unit>]'.`, locationOf(line));
        return undefined;
    }
    let expiresAfter: WorkflowDurationSyntax | null = null;
    if (match[3] !== undefined) {
        const parsed = duration(context, match[3], match[4], line);
        if (parsed === undefined) return undefined;
        expiresAfter = parsed;
    }
    return empty('Approval', match[1], locationOf(line), { by: match[2], expiresAfter });
}

function parseForEach(context: ParserContext, line: SourceLine): WorkflowNodeSyntax | undefined {
    const match = forEachPattern.exec(line.content);
    if (match === null || (match[3] !== undefined && Number(match[3]) < 1)) {
        context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid foreach '${line.content}' - expected 'foreach <Name> over <property> [parallel <n>]'.`, locationOf(line));
        return undefined;
    }
    return empty('ForEach', match[1], locationOf(line), { items: match[2], parallel: match[3] === undefined ? null : Number(match[3]) });
}

function parseJoin(context: ParserContext, line: SourceLine): WorkflowNodeSyntax | undefined {
    const match = joinPattern.exec(line.content);
    if (match === null || (match[3] !== undefined && Number(match[3]) < 1)) {
        context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid join '${line.content}' - expected 'join <Name> all', 'join <Name> any' or 'join <Name> at least <n>'.`, locationOf(line));
        return undefined;
    }
    return empty('Join', match[1], locationOf(line), { joinMode: match[2] === 'all' ? 'All' : match[2] === 'any' ? 'Any' : 'AtLeast', joinCount: match[3] === undefined ? null : Number(match[3]) });
}

function duration(context: ParserContext, amount: string, text: string, line: SourceLine): WorkflowDurationSyntax | undefined {
    const count = Number(amount);
    if (!Number.isInteger(count) || count < 1) {
        context.error(DiagnosticCodes.InvalidWorkflowDeclaration, `Invalid duration '${amount} ${text}' in '${line.content}' - an amount is at least 1.`, locationOf(line));
        return undefined;
    }
    const base = text.replace(/s$/, '');
    const parsed: IntervalUnit = base === 'second' ? 'Seconds' : base === 'minute' ? 'Minutes' : base === 'hour' ? 'Hours' : 'Days';
    return { kind: 'WorkflowDurationSyntax', amount: count, unit: parsed, location: locationOf(line) };
}

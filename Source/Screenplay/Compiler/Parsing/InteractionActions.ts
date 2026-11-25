// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InteractionActionSyntax, InteractionArgumentSyntax } from '../Syntax/Interactions';
import { firstWord } from './LineText';
import { ParserContext } from './ParserContext';
import { SourceLine, locationOf } from './SourceLine';
import { stringBody, stringLiteralText } from './StringLiteral';

interface ActionBody {
    readonly arguments: InteractionArgumentSyntax[];
    readonly onSuccess: InteractionActionSyntax[];
    readonly onFailure: InteractionActionSyntax[];
    readonly onResult: InteractionActionSyntax[];
}

type ActionHead =
    | { kind: 'ExecuteCommandActionSyntax'; command: string }
    | { kind: 'NavigateActionSyntax'; screen: string }
    | { kind: 'NavigateBackActionSyntax' }
    | { kind: 'OpenDialogActionSyntax'; dialogTemplate: string }
    | { kind: 'CloseDialogActionSyntax' }
    | { kind: 'RefreshQueryActionSyntax'; query: string }
    | { kind: 'SetStateActionSyntax'; target: string; value: string }
    | { kind: 'NotifyActionSyntax'; level: 'Info' | 'Warning' | 'Error'; message: string; messageIsLiteral: boolean }
    | { kind: 'ConfirmActionSyntax'; message: string; messageIsLiteral: boolean }
    | { kind: 'RaiseTriggerActionSyntax'; trigger: string };

const target = '([A-Za-z_]\\w*(?:\\.\\w+)*)';
const notification = new RegExp(`^notify\\s+(info|warning|error)\\s+(?:"(${stringBody})"|(\\$strings\\.\\w+(?:\\.\\w+)*)|([A-Za-z_][\\w.]*))$`);
const confirmation = new RegExp(`^confirm\\s+(?:"(${stringBody})"|(\\$strings\\.\\w+(?:\\.\\w+)*)|([A-Za-z_][\\w.]*))$`);
const levels = { info: 'Info', warning: 'Warning', error: 'Error' } as const;
const continuationMembers = { success: 'onSuccess', failure: 'onFailure', result: 'onResult' } as const;
interface TargetAction { readonly pattern: RegExp; readonly build: (value: string) => ActionHead }
const targetedActions: Readonly<Record<string, TargetAction>> = {
    execute: { pattern: new RegExp(`^execute\\s+${target}$`), build: command => ({ kind: 'ExecuteCommandActionSyntax', command }) },
    navigate: { pattern: new RegExp(`^navigate\\s+to\\s+${target}$`), build: screen => ({ kind: 'NavigateActionSyntax', screen }) },
    open: { pattern: new RegExp(`^open\\s+dialog\\s+${target}$`), build: dialogTemplate => ({ kind: 'OpenDialogActionSyntax', dialogTemplate }) },
    refresh: { pattern: new RegExp(`^refresh\\s+${target}$`), build: query => ({ kind: 'RefreshQueryActionSyntax', query }) },
    raise: { pattern: new RegExp(`^raise\\s+${target}$`), build: trigger => ({ kind: 'RaiseTriggerActionSyntax', trigger }) },
};

/** The same action grammar serves inline bindings, alternatives and all three continuations. */
export function parseInteractionAction(context: ParserContext, line: SourceLine, depth = 0): InteractionActionSyntax | null {
    const head = actionHead(line.content);
    if (head === null || depth >= 16) {
        context.skipBlock(line.indent);
        return null;
    }
    return { ...head, ...parseActionBody(context, line, head.kind, depth), location: locationOf(line) };
}

export function parseInteractionActionList(context: ParserContext, header: SourceLine, depth = 0): InteractionActionSyntax[] {
    const actions: InteractionActionSyntax[] = [];
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const action = parseInteractionAction(context, child, depth);
        if (action !== null) actions.push(action);
    }
    return actions;
}

function parseActionBody(context: ParserContext, header: SourceLine, kind: ActionHead['kind'], depth: number): ActionBody {
    const body: ActionBody = { arguments: [], onSuccess: [], onFailure: [], onResult: [] };
    for (let child = context.peekChild(header.indent); child !== undefined; child = context.peekChild(header.indent)) {
        context.reader.takeSignificant();
        const argument = /^with\s+([A-Za-z_]\w*)\s+from\s+(.+)$/.exec(child.content);
        if (argument !== null) {
            body.arguments.push({ kind: 'InteractionArgumentSyntax', name: argument[1], binding: argument[2].trim(), location: locationOf(child) });
            continue;
        }
        const continuation = /^on\s+(success|failure|result)$/.exec(child.content);
        if (continuation !== null && acceptsContinuation(kind, continuation[1])) {
            const member = continuationMembers[continuation[1] as keyof typeof continuationMembers];
            body[member].push(...parseInteractionActionList(context, child, depth + 1));
        } else context.skipBlock(child.indent);
    }
    return body;
}

function acceptsContinuation(kind: ActionHead['kind'], continuation: string): boolean {
    if (continuation === 'result') return kind === 'OpenDialogActionSyntax';
    return ['ExecuteCommandActionSyntax', 'RefreshQueryActionSyntax', 'ConfirmActionSyntax', 'OpenDialogActionSyntax', 'RaiseTriggerActionSyntax'].includes(kind);
}

function actionHead(text: string): ActionHead | null {
    if (/^navigate\s+back$/.test(text)) return { kind: 'NavigateBackActionSyntax' };
    if (/^close\s+dialog$/.test(text)) return { kind: 'CloseDialogActionSyntax' };
    const keyword = firstWord(text);
    if (Object.hasOwn(targetedActions, keyword)) return targeted(text, targetedActions[keyword]);
    switch (keyword) {
        case 'set': return stateAction(text);
        case 'notify': return notifyAction(text);
        case 'confirm': return confirmAction(text);
        default: return null;
    }
}

function targeted(text: string, action: TargetAction): ActionHead | null {
    const match = action.pattern.exec(text);
    return match === null ? null : action.build(match[1]);
}

function stateAction(text: string): ActionHead | null {
    const match = /^set\s+([\w.]+)\s+to\s+(.+)$/.exec(text);
    return match === null ? null : { kind: 'SetStateActionSyntax', target: match[1], value: match[2].trim() };
}

function notifyAction(text: string): ActionHead | null {
    const match = notification.exec(text);
    return match === null ? null : { kind: 'NotifyActionSyntax', level: levels[match[1] as keyof typeof levels], message: message(match, 2), messageIsLiteral: match[2] !== undefined };
}

function confirmAction(text: string): ActionHead | null {
    const match = confirmation.exec(text);
    return match === null ? null : { kind: 'ConfirmActionSyntax', message: message(match, 1), messageIsLiteral: match[1] !== undefined };
}

function message(match: RegExpExecArray, literal: number): string {
    return match[literal] === undefined ? match[literal + 1] ?? match[literal + 2] : stringLiteralText(match[literal]);
}

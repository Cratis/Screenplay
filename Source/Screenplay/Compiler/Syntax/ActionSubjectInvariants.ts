// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InvalidSyntaxJson } from './InvalidSyntaxJson';
import { ScreenActionSyntax, ScreenGuardedActionSyntax, ToolbarItemSyntax } from './Screens';
import { SyntaxNode } from './SyntaxNode';

export function validateActionSubject(node: SyntaxNode, validateCollection: (value: unknown, kind: string, message: string) => void): void {
    if (!['ScreenActionSyntax', 'ScreenGuardedActionSyntax', 'ToolbarItemSyntax'].includes(node.kind)) return;
    const action = node as ScreenActionSyntax | ScreenGuardedActionSyntax | ToolbarItemSyntax;
    if (!isValidActionSubject(action.subject)) throw new InvalidSyntaxJson('An action subject must be Item or Screen.');
    if (hasInapplicableToolbarSubject(action)) throw new InvalidSyntaxJson('Only an action toolbar item can have a subject.');
    if (action.kind === 'ScreenActionSyntax' && action.inputs !== undefined) validateCollection(action.inputs, 'InteractionArgumentSyntax', 'Action inputs must be a collection of arguments.');
}

function isValidActionSubject(subject: unknown): boolean {
    return subject == null || subject === 'Item' || subject === 'Screen';
}

function hasInapplicableToolbarSubject(action: ScreenActionSyntax | ScreenGuardedActionSyntax | ToolbarItemSyntax): boolean {
    return action.kind === 'ToolbarItemSyntax' && action.subject != null && action.syntaxKind !== 'Action';
}

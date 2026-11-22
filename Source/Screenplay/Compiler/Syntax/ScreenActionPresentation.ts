// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PresentationValueSyntax, ScreenActionSubjectKind, ScreenActionSyntax } from './Screens';
import { SyntaxNode } from './SyntaxNode';
import { InvalidSyntaxJson } from './InvalidSyntaxJson';

const invalidValueMessage = 'Action presentation requires a key and string value.';

// The reader calls this after restoring the schema-validated shape. Writers preserve erroneous
// parser output so authoring tools can describe a partial document alongside its diagnostics.
export function validateActionPresentation(node: SyntaxNode): void {
    if (node.kind !== 'ScreenActionSyntax') return;
    const action = node as ScreenActionSyntax;
    if (action.presentation === undefined) return;
    const reason = invalidActionPresentation(action.presentation, action.subject);
    if (reason !== undefined) throw new InvalidSyntaxJson(reason);
}

export function invalidActionPresentation(presentation: readonly PresentationValueSyntax[] | undefined, subject?: ScreenActionSubjectKind | null): string | undefined {
    if (presentation === undefined) return undefined;
    if (presentation.length > 0 && subject !== 'Item') return "Action presentation requires an explicitly item-scoped action ('for item').";
    const names = new Set<string>();
    for (const value of presentation) {
        const reason = invalidValue(value, names);
        if (reason !== undefined) return reason;
    }
    return undefined;
}

function invalidValue(value: PresentationValueSyntax, names: Set<string>): string | undefined {
    if (!hasNameAndValue(value)) return invalidValueMessage;
    if (names.has(value.name)) return `Duplicate action presentation key '${value.name}'.`;
    names.add(value.name);
    if (value.name !== 'placement') return undefined;
    return value.value.trim().length === 0 ? 'Action presentation placement must not be empty.' : undefined;
}

function hasNameAndValue(value: PresentationValueSyntax): boolean {
    if (value == null) return false;
    if (typeof value.name !== 'string') return false;
    if (value.name.trim().length === 0) return false;
    return typeof value.value === 'string';
}

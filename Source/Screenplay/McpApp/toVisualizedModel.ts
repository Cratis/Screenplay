// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { VisualizedModel } from './VisualizedModel';

// The tool result as the board needs it. A result that is an error, or that does not carry documents, is
// reported in the words the server used rather than drawn as an empty board.
export function toVisualizedModel(result: { isError?: boolean; structuredContent?: unknown; content?: unknown }): VisualizedModel | Error {
    const structured = result.structuredContent as Partial<VisualizedModel> | undefined;
    if (result.isError || structured === undefined || !Array.isArray(structured.documents) || typeof structured.application !== 'string') {
        return new Error(textOf(result.content) ?? 'The server sent nothing the board can draw.');
    }
    return structured as VisualizedModel;
}

function textOf(content: unknown): string | undefined {
    if (!Array.isArray(content)) {
        return undefined;
    }
    const texts = content.filter(block => block?.type === 'text' && typeof block.text === 'string').map(block => block.text as string);
    return texts.length === 0 ? undefined : texts.join('\n');
}

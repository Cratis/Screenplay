// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap } from './document-context';

/** Where a named construct is declared. */
export interface DeclarationHeader {
    /** The construct keyword, such as `concept`, `screen template` or `slice StateChange`. */
    readonly keyword: string;

    /** The name the construct declares. */
    readonly name: string;

    /** The zero-based line of the header. */
    readonly line: number;

    /** The zero-based column the name starts at. */
    readonly column: number;
}

// Every construct that is declared by a header line naming it: `concept TalkId : Uuid`, `screen template ReviewDesk`,
// `slice StateChange SubmitTalk`. References to a name (`navigate to TalkList`, `template ReviewDesk`, `authorize
// IsSpeaker`) are not headers, so a reference never reads as a second declaration.
const header = new RegExp(
    '^(\\s*(?:public\\s+)?(?:' + [
        'screen\\s+template', 'dialog\\s+template', 'slice\\s+\\w+', 'ui\\s+profile', 'sample\\s+data', 'toolbar',
        'concept', 'type', 'policy', 'purpose', 'persona', 'module', 'layout', 'theme', 'feature', 'event', 'system', 'eventsource',
        'operation', 'command', 'query', 'projection', 'reaction', 'reducer', 'readmodel', 'trigger', 'screen', 'dialog', 'form',
        'constraint', 'specification', 'behavior', 'exposure', 'instance',
    ].join('|') + ')\\s+)([A-Za-z_][\\w.]*)');

/**
 * Finds the headers that declare constructs in a document.
 *
 * @param lines The document's lines.
 * @returns The headers, in document order.
 */
export function declarationHeaders(lines: readonly string[]): DeclarationHeader[] {
    const fences = fenceMap([...lines]);
    const headers: DeclarationHeader[] = [];
    lines.forEach((text, line) => {
        if (fences[line] || /^\s*\/\//.test(text)) return;
        const match = header.exec(text);
        if (match === null) return;
        const keyword = match[1].trim().replace(/^public\s+/, '').replace(/\s+/g, ' ');
        headers.push({ keyword, name: match[2], line, column: match[1].length });
    });

    return headers;
}

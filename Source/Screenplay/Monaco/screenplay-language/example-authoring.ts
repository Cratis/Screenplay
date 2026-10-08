// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, withoutComment } from './document-context';
import { responseAnalysis } from './response-analysis';
import { DocumentSymbols, symbolsForBuffer } from './symbols';

function analyze(lines: string[], application?: DocumentSymbols) {
    const symbols = symbolsForBuffer(lines, application);
    return responseAnalysis(lines, symbols.authoringDocuments ?? symbols.authoringSources?.filter(source => source !== lines.join('\n')) ?? [],
        symbols.authoringPlacement, symbols.authoringPath, symbols.authoringPlacementResolved).examples;
}

export function exampleCompletions(lines: string[], line: number, before: string, application?: DocumentSymbols) {
    if (fenceMap(lines)[line] || withoutComment(before).length < before.length) return null;
    return analyze(lines, application).completions(line, before);
}

export function exampleHover(lines: string[], line: number, start: number, end: number, application?: DocumentSymbols) {
    return analyze(lines, application).hover(line, start, end);
}

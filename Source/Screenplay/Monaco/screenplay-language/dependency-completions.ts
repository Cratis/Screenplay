// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fenceMap, withoutComment } from './document-context';
import { responseAnalysis } from './response-analysis';
import { DocumentSymbols, symbolsForBuffer } from './symbols';
import { CompletionEntry } from './completion-items';

export function dependencyTargetCompletions(lines: string[], line: number, before: string, application: DocumentSymbols): CompletionEntry[] | null {
    if (fenceMap(lines)[line] || withoutComment(before).length < before.length) return null;
    const match = before.match(/^\s*depends\s+on\s+((?:[A-Za-z_]\w*\.)*)([A-Za-z_]\w*)?$/);
    if (!match) return null;
    const symbols = symbolsForBuffer(lines, application);
    const analysis = responseAnalysis(lines, symbols.authoringDocuments ?? symbols.authoringSources?.filter(source => source !== lines.join('\n')) ?? [],
        symbols.authoringPlacement, symbols.authoringPath, symbols.authoringPlacementResolved);
    return analysis.dependencies.completions(line, match[1], symbols.authoringPlacement);
}

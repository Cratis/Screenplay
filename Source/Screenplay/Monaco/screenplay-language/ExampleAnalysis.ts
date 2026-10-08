// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CompletionEntry } from './completion-items';

export interface ExampleAnalysis {
    completions(line: number, before: string): CompletionEntry[] | null;
    hover(line: number, start: number, end: number): string | null;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceLocation } from './SourceLocation';

export type DiagnosticSeverity = 'error' | 'warning';

// A problem the parser found. Codes are the C# compiler's PLAYnnnn codes, so a diagnostic means the same
// thing whichever compiler reported it.
export interface Diagnostic {
    readonly severity: DiagnosticSeverity;
    readonly code: string;
    readonly message: string;
    readonly location: SourceLocation;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The report the C# `migrate` tool returns for one migrated document. */
export function migrationReport(file: Record<string, unknown> = {}): Record<string, unknown> {
    return {
        summary: '1 file rewritten · 0 need review · 0 refused',
        from: 5,
        to: 6,
        steps: [{ from: 5, to: 6, name: 'slots-and-outlets' }],
        outcome: file.outcome ?? 'migrated',
        files: [{
            path: 'shell.play',
            outcome: 'migrated',
            changed: true,
            written: false,
            failure: null,
            changes: [{ step: 'slots-and-outlets', line: 3, column: 3, before: 'navigation topbar', after: 'outlet topbar of navigation' }],
            reviews: [],
            diagnostics: [],
            source: 'layout AppShell\n  outlet topbar of navigation\n',
            ...file,
        }],
    };
}

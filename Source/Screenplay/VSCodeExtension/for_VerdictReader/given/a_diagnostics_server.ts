// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface ServedFinding { code: string; severity?: string; path?: string; line?: number }

export function finding(value: ServedFinding): Record<string, unknown> {
    return { code: value.code, message: `${value.code} message`, severity: value.severity ?? 'Warning', location: { path: value.path ?? 'app.play', line: value.line ?? 3, column: 5 } };
}

/** The MCP diagnostics tool: serves the findings in pages of `size`, as the real tool pages by offset/limit. */
export function diagnosticsServer(findings: Record<string, unknown>[], options: { size?: number; revisions?: string[]; status?: string } = {}) {
    const calls: Record<string, unknown>[] = [];
    const call = async (args: Record<string, unknown>): Promise<Record<string, unknown>> => {
        calls.push(args);
        const offset = args.offset as number;
        const size = options.size ?? 200;
        const items = findings.slice(offset, offset + size);
        const revision = options.revisions?.[calls.length - 1] ?? 'rev-1';
        return {
            success: true, sourceRevision: revision, fileCount: 1,
            page: { revision, totalCount: findings.length, offset, items, nextOffset: offset + items.length < findings.length ? offset + items.length : null },
            ...(options.status ? { completenessStatus: options.status } : {}),
        };
    };
    return { call, calls };
}

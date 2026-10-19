// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairFailure } from '../../RepairClient';

export interface ServedFinding { code: string; severity?: string; path?: string | null; line?: number; column?: number }

export function finding(value: ServedFinding): Record<string, unknown> {
    return { code: value.code, message: `${value.code} message`, severity: value.severity ?? 'Warning', location: { path: value.path === undefined ? 'app.play' : value.path, line: value.line ?? 3, column: value.column ?? 5 } };
}

/** The MCP diagnostics tool (shapes follow McpModelQueries.cs, McpPage.cs and McpJson: camelCase, null path kept, completeness fields present when `checks` is set): serves the findings in pages of `size`, as the real tool pages by offset/limit. */
export function diagnosticsServer(findings: Record<string, unknown>[], options: { size?: number; revisions?: string[]; status?: string; binder?: Record<string, unknown>[] | 'refused'; driftedWorkspace?: boolean } = {}) {
    const calls: Record<string, unknown>[] = [];
    const workspaceCalls: Record<string, unknown>[] = [];
    const workspace = (revision: string) => ({ revision, catalogRevision: 'catalog-1' });
    const call = async (tool: string, args: Record<string, unknown>): Promise<Record<string, unknown>> => {
        if (tool === 'open-workspace') {
            if (options.binder === 'refused') throw new RepairFailure('ToolRefused', 'no binding');
            return { workspace: workspace('ws-1') };
        }
        if (tool === 'read-workspace') {
            workspaceCalls.push(args);
            const items = (options.binder as Record<string, unknown>[] ?? []).slice(args.offset as number, (args.offset as number) + 1);
            const total = (options.binder as Record<string, unknown>[] ?? []).length;
            const next = (args.offset as number) + items.length;
            return { workspace: workspace(options.driftedWorkspace ? 'ws-2' : 'ws-1'), view: args.view, page: { revision: 'ws-1', totalCount: total, offset: args.offset, items, nextOffset: next < total ? next : null } };
        }
        calls.push(args);
        const offset = args.offset as number;
        const size = options.size ?? 200;
        const items = findings.slice(offset, offset + size);
        const revision = options.revisions?.[calls.length - 1] ?? 'rev-1';
        return {
            success: true, sourceRevision: revision, fileCount: 1, summary: { errors: 0, warnings: findings.length, information: 0 },
            page: { revision, totalCount: findings.length, offset, items, nextOffset: offset + items.length < findings.length ? offset + items.length : null },
            completenessStatus: options.status ?? null, completenessCoverage: 'structure only; a finding is a prompt to look',
        };
    };
    return { call, calls, workspaceCalls };
}

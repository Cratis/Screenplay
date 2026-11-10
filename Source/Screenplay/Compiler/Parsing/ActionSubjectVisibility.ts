// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ScreenActionSyntax, ScreenDataSyntax, ScreenDirectiveSyntax } from '../Syntax/Screens';

export interface ActionSubjectVisibility {
    readonly data: readonly ScreenDataSyntax[];
    readonly tables: readonly string[];
}

// The data and tables inside a container: its own where it has any, otherwise those it inherits.
export function visibleWithin<T extends ActionSubjectVisibility>(directives: readonly ScreenDirectiveSyntax[], inherited: T): T {
    const local = directives.filter((node): node is Extract<ScreenDirectiveSyntax, { kind: 'ScreenDataSyntax' }> => node.kind === 'ScreenDataSyntax');
    const tables = directives.filter((node): node is Extract<ScreenDirectiveSyntax, { kind: 'ScreenTableSyntax' }> => node.kind === 'ScreenTableSyntax').map(node => node.target);
    return { ...inherited, data: local.length > 0 ? local : inherited.data, tables: tables.length > 0 ? tables : inherited.tables };
}

// An explicit 'for item' acts on the one collection data a table in scope shows; anything else keeps the nearest data.
export function itemSubjects(authored: ScreenActionSyntax['subject'], data: readonly ScreenDataSyntax[], tables: readonly string[]): readonly ScreenDataSyntax[] {
    if (authored !== 'Item' || data.length <= 1) return data;
    const shown = data.filter(entry => entry.type.isCollection && tables.includes(entry.type.name));
    return shown.length === 1 ? shown : data;
}

export function unresolvedSubjectMessage(subjects: readonly ScreenDataSyntax[], guarded: boolean): string {
    if (guarded) return "A guarded action requires exactly one nearest 'data' subject; no data or equally near data bindings leave 'item' unresolved";
    if (subjects.length <= 1) return "Action 'for item' requires one resolvable data subject - no item binding is inferred";
    const candidates = subjects.map(data => data.type.isCollection ? `${data.type.name}[]` : data.type.name).join(', ');
    return `Action 'for item' is ambiguous between data ${candidates} - keep exactly one data (or a table over exactly one collection data) in the action's container, or move the action into a section beside the data it acts on`;
}

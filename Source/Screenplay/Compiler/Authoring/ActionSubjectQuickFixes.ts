// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ActionSubjectEvidence, resolveActionSubjectApplication } from '../Parsing/ActionSubjectResolution';
import { ActionSubjectInputs } from '../Parsing/ActionSubjectInputs';
import { RefusalDeclarations } from '../Parsing/RefusalDeclarations';
import { splitLines } from '../Parsing/SourceLineSplitter';
import { parseForAuthoring } from '../ScreenplayCompiler';
import { InteractionArgumentSyntax, ScreenActionSubjectKind } from '../Syntax/Screens';
import { ApplicationSyntax } from '../Syntax/Structure';
import { toSyntaxJson } from '../Syntax/SyntaxJson';
import { pattern } from '../Text/patterns';
import { applyQuickFixEdits, QuickFix, QuickFixEdit, QuickFixOptions } from './QuickFixes';

export function prepareActionSubjectQuickFixes(source: string, options: Pick<QuickFixOptions, 'placement' | 'path' | 'documents'>): (line?: number, code?: string) => QuickFix[] {
    // Imported application snapshots belong to the native workspace; never guess missing declarations in a buffer.
    if (options.documents !== undefined && options.documents.size > 1) return () => [];
    const original = parseForAuthoring(source, undefined, options.placement);
    if (!original.success) return () => [];
    const declarations = new RefusalDeclarations(original.value);
    const resolver = new ActionSubjectInputs(declarations, []);
    const evidence: ActionSubjectEvidence[] = [];
    resolveActionSubjectApplication(original.value, entry => evidence.push(entry));
    const lines = splitLines(source);
    const ending = source.includes('\r\n') ? '\r\n' : '\n';
    const fixes: QuickFix[] = [];
    for (const entry of evidence) {
        if (entry.authored.subject != null || declarations.resolve(entry.command, entry.context.slice, slice => slice.commands) === null) continue;
        const diagnostic = original.diagnostics.find(diagnostic => diagnostic.location.line === entry.authored.location.line && diagnostic.code === DiagnosticCodes.InferredActionSubject);
        if (diagnostic === undefined) continue;
        const subject = entry.effective.subject ?? 'Screen';
        const explicit = candidate(entry, subject, `Make inferred 'for ${subject.toLowerCase()}' explicit`);
        if (explicit !== undefined) fixes.push(explicit);
        if (original.diagnostics.some(diagnostic => diagnostic.location.line === entry.authored.location.line && diagnostic.code === DiagnosticCodes.ChangedActionSubject)) {
            const authored = entry.authored.kind === 'ScreenActionSyntax' ? entry.authored.inputs ?? [] : entry.authored.parameters.map(parameter => ({ kind: 'InteractionArgumentSyntax' as const, name: parameter.name, binding: parameter.binding.path, location: parameter.location }));
            const retained = resolver.legacyInputs(entry.command, authored, entry.context);
            if (retained !== null) {
                const legacy = candidate(entry, 'Item', 'Retain legacy row behavior explicitly', retained.slice(authored.length));
                if (legacy !== undefined) fixes.push({ ...legacy, diagnosticCode: DiagnosticCodes.ChangedActionSubject });
            }
            if (explicit !== undefined) fixes.push({ ...explicit, diagnosticCode: DiagnosticCodes.ChangedActionSubject });
        }
    }
    return (line, code) => fixes.filter(fix => (line === undefined || fix.line === line) && (code === undefined || fix.diagnosticCode === code));

    function candidate(entry: ActionSubjectEvidence, subject: ScreenActionSubjectKind, title: string, additions: readonly InteractionArgumentSyntax[] = []): QuickFix | undefined {
        const line = lines[entry.authored.location.line - 1];
        const header = pattern(entry.authored.kind === 'ScreenActionSyntax' ? '^(\\s*action\\s+[\\w.]+)' : '^(\\s*item\\s+\\w+\\s+action\\s+[\\w.]+)').exec(line.raw);
        if (header === null) return undefined;
        const edits: QuickFixEdit[] = [{ path: options.path, start: line.startOffset + header[0].length, length: 0, text: ` for ${subject.toLowerCase()}` }];
        const prefix = /^\s*/.exec(line.raw)![0];
        const end = lines.find(child => child.number > line.number && child.raw.trim().length > 0 && /^\s*/.exec(child.raw)![0].length <= prefix.length)?.startOffset ?? source.length;
        if (additions.length > 0) {
            const indent = lines.find(child => child.number > line.number && child.startOffset < end && child.raw.trim().length > 0)?.raw.match(/^\s*/)![0] ?? `${prefix}  `;
            const text = additions.map(input => `${indent}${entry.authored.kind === 'ScreenActionSyntax' ? 'with' : 'parameter'} ${input.name} from ${entry.authored.kind === 'ScreenActionSyntax' ? '' : 'data '}${input.binding}`).join(ending) + ending;
            edits.push({ path: options.path, start: end, length: 0, text: (end > 0 && source[end - 1] !== '\n' ? ending : '') + text });
        }
        const replacement = entry.authored.kind === 'ScreenActionSyntax'
            ? { ...entry.authored, subject, inputs: [...(entry.authored.inputs ?? []), ...additions] }
            : { ...entry.authored, subject, parameters: [...entry.authored.parameters, ...additions.map(input => ({ kind: 'ScreenNavigationParameterSyntax', name: input.name, binding: { kind: 'UiBindingSyntax', bindingKind: 'DataContext', path: input.binding, query: null, componentId: null, componentPropertyPath: null, mode: null, nullBehavior: null, expectedValueType: null, literal: null, rawText: null, location: input.location }, location: input.location }))] };
        const expected = JSON.parse(JSON.stringify(original.value, (_key, value: unknown) => value === entry.authored ? replacement : value)) as ApplicationSyntax;
        const edited = applyQuickFixEdits(source, edits);
        if (edited === undefined) return undefined;
        const parsed = parseForAuthoring(edited, undefined, options.placement);
        if (!parsed.success || JSON.stringify(toSyntaxJson(parsed.value)) !== JSON.stringify(toSyntaxJson(expected))) return undefined;
        if (parsed.diagnostics.some(diagnostic => diagnostic.location.line === line.number && (diagnostic.code === DiagnosticCodes.InferredActionSubject || diagnostic.code === DiagnosticCodes.ChangedActionSubject))) return undefined;
        return { line: line.number, diagnosticCode: DiagnosticCodes.InferredActionSubject, title, scope: 'occurrence', edits };
    }
}

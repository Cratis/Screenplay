// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
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

type Options = Pick<QuickFixOptions, 'placement' | 'path' | 'documents'>;
interface RepairContext {
    source: string;
    options: Options;
    application: ApplicationSyntax;
    diagnostics: readonly Diagnostic[];
    declarations: RefusalDeclarations;
    resolver: ActionSubjectInputs;
    lines: ReturnType<typeof splitLines>;
    ending: string;
}
interface Proposal {
    subject: ScreenActionSubjectKind;
    title: string;
    additions?: readonly InteractionArgumentSyntax[];
}

export function prepareActionSubjectQuickFixes(source: string, options: Options): (line?: number, code?: string) => QuickFix[] {
    if (!/^\s*(?:action\s+|item\s+\w+\s+action\s+)/mu.test(source)) return () => [];
    // Imported application snapshots belong to the native workspace; never guess missing declarations in a buffer.
    if (options.documents !== undefined && options.documents.size > 1) return () => [];
    const original = parseForAuthoring(source, undefined, options.placement);
    if (!original.success) return () => [];
    const declarations = new RefusalDeclarations(original.value);
    const context: RepairContext = { source, options, application: original.value, diagnostics: original.diagnostics, declarations,
        resolver: new ActionSubjectInputs(declarations, []), lines: splitLines(source), ending: source.includes('\r\n') ? '\r\n' : '\n' };
    const evidence: ActionSubjectEvidence[] = [];
    resolveActionSubjectApplication(original.value, entry => evidence.push(entry));
    const fixes = evidence.flatMap(entry => entryFixes(context, entry));
    return (line, code) => selectFixes(fixes, line, code);
}

function selectFixes(fixes: QuickFix[], line?: number, code?: string): QuickFix[] {
    return fixes.filter(fix => (line === undefined || fix.line === line) && (code === undefined || fix.diagnosticCode === code));
}

function entryFixes(context: RepairContext, entry: ActionSubjectEvidence): QuickFix[] {
    if (entry.authored.subject != null || context.declarations.resolve(entry.command, entry.context.slice, slice => slice.commands) === null) return [];
    if (!hasDiagnostic(context, entry, DiagnosticCodes.InferredActionSubject)) return [];
    const subject = entry.effective.subject ?? 'Screen';
    const explicit = candidate(context, entry, { subject, title: `Make inferred 'for ${subject.toLowerCase()}' explicit` });
    const fixes = explicit === undefined ? [] : [explicit];
    return hasDiagnostic(context, entry, DiagnosticCodes.ChangedActionSubject) ? [...fixes, ...changedFixes(context, entry)] : fixes;
}

function hasDiagnostic(context: RepairContext, entry: ActionSubjectEvidence, code: string): boolean {
    return context.diagnostics.some(diagnostic => diagnostic.location.line === entry.authored.location.line && diagnostic.code === code);
}

function changedFixes(context: RepairContext, entry: ActionSubjectEvidence): QuickFix[] {
    const proposals: Proposal[] = [
        { subject: 'Item', title: "Use 'for item' (item-bound action)" },
        { subject: 'Screen', title: "Use 'for screen' (no selection binding)" },
    ];
    const authored = authoredInputs(entry);
    const retained = context.resolver.legacyInputs(entry.command, authored, entry.context);
    if (retained !== null && retained.length > authored.length) proposals.push({ subject: 'Item', title: 'Keep legacy row bindings explicitly', additions: retained.slice(authored.length) });
    return proposals.flatMap(proposal => {
        const fix = candidate(context, entry, proposal);
        return fix === undefined ? [] : [{ ...fix, diagnosticCode: DiagnosticCodes.ChangedActionSubject }];
    });
}

function authoredInputs(entry: ActionSubjectEvidence): readonly InteractionArgumentSyntax[] {
    return entry.authored.kind === 'ScreenActionSyntax' ? entry.authored.inputs ?? [] : entry.authored.parameters.map(parameter => ({ kind: 'InteractionArgumentSyntax', name: parameter.name, binding: parameter.binding.path, location: parameter.location }));
}

function candidate(context: RepairContext, entry: ActionSubjectEvidence, proposal: Proposal): QuickFix | undefined {
    const line = context.lines[entry.authored.location.line - 1];
    const header = pattern(entry.authored.kind === 'ScreenActionSyntax' ? '^(\\s*action\\s+[\\w.]+)' : '^(\\s*item\\s+\\w+\\s+action\\s+[\\w.]+)').exec(line.raw);
    if (header === null) return undefined;
    const edits: QuickFixEdit[] = [{ path: context.options.path, start: line.startOffset + header[0].length, length: 0, text: ` for ${proposal.subject.toLowerCase()}` }];
    if (proposal.additions !== undefined) edits.push(inputEdit(context, entry, proposal.additions));
    const replacement = replacementNode(entry, proposal);
    const expected = JSON.parse(JSON.stringify(context.application, (_key, value: unknown) => value === entry.authored ? replacement : value)) as ApplicationSyntax;
    if (!matchesExpected(context, entry, edits, expected)) return undefined;
    return { line: line.number, diagnosticCode: DiagnosticCodes.InferredActionSubject, title: proposal.title, scope: 'occurrence', edits };
}

function inputEdit(context: RepairContext, entry: ActionSubjectEvidence, additions: readonly InteractionArgumentSyntax[]): QuickFixEdit {
    const line = context.lines[entry.authored.location.line - 1];
    const prefix = /^\s*/.exec(line.raw)![0];
    const end = actionEnd(context, entry);
    const indent = context.lines.find(child => child.number > line.number && child.startOffset < end && child.raw.trim().length > 0)?.raw.match(/^\s*/)![0] ?? `${prefix}  `;
    const text = additions.map(input => inputLine(entry, input, indent)).join(context.ending) + context.ending;
    return { path: context.options.path, start: end, length: 0, text: (end > 0 && context.source[end - 1] !== '\n' ? context.ending : '') + text };
}

function actionEnd(context: RepairContext, entry: ActionSubjectEvidence): number {
    const line = context.lines[entry.authored.location.line - 1];
    const prefix = /^\s*/.exec(line.raw)![0];
    return context.lines.find(child => child.number > line.number && child.raw.trim().length > 0 && /^\s*/.exec(child.raw)![0].length <= prefix.length)?.startOffset ?? context.source.length;
}

function inputLine(entry: ActionSubjectEvidence, input: InteractionArgumentSyntax, indent: string): string {
    return `${indent}${entry.authored.kind === 'ScreenActionSyntax' ? 'with' : 'parameter'} ${input.name} from ${entry.authored.kind === 'ScreenActionSyntax' ? '' : 'data '}${input.binding}`;
}

function replacementNode(entry: ActionSubjectEvidence, proposal: Proposal): ActionSubjectEvidence['authored'] {
    const additions = proposal.additions ?? [];
    return entry.authored.kind === 'ScreenActionSyntax'
        ? { ...entry.authored, subject: proposal.subject, inputs: [...(entry.authored.inputs ?? []), ...additions] }
        : { ...entry.authored, subject: proposal.subject, parameters: [...entry.authored.parameters, ...additions.map(input => ({ kind: 'ScreenNavigationParameterSyntax' as const, name: input.name, binding: { kind: 'UiBindingSyntax' as const, bindingKind: 'DataContext' as const, path: input.binding, query: null, componentId: null, componentPropertyPath: null, mode: null, nullBehavior: null, expectedValueType: null, literal: null, rawText: null, location: input.location }, location: input.location }))] };
}

function matchesExpected(context: RepairContext, entry: ActionSubjectEvidence, edits: QuickFixEdit[], expected: ApplicationSyntax): boolean {
    const edited = applyQuickFixEdits(context.source, edits);
    if (edited === undefined) return false;
    const parsed = parseForAuthoring(edited, undefined, context.options.placement);
    if (!parsed.success || JSON.stringify(toSyntaxJson(parsed.value)) !== JSON.stringify(toSyntaxJson(expected))) return false;
    return !parsed.diagnostics.some(diagnostic => diagnostic.location.line === entry.authored.location.line && (diagnostic.code === DiagnosticCodes.InferredActionSubject || diagnostic.code === DiagnosticCodes.ChangedActionSubject));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { CompilationResult } from '../ScreenplayCompiler';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from '../Syntax/Structure';
import { ScreenActionSyntax, ScreenDirectiveSyntax, ScreenGuardedActionSyntax, ScreenSyntax, ToolbarItemSyntax } from '../Syntax/Screens';
import { RefusalDeclarations } from './RefusalDeclarations';
import { ActionSubjectContext, ActionSubjectInputs } from './ActionSubjectInputs';

export interface ActionSubjectEvidence {
    readonly authored: ScreenActionSyntax | ToolbarItemSyntax;
    readonly effective: ScreenActionSyntax | ToolbarItemSyntax;
    readonly command: string;
    readonly context: ActionSubjectContext;
}

export function resolveActionSubjectApplication(application: ApplicationSyntax, observe?: (evidence: ActionSubjectEvidence) => void): CompilationResult<ApplicationSyntax> {
    const diagnostics: Diagnostic[] = [];
    const resolver = new ActionSubjectInputs(new RefusalDeclarations(application), diagnostics);
    const hasSubject = (context: ActionSubjectContext, guarded: boolean): boolean => {
        if (context.data.length === 1) return true;
        const candidates = context.data.map(data => data.type.isCollection ? `${data.type.name}[]` : data.type.name).join(', ');
        const ambiguous = `Action 'for item' is ambiguous between data ${candidates} - keep exactly one data (or a table over exactly one collection data) in the action's container, or move the action into a section beside the data it acts on`;
        const unresolved = context.data.length > 1 ? ambiguous : "Action 'for item' requires one resolvable data subject - no item binding is inferred";
        const message = guarded ? "A guarded action requires exactly one nearest 'data' subject; no data or equally near data bindings leave 'item' unresolved" : unresolved;
        diagnostics.push({ severity: 'warning', code: DiagnosticCodes.UnresolvedActionSubject, message, location: context.location });
        return false;
    };
    // An explicit 'for item' acts on the one collection data a table in scope shows; anything else keeps the nearest data.
    const itemContext = (authored: ScreenActionSyntax['subject'], visible: ActionSubjectContext): ActionSubjectContext => {
        if (authored !== 'Item' || visible.data.length <= 1) return visible;
        const shown = visible.data.filter(data => data.type.isCollection && visible.tables.includes(data.type.name));
        return shown.length === 1 ? { ...visible, data: shown } : visible;
    };
    const toolbar = (item: ToolbarItemSyntax, visible: ActionSubjectContext): ToolbarItemSyntax => {
        if (item.syntaxKind !== 'Action') return item;
        const context = itemContext(item.subject, visible);
        const subject = resolver.subject(item.subject, item.target, item.parameters.map(parameter => parameter.name), context);
        const authored = item.parameters.map(parameter => ({ kind: 'InteractionArgumentSyntax' as const, name: parameter.name, binding: parameter.binding.path, location: parameter.location }));
        const additions = subject === 'Item' && hasSubject(context, false) ? resolver.inputs(item.target, authored, context).slice(authored.length) : [];
        const parameters = [...item.parameters, ...additions.map(input => ({ kind: 'ScreenNavigationParameterSyntax' as const, name: input.name, binding: { kind: 'UiBindingSyntax' as const, bindingKind: 'DataContext' as const, path: input.binding, query: null, componentId: null, componentPropertyPath: null, mode: null, nullBehavior: null, expectedValueType: null, literal: null, rawText: null, location: input.location }, location: input.location }))];
        const effective = subject == null && additions.length === 0 ? item : { ...item, subject, parameters };
        observe?.({ authored: item, effective, command: item.target, context });
        return effective;
    };
    const guarded = (action: ScreenGuardedActionSyntax, context: ActionSubjectContext): ScreenGuardedActionSyntax => {
        if (!hasSubject(context, true)) return action;
        if (action.subject === 'Screen' && context.data[0].type.isCollection) {
            diagnostics.push({ severity: 'warning', code: DiagnosticCodes.UnsupportedActionSubjectContext, message: "A guarded action 'for screen' cannot use a collection as 'item' - use a single-record data context or 'for item'", location: action.location });
            return action;
        }
        const fallback = action.otherwise;
        return { ...action,
            alternatives: action.alternatives.map(alternative => ({ ...alternative, arguments: resolver.inputs(alternative.command, alternative.arguments, { ...context, location: alternative.location }) })),
            otherwise: fallback !== null && fallback.command !== null ? { ...fallback, arguments: resolver.inputs(fallback.command, fallback.arguments, { ...context, location: fallback.location }) } : fallback,
        };
    };
    const plain = (node: ScreenActionSyntax, visible: ActionSubjectContext): ScreenActionSyntax => {
        const context = itemContext(node.subject, visible);
        const subject = resolver.subject(node.subject, node.command, (node.inputs ?? []).map(input => input.name), context);
        const inputs = subject === 'Item' && hasSubject(context, false) ? resolver.inputs(node.command, node.inputs ?? [], context) : node.inputs;
        const effective = subject == null && inputs === node.inputs ? node : { ...node, subject, ...(inputs === undefined ? {} : { inputs }) };
        observe?.({ authored: node, effective, command: node.command, context });
        return effective;
    };
    const body = (directives: readonly ScreenDirectiveSyntax[], inherited: ActionSubjectContext): readonly ScreenDirectiveSyntax[] => {
        const local = directives.filter((node): node is Extract<ScreenDirectiveSyntax, { kind: 'ScreenDataSyntax' }> => node.kind === 'ScreenDataSyntax');
        const tables = directives.filter((node): node is Extract<ScreenDirectiveSyntax, { kind: 'ScreenTableSyntax' }> => node.kind === 'ScreenTableSyntax').map(node => node.target);
        const scope = { ...inherited, data: local.length > 0 ? local : inherited.data, tables: tables.length > 0 ? tables : inherited.tables };
        return directives.map(node => {
            const context = { ...scope, location: node.location };
            switch (node.kind) {
                case 'ScreenActionSyntax': return plain(node, context);
                case 'ScreenGuardedActionSyntax': return guarded(node, context);
                case 'ScreenToolbarSyntax': return { ...node, items: node.items.map(item => toolbar(item, { ...context, location: item.location })) };
                case 'ScreenSectionSyntax': return { ...node, directives: body(node.directives, scope) };
                case 'ScreenTemplateReferenceSyntax': return { ...node, slots: node.slots.map(slot => ({ ...slot, directives: body(slot.directives, scope) })) };
                case 'ScreenComponentSyntax': return { ...node, slots: node.slots.map(slot => ({ ...slot, directives: body(slot.directives, scope) })) };
                default: return node;
            }
        });
    };
    const screen = (node: ScreenSyntax, slice: SliceSyntax): ScreenSyntax => {
        const context = { data: [], tables: [], slice, location: node.location };
        return { ...node, directives: body(node.directives, context), ...(node.contributions === undefined ? {} : { contributions: node.contributions.map(contribution => ({ ...contribution, directives: body(contribution.directives, context) })) }) };
    };
    const feature = (node: FeatureSyntax): FeatureSyntax => ({ ...node, features: node.features.map(feature), slices: node.slices.map(slice => ({ ...slice, screens: slice.screens.map(node => screen(node, slice)) })) });
    const value = { ...application, modules: application.modules.map(module => ({ ...module, features: module.features.map(feature) })) };
    return { value, diagnostics, success: !diagnostics.some(diagnostic => diagnostic.severity === 'error') };
}

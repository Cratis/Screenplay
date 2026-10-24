// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from './Diagnostics/Diagnostic';
import { DiagnosticCodes } from './Diagnostics/DiagnosticCodes';
import { ActionSubjectInputs } from './Parsing/ActionSubjectInputs';
import { RefusalDeclarations } from './Parsing/RefusalDeclarations';
import type { CompilationResult } from './ScreenplayCompiler';
import { ScreenDataSyntax, ScreenDirectiveSyntax, ScreenGuardedActionSyntax, ToolbarItemSyntax } from './Syntax/Screens';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from './Syntax/Structure';

/** Resolves effective action subjects and inputs without modifying authored syntax. */
export function resolveActionSubjects(application: ApplicationSyntax): CompilationResult<ApplicationSyntax> {
    const diagnostics: Diagnostic[] = [];
    const resolver = new ActionSubjectInputs(new RefusalDeclarations(application), diagnostics);

    function toolbar(item: ToolbarItemSyntax, subjects: readonly ScreenDataSyntax[], slice: SliceSyntax): ToolbarItemSyntax {
        if (item.syntaxKind !== 'Action') return item;
        const subject = resolver.subject(item.subject, item.target, subjects, slice, item.location);
        if (subject !== 'Item') return { ...item, subject };
        const authored = item.parameters.map(parameter => ({ kind: 'InteractionArgumentSyntax' as const, name: parameter.name, binding: '', location: parameter.location }));
        const inferred = resolver.inputs(item.target, authored, subjects, slice, item.location).slice(authored.length);
        return { ...item, subject, parameters: [...item.parameters, ...inferred.map(input => ({ kind: 'ScreenNavigationParameterSyntax' as const, name: input.name, binding: { kind: 'UiBindingSyntax' as const, bindingKind: 'DataContext' as const, path: input.binding, query: null, componentId: null, componentPropertyPath: null, mode: null, nullBehavior: null, expectedValueType: null, literal: null, rawText: null, location: input.location }, location: input.location }))] };
    }

    function guarded(action: ScreenGuardedActionSyntax, subjects: readonly ScreenDataSyntax[], slice: SliceSyntax): ScreenGuardedActionSyntax {
        if (action.subject === 'Screen' && subjects.length === 1 && subjects[0].type.isCollection) {
            diagnostics.push({ severity: 'warning', code: DiagnosticCodes.UnsupportedActionSubjectContext, message: "A guarded action 'for screen' cannot use a collection as 'item' - use a single-record data context or 'for item'", location: action.location });
            return action;
        }
        return { ...action, subject: action.subject ?? 'Item', alternatives: action.alternatives.map(alternative => ({ ...alternative, arguments: resolver.inputs(alternative.command, alternative.arguments, subjects, slice, alternative.location) })), otherwise: action.otherwise?.command != null ? { ...action.otherwise, arguments: resolver.inputs(action.otherwise.command, action.otherwise.arguments, subjects, slice, action.otherwise.location) } : action.otherwise };
    }

    function container(nodes: readonly ScreenDirectiveSyntax[], inherited: readonly ScreenDataSyntax[], slice: SliceSyntax): readonly ScreenDirectiveSyntax[] {
        const local = nodes.filter((node): node is ScreenDataSyntax => node.kind === 'ScreenDataSyntax');
        const subjects = local.length === 0 ? inherited : local;
        return nodes.map(node => {
            switch (node.kind) {
                case 'ScreenActionSyntax': {
                    const subject = resolver.subject(node.subject, node.command, subjects, slice, node.location);
                    const inputs = subject === 'Item' ? resolver.inputs(node.command, node.inputs ?? [], subjects, slice, node.location) : node.inputs;
                    return { ...node, subject, ...(inputs === undefined ? {} : { inputs }) };
                }
                case 'ScreenGuardedActionSyntax': return guarded(node, subjects, slice);
                case 'ScreenToolbarSyntax': return { ...node, items: node.items.map(item => toolbar(item, subjects, slice)) };
                case 'ScreenSectionSyntax': return { ...node, directives: container(node.directives, subjects, slice) };
                case 'ScreenTemplateReferenceSyntax': return { ...node, slots: node.slots.map(slot => ({ ...slot, directives: container(slot.directives, subjects, slice) })) };
                case 'ScreenComponentSyntax': return { ...node, outlets: node.outlets.map(outlet => ({ ...outlet, directives: container(outlet.directives, subjects, slice) })) };
                default: return node;
            }
        });
    }

    function feature(node: FeatureSyntax): FeatureSyntax {
        return { ...node, features: node.features.map(feature), slices: node.slices.map(slice => ({ ...slice, screens: slice.screens.map(screen => ({ ...screen, directives: container(screen.directives, [], slice), ...(screen.contributions === undefined ? {} : { contributions: screen.contributions.map(contribution => ({ ...contribution, directives: container(contribution.directives, [], slice) })) }) })) })) };
    }

    const value = { ...application, modules: application.modules.map(module => ({ ...module, features: module.features.map(feature) })) };
    return { value, diagnostics, success: !diagnostics.some(diagnostic => diagnostic.severity === 'error') };
}

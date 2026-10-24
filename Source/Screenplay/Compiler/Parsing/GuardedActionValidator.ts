// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveActionSubjects } from '../ScreenActionSubjects';
import { ScreenDataSyntax, ScreenDirectiveSyntax, ScreenGuardedActionSyntax, ScreenTableSyntax } from '../Syntax/Screens';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { comparisons, validateArguments, validateItemPath, viewProperties } from './GuardedActionInputs';
import { validateGuardedActionShadowing } from './GuardedActionShadowing';
import { GuardedInteractions } from './GuardedInteractions';
import { ParserContext } from './ParserContext';
import { RefusalDeclarations } from './RefusalDeclarations';

// Resolves known subject and input shapes without guessing imported or ambiguous declarations.
export function validateGuardedActions(application: ApplicationSyntax, context: ParserContext): void {
    for (const diagnostic of resolveActionSubjects(application).diagnostics) context[diagnostic.severity](diagnostic.code, diagnostic.message, diagnostic.location, diagnostic.arguments);
    const declarations = new RefusalDeclarations(application);
    const interactions = new GuardedInteractions(declarations, context);
    interactions.structural(application);
    for (const { slice } of declarations.slices) {
        for (const screen of slice.screens) validateContainer(screen.directives, [], slice, declarations, context, interactions);
    }
}

function tableSubjects(table: ScreenTableSyntax, subjects: readonly ScreenDataSyntax[], slice: SliceSyntax, declarations: RefusalDeclarations): readonly ScreenDataSyntax[] {
    const direct = subjects.filter(subject => subject.type.name === table.target);
    if (direct.length > 0 || subjects.length !== 1) return direct;
    const source = subjects[0];
    const fields = viewProperties(source.type.name, slice, declarations)?.filter(property => property.name === table.target && property.type.isCollection);
    return fields?.length === 1 ? [{ ...source, type: fields[0].type, by: null, location: table.location }] : [];
}

function validateContainer(directives: readonly ScreenDirectiveSyntax[], inherited: readonly ScreenDataSyntax[], slice: SliceSyntax, declarations: RefusalDeclarations, context: ParserContext, interactions: GuardedInteractions): void {
    const local = directives.filter(directive => directive.kind === 'ScreenDataSyntax');
    const subjects = local.length === 0 ? inherited : local;
    for (const directive of directives) {
        switch (directive.kind) {
            case 'ScreenGuardedActionSyntax':
                validateAction(directive, subjects, slice, declarations, context);
                break;
            case 'ScreenActionSyntax':
                validateArguments(directive.command, directive.inputs ?? [], subjects.length === 1 ? viewProperties(subjects[0].type.name, slice, declarations) : null, slice, declarations, context);
                break;
            case 'ScreenBehaviorSyntax':
                interactions.at(directive.location, subjects, slice);
                break;
            case 'ScreenUsesBehaviorSyntax':
                interactions.useAt(directive.location, subjects, slice);
                break;
            case 'ScreenTableSyntax':
                interactions.at(directive.location, tableSubjects(directive, subjects, slice, declarations), slice, true);
                break;
            case 'ScreenComponentSyntax':
                interactions.at(directive.location, subjects, slice, true);
                for (const outlet of directive.outlets) validateContainer(outlet.directives, subjects, slice, declarations, context, interactions);
                break;
            case 'ScreenSectionSyntax':
                validateContainer(directive.directives, subjects, slice, declarations, context, interactions);
                break;
            case 'ScreenTemplateReferenceSyntax':
                for (const slot of directive.slots) validateContainer(slot.directives, subjects, slice, declarations, context, interactions);
                break;
        }
    }
}

function validateAction(action: ScreenGuardedActionSyntax, subjects: readonly ScreenDataSyntax[], slice: SliceSyntax, declarations: RefusalDeclarations, context: ParserContext): void {
    const properties = subjects.length === 1 ? viewProperties(subjects[0].type.name, slice, declarations) : null;
    for (const alternative of action.alternatives) {
        for (const comparison of comparisons(alternative.condition)) validateItemPath(comparison.left, comparison.location, properties, slice, declarations, context);
        validateArguments(alternative.command, alternative.arguments, properties, slice, declarations, context);
    }
    if (action.otherwise?.command != null) validateArguments(action.otherwise.command, action.otherwise.arguments, properties, slice, declarations, context);
    validateGuardedActionShadowing(action, context);
}

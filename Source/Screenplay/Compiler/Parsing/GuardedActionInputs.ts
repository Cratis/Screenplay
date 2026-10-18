// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ComparisonConditionSyntax, ConditionSyntax } from '../Syntax/Conditions';
import { PropertySyntax } from '../Syntax/Declarations';
import { InteractionArgumentSyntax } from '../Syntax/Screens';
import { SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { RefusalDeclarations } from './RefusalDeclarations';

const primitives = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime']);

export function viewProperties(name: string, slice: SliceSyntax | undefined, declarations: RefusalDeclarations): readonly PropertySyntax[] | null {
    return declarations.resolve(name, slice, candidate => candidate.readModels)?.node.properties ?? null;
}

export function validateArguments(commandName: string, arguments_: readonly InteractionArgumentSyntax[], properties: readonly PropertySyntax[] | null, slice: SliceSyntax | undefined, declarations: RefusalDeclarations, context: ParserContext): void {
    const command = declarations.resolve(commandName, slice, candidate => candidate.commands)?.node;
    for (const argument of arguments_) {
        const inputs = command?.properties.filter(property => property.name === argument.name);
        if (inputs?.length === 0) context.warning(DiagnosticCodes.UnknownActionArgumentProperty, `Command '${commandName}' has no argument property '${argument.name}'`, argument.location);
        const subjectProperty = validateItemPath(argument.binding, argument.location, properties, slice, declarations, context, true);
        if (inputs?.length === 1 && subjectProperty !== null && inputs[0].type.isCollection !== subjectProperty.type.isCollection) {
            context.warning(DiagnosticCodes.UnknownActionArgumentProperty, `Command argument '${commandName}.${argument.name}' and subject field '${argument.binding}' must have matching collection cardinality`, argument.location);
        }
    }
}

export function validateItemPath(path: string, location: SourceLocation, properties: readonly PropertySyntax[] | null, slice: SliceSyntax | undefined, declarations: RefusalDeclarations, context: ParserContext, allowTerminalCollection = false): PropertySyntax | null {
    if (!path.startsWith('item.') || properties === null) return null;
    const segments = path.substring('item.'.length).split('.');
    for (const [index, segment] of segments.entries()) {
        const matches: readonly PropertySyntax[] = properties.filter(property => property.name === segment);
        if (matches.length === 0 || (matches.length === 1 && matches[0].type.isCollection && (!allowTerminalCollection || index < segments.length - 1))) {
            context.warning(DiagnosticCodes.UnknownActionSubjectField, `Unknown or collection-valued subject field '${path}' - guarded actions require an item field path`, location);
            return null;
        }
        if (matches.length !== 1) return null;
        if (index === segments.length - 1) return matches[0];
        const type = matches[0].type;
        properties = declarations.types.get(type.name)?.properties ?? viewProperties(type.name, slice, declarations);
        if (properties === null) {
            if (primitives.has(type.name) || declarations.application.concepts.some(concept => concept.name === type.name) && !declarations.application.types.some(composite => composite.name === type.name)) {
                context.warning(DiagnosticCodes.UnknownActionSubjectField, `Subject field '${path}' continues past scalar '${segment}'`, location);
            }
            return null;
        }
    }
    return null;
}

export function comparisons(condition: ConditionSyntax): readonly ComparisonConditionSyntax[] {
    return condition.kind === 'ComparisonConditionSyntax' ? [condition] : [...comparisons(condition.left), ...comparisons(condition.right)];
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax } from '../Syntax/Declarations';
import { InteractionArgumentSyntax, ScreenActionSubjectKind, ScreenDataSyntax } from '../Syntax/Screens';
import { SliceSyntax } from '../Syntax/Structure';
import { RefusalDeclarations } from './RefusalDeclarations';

export interface ActionSubjectContext {
    readonly data: readonly ScreenDataSyntax[];
    readonly slice: SliceSyntax;
    readonly location: SourceLocation;
}

export class ActionSubjectInputs {
    constructor(readonly declarations: RefusalDeclarations, readonly diagnostics: Diagnostic[]) {}

    properties(context: ActionSubjectContext): readonly PropertySyntax[] {
        return context.data.length === 1 ? this.declarations.resolve(context.data[0].type.name, context.slice, entry => entry.readModels)?.node.properties ?? [] : [];
    }

    command(name: string, context: ActionSubjectContext): readonly PropertySyntax[] {
        return this.declarations.resolve(name, context.slice, entry => entry.commands)?.node.properties ?? [];
    }

    sameConcept(input: PropertySyntax, key: PropertySyntax): boolean {
        return !key.type.isCollection && !key.type.isOptional && input.type.name === key.type.name && this.declarations.application.concepts.filter(concept => concept.name === input.type.name).length === 1;
    }

    eligible(input: PropertySyntax): boolean {
        return input.isIdentifier && !input.isGenerated && !input.type.isOptional && !input.type.isCollection;
    }

    keys(input: PropertySyntax, properties: readonly PropertySyntax[]): readonly PropertySyntax[] {
        return this.eligible(input) ? properties.filter(property => property.isKey && this.sameConcept(input, property)) : [];
    }

    binding(input: PropertySyntax, properties: readonly PropertySyntax[]): PropertySyntax | null {
        const named = properties.filter(property => property.name === input.name && this.declarations.compatible(property.type, input.type) === true);
        if (named.length === 1) return named[0];
        const keys = this.keys(input, properties);
        return keys.length === 1 ? keys[0] : null;
    }

    subject(authored: ScreenActionSubjectKind | null | undefined, command: string, bound: readonly string[], context: ActionSubjectContext): ScreenActionSubjectKind | null {
        if (authored != null) return authored;
        const identifiers = this.command(command, context).filter(input => this.eligible(input));
        const keys = identifiers.length === 1 ? this.keys(identifiers[0], this.properties(context)) : [];
        const item = context.data.length === 1 && context.data[0].type.isCollection && keys.length === 1;
        this.diagnostics.push({ severity: 'information', code: DiagnosticCodes.InferredActionSubject, message: `Action '${command}' infers 'for ${item ? 'item' : 'screen'}' from its declared data and identifier types`, location: context.location });
        this.migration(command, bound, context, item);
        return item ? 'Item' : null;
    }

    migration(command: string, bound: readonly string[], context: ActionSubjectContext, item: boolean): void {
        if (context.data.length !== 1 || !context.data[0].type.isCollection) return;
        const properties = this.properties(context);
        const inputs = this.command(command, context).filter(input => !input.isGenerated && !bound.includes(input.name));
        const legacy = inputs.flatMap(input => properties.filter(property => input.name.toLowerCase() === property.name.toLowerCase()).map(property => `${input.name} from item.${property.name}`));
        const current = item ? inputs.flatMap(input => {
            const property = this.binding(input, properties);
            return property === null ? [] : [`${input.name} from item.${property.name}`];
        }) : [];
        if (JSON.stringify(legacy) !== JSON.stringify(current)) this.diagnostics.push({ severity: 'warning', code: DiagnosticCodes.ChangedActionSubject, message: `Action '${command}' now infers 'for ${item ? 'item' : 'screen'}'; legacy row-name matching would use ${legacy.length === 0 ? 'no row property' : legacy.join(', ')}`, location: context.location });
    }

    legacyInputs(command: string, authored: readonly InteractionArgumentSyntax[], context: ActionSubjectContext): readonly InteractionArgumentSyntax[] | null {
        if (context.data.length !== 1 || !context.data[0].type.isCollection) return null;
        const result = [...authored];
        const properties = this.properties(context);
        for (const input of this.command(command, context).filter(input => !input.isGenerated)) {
            if (result.some(argument => argument.name === input.name)) continue;
            const matches = properties.filter(property => property.name.toLowerCase() === input.name.toLowerCase());
            if (matches.length > 1 || (matches.length === 0 && this.keys(input, properties).length > 0)) return null;
            if (matches.length === 1) result.push({ kind: 'InteractionArgumentSyntax', name: input.name, binding: `item.${matches[0].name}`, location: context.location });
        }
        return result;
    }

    inputs(command: string, authored: readonly InteractionArgumentSyntax[], context: ActionSubjectContext): readonly InteractionArgumentSyntax[] {
        const result = [...authored];
        if (context.data.length !== 1) return result;
        const properties = this.properties(context);
        for (const input of this.command(command, context).filter(input => !input.isGenerated)) {
            if (result.some(argument => argument.name === input.name)) continue;
            const property = this.binding(input, properties);
            if (property !== null) result.push({ kind: 'InteractionArgumentSyntax', name: input.name, binding: `item.${property.name}`, location: context.location });
            else if (this.keys(input, properties).length > 1) this.diagnostics.push({ severity: 'warning', code: DiagnosticCodes.AmbiguousActionInputBinding, message: `Input '${input.name}' of action '${command}' matches several declared key fields - write an explicit binding`, location: context.location });
        }
        return result;
    }
}

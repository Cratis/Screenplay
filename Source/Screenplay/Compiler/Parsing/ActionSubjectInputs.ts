// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { PropertySyntax } from '../Syntax/Declarations';
import { InteractionArgumentSyntax, ScreenActionSubjectKind, ScreenDataSyntax } from '../Syntax/Screens';
import { SliceSyntax } from '../Syntax/Structure';
import { RefusalDeclarations } from './RefusalDeclarations';

export class ActionSubjectInputs {
    constructor(readonly declarations: RefusalDeclarations, readonly diagnostics: Diagnostic[]) {}

    properties(subjects: readonly ScreenDataSyntax[], slice: SliceSyntax): readonly PropertySyntax[] {
        return subjects.length === 1 ? this.declarations.resolve(subjects[0].type.name, slice, entry => entry.readModels)?.node.properties ?? [] : [];
    }

    command(name: string, slice: SliceSyntax): readonly PropertySyntax[] {
        return this.declarations.resolve(name, slice, entry => entry.commands)?.node.properties ?? [];
    }

    sameConcept(input: PropertySyntax, key: PropertySyntax): boolean {
        return !key.type.isCollection && !key.type.isOptional && input.type.name === key.type.name && this.declarations.application.concepts.filter(concept => concept.name === input.type.name).length === 1;
    }

    eligible(input: PropertySyntax): boolean {
        return input.isIdentifier && !input.isGenerated && !input.type.isOptional && !input.type.isCollection;
    }

    subject(authored: ScreenActionSubjectKind | null | undefined, command: string, subjects: readonly ScreenDataSyntax[], slice: SliceSyntax, location: SourceLocation): ScreenActionSubjectKind {
        if (authored != null) return authored;
        const inputs = this.command(command, slice);
        const properties = this.properties(subjects, slice);
        const keys = properties.filter(property => property.isKey);
        const eligible = inputs.filter(input => this.eligible(input) && keys.some(key => this.sameConcept(input, key)));
        const inferred = subjects.length === 1 && subjects[0].type.isCollection && eligible.length === 1 && keys.filter(key => this.sameConcept(eligible[0], key)).length === 1 ? 'Item' : 'Screen';
        this.diagnostics.push({ severity: 'information', code: DiagnosticCodes.InferredActionSubject, message: `Action '${command}' infers 'for ${inferred.toLowerCase()}' from its declared data and identifier types`, location });
        if (subjects.length === 1 && subjects[0].type.isCollection) {
            const legacy = inputs.flatMap(input => properties.filter(property => input.name.toLowerCase() === property.name.toLowerCase()).map(property => `${input.name} from item.${property.name}`));
            if ((legacy.length > 0) !== (inferred === 'Item')) this.diagnostics.push({ severity: 'warning', code: DiagnosticCodes.ChangedActionSubject, message: `Action '${command}' now infers 'for ${inferred.toLowerCase()}'; legacy row-name matching would use ${legacy.length === 0 ? 'no row property' : legacy.join(', ')}`, location });
        }
        return inferred;
    }

    inputs(command: string, authored: readonly InteractionArgumentSyntax[], subjects: readonly ScreenDataSyntax[], slice: SliceSyntax, location: SourceLocation): readonly InteractionArgumentSyntax[] {
        const result = [...authored];
        if (subjects.length !== 1) return result;
        const properties = this.properties(subjects, slice);
        for (const input of this.command(command, slice).filter(input => !input.isGenerated)) {
            if (result.some(argument => argument.name === input.name)) continue;
            const named = properties.filter(property => property.name === input.name);
            if (named.length === 1) {
                result.push({ kind: 'InteractionArgumentSyntax', name: input.name, binding: `item.${named[0].name}`, location });
                continue;
            }
            if (!this.eligible(input)) continue;
            const keys = properties.filter(property => property.isKey && this.sameConcept(input, property));
            if (keys.length === 1) result.push({ kind: 'InteractionArgumentSyntax', name: input.name, binding: `item.${keys[0].name}`, location });
            else if (keys.length > 1) this.diagnostics.push({ severity: 'warning', code: DiagnosticCodes.AmbiguousActionInputBinding, message: `Input '${input.name}' of action '${command}' matches several declared key fields - write an explicit binding`, location });
        }
        return result;
    }
}

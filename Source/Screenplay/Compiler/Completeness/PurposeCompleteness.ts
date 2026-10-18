// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { RefusalDeclarations } from '../Parsing/RefusalDeclarations';
import { TypeRefSyntax } from '../Syntax/Declarations';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { PurposeReferenceSyntax } from '../Syntax/Purposes';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from '../Syntax/Structure';

class References extends ScreenplaySyntaxWalker {
    readonly names = new Set<string>();
    override visitPurposeReference(syntax: PurposeReferenceSyntax): void { this.names.add(syntax.name); }
}

class FieldTypes extends ScreenplaySyntaxWalker {
    readonly names = new Set<string>();
    override visitTypeRef(syntax: TypeRefSyntax): void { this.names.add(syntax.name); }
}

function coverage(application: ApplicationSyntax): readonly { slice: SliceSyntax; purposes: readonly string[] }[] {
    const result: { slice: SliceSyntax; purposes: readonly string[] }[] = [];
    const collect = (feature: FeatureSyntax, inherited: readonly string[]): void => {
        const purposes = [...new Set([...inherited, ...(feature.purposes ?? []).map(purpose => purpose.name)])];
        for (const slice of feature.slices) result.push({ slice, purposes: [...new Set([...purposes, ...(slice.purposes ?? []).map(purpose => purpose.name)])] });
        feature.features.forEach(child => collect(child, purposes));
    };
    application.modules.forEach(module => module.features.forEach(feature => collect(feature, (module.purposes ?? []).map(purpose => purpose.name))));
    return result;
}

function conceptsIn(application: ApplicationSyntax, declarations: RefusalDeclarations, slice: SliceSyntax) {
    const visitor = new FieldTypes();
    slice.commands.forEach(command => visitor.visitCommand(command));
    slice.events.forEach(event => visitor.visitEvent(event));
    slice.readModels.forEach(model => visitor.visitReadModel(model));
    slice.queries.forEach(query => visitor.visitQuery(query));
    const views = [...slice.queries.map(query => query.returnType.name), ...slice.commands.flatMap(command => dependencySourcesOf(command).reads ?? []).map(reads => reads.readModel)];
    for (const view of views) {
        const properties = declarations.resolve(view, slice, owner => owner.readModels)?.node.properties ?? [];
        properties.forEach(property => visitor.visitProperty(property));
    }
    const pending = [...visitor.names];
    for (let index = 0; index < pending.length; index++) {
        const type = application.types.find(type => type.name === pending[index]);
        for (const property of type?.properties ?? []) {
            if (!visitor.names.has(property.type.name)) {
                visitor.names.add(property.type.name);
                pending.push(property.type.name);
            }
        }
    }
    return application.concepts.filter(concept => visitor.names.has(concept.name));
}

export function purposeDiagnostics(application: ApplicationSyntax): readonly Diagnostic[] {
    const slices = coverage(application);
    const references = new References();
    references.visitApplication(application);
    const diagnostics: Diagnostic[] = [];
    for (const purpose of application.purposes ?? []) {
        if (purpose.basis === null) diagnostics.push({ severity: 'warning', code: DiagnosticCodes.PurposeWithoutBasis,
            message: `Purpose '${purpose.name}' has no declared basis (Art. 6(1)); this finding is a prompt to look, not a legal verdict`, location: purpose.location });
        if (!references.names.has(purpose.name)) diagnostics.push({ severity: 'warning', code: DiagnosticCodes.UnusedPurpose,
            message: `Purpose '${purpose.name}' is declared but never referenced`, location: purpose.location });
    }
    const declarations = new RefusalDeclarations(application);
    for (const { slice, purposes } of slices) {
        const concepts = conceptsIn(application, declarations, slice).filter(concept => concept.attributes.some(attribute => attribute.name === 'pii'));
        if (concepts.length === 0) continue;
        const covered = (application.purposes ?? []).filter(purpose => purposes.includes(purpose.name));
        if (covered.length === 0) diagnostics.push({ severity: 'warning', code: DiagnosticCodes.PersonalDataWithoutPurpose,
            message: `Slice '${slice.name}' carries pii concepts but has no declared purpose in scope`, location: slice.location });
        for (const purpose of covered) {
            if (purpose.condition === null && concepts.some(concept => concept.attributes.some(attribute => attribute.specialCategory != null))) diagnostics.push({ severity: 'warning', code: DiagnosticCodes.SpecialDataWithoutCondition,
                message: `Purpose '${purpose.name}' covers special-category pii in slice '${slice.name}' but has no condition (Art. 9(2))`, location: purpose.location });
            if (purpose.authorization === null && concepts.some(concept => concept.attributes.some(attribute => attribute.criminal))) diagnostics.push({ severity: 'warning', code: DiagnosticCodes.CriminalDataWithoutAuthorization,
                message: `Purpose '${purpose.name}' covers criminal-offence pii in slice '${slice.name}' but has no authorization (Art. 10)`, location: purpose.location });
        }
    }
    return diagnostics;
}

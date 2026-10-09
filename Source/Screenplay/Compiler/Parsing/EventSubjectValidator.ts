// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { PropertySyntax } from '../Syntax/Declarations';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { ApplicationSyntax } from '../Syntax/Structure';
import { supportsStreamIdentity } from './EventSourceValidator';
import { ParserContext } from './ParserContext';

export function validateEventSubjects(application: ApplicationSyntax, context: ParserContext): void {
    const refuse = (properties: readonly PropertySyntax[], owner: string): void => {
        for (const property of properties.filter(property => property.isSubject))
            context.error(DiagnosticCodes.InvalidSubjectOwner, `The subject modifier is only valid on event properties, not ${owner} properties (decision 0008: one data subject per event).`, property.location);
    };
    for (const type of application.types) refuse(type.properties, 'type');
    for (const { slice } of new AuthoringProductionResolver(application).slices) {
        for (const command of slice.commands) refuse(command.properties, 'command');
        for (const model of slice.readModels) for (const property of model.properties.filter(property => property.isSubject))
            context.error(DiagnosticCodes.ReadModelSubjectNotSupported, 'Read model subject marks are not yet supported. Chronicle resolves read model subjects and reserves _subject, __subject and __subjects; Screenplay support is tracked in #559.', property.location);
        for (const event of eventDeclarations(slice)) {
            const subjects = event.properties.filter(property => property.isSubject);
            for (const extra of subjects.slice(1))
                context.error(DiagnosticCodes.DuplicateEventSubject, `Event '${event.name}' has one data subject (decision 0008); '${extra.name}' is an extra mark after '${subjects[0].name}'. Choose one subject; no automatic repair is safe.`, extra.location);
            for (const property of subjects) {
                const type = property.type;
                if (type.isOptional) {
                    context.error(DiagnosticCodes.InvalidSubjectType, 'A subject must be required: a null value silently falls back to the event source.', property.location);
                    continue;
                }
                if (type.isCollection || application.types.some(composite => composite.name === type.name)) {
                    context.error(DiagnosticCodes.InvalidSubjectType, 'A subject must be a scalar identity, not a collection or composite type.', property.location);
                    continue;
                }
                const concepts = application.concepts.filter(concept => concept.name === type.name);
                if (concepts.some(concept => concept.attributes.some(attribute => ['pii', 'sensitive'].includes(attribute.name))))
                    context.error(DiagnosticCodes.ProtectedSubjectType, `Subject concept '${type.name}' is pii or secret. EventContext.Subject is stored in plaintext; use a surrogate Uuid identity.`, property.location);
                const primitive = concepts.length === 1 ? concepts[0].type : concepts.length === 0 && ['String', 'Uuid', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime'].includes(type.name) ? type.name : null;
                if (concepts.some(concept => concept.type === 'Enum') || primitive !== null && !supportsStreamIdentity(type, application))
                    context.error(DiagnosticCodes.InvalidSubjectType, 'Subjects support String, Uuid and their concepts, plus Int-backed concepts; bare Int, enums, Decimal, Bool, Date and DateTime are refused.', property.location);
            }
        }
    }
}

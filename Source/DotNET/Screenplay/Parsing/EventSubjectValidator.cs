// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static class EventSubjectValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        foreach (var type in application.Types ?? []) Refuse(type.Properties, "type");
        foreach (var (slice, _) in declarations.Slices)
        {
            foreach (var command in slice.Commands) Refuse(command.Properties, "command");
            foreach (var model in slice.ReadModels ?? [])
            {
                foreach (var property in model.Properties.Where(property => property.IsSubject))
                {
                    context.Error(DiagnosticCodes.ReadModelSubjectNotSupported, "Read model subject marks are not yet supported. Chronicle resolves read model subjects and reserves _subject, __subject and __subjects; Screenplay support is tracked in #559.", property.Location);
                }
            }
            foreach (var @event in EventDeclarations.In(slice))
            {
                var subjects = @event.Properties.Where(property => property.IsSubject).ToArray();
                foreach (var extra in subjects.Skip(1))
                {
                    context.Error(DiagnosticCodes.DuplicateEventSubject, $"Event '{@event.Name}' has one data subject (decision 0008); '{extra.Name}' is an extra mark after '{subjects[0].Name}'. Choose one subject; no automatic repair is safe.", extra.Location);
                }
                foreach (var property in subjects) ValidateTarget(property);
            }
        }

        void Refuse(IEnumerable<PropertySyntax> properties, string owner)
        {
            foreach (var property in properties.Where(property => property.IsSubject))
            {
                context.Error(DiagnosticCodes.InvalidSubjectOwner, $"The subject modifier is only valid on event properties, not {owner} properties (decision 0008: one data subject per event).", property.Location);
            }
        }

        void ValidateTarget(PropertySyntax property)
        {
            var type = property.Type;
            if (type.IsOptional)
            {
                context.Error(DiagnosticCodes.InvalidSubjectType, "A subject must be required: a null value silently falls back to the event source.", property.Location);
                return;
            }
            if (type.IsCollection || (application.Types ?? []).Any(composite => composite.Name == type.Name))
            {
                context.Error(DiagnosticCodes.InvalidSubjectType, "A subject must be a scalar identity, not a collection or composite type.", property.Location);
                return;
            }
            var concepts = application.Concepts.Where(concept => concept.Name == type.Name).ToArray();
            if (concepts.Any(concept => concept.AttributeNames.Contains(ConceptAttributeSyntax.Pii) || concept.AttributeNames.Contains(ConceptAttributeSyntax.Sensitive)))
            {
                context.Error(DiagnosticCodes.ProtectedSubjectType, $"Subject concept '{type.Name}' is pii or secret. EventContext.Subject is stored in plaintext; use a surrogate Uuid identity.", property.Location);
            }
            if (concepts.Any(concept => concept.IsEnum) || (EventSourceValidator.Primitive(type, application) is not null && !EventSourceValidator.SupportsStreamIdentity(type, application)))
            {
                context.Error(DiagnosticCodes.InvalidSubjectType, "Subjects support String, Uuid and their concepts, plus Int-backed concepts; bare Int, enums, Decimal, Bool, Date and DateTime are refused.", property.Location);
            }

            // Unknown/imported shapes retain the ordinary property unknown-type disposition.
        }
    }
}

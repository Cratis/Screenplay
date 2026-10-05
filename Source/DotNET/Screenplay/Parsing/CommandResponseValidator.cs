// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks syntax-only generated-value and response contracts without constructing executable semantics.
/// </summary>
internal static class CommandResponseValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        var commands = declarations.Slices.SelectMany(entry => entry.Slice.Commands).ToArray();
        var commandProperties = commands.SelectMany(command => command.Properties).ToHashSet();
        var concepts = application.Concepts.ToLookup(concept => concept.Name, StringComparer.Ordinal);
        var compositeTypes = (application.Types ?? []).Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        var primitives = ConceptSyntax.PrimitiveTypes.ToHashSet(StringComparer.Ordinal);
        var readModels = declarations.Slices.SelectMany(entry => entry.Slice.ReadModels ?? []).Select(model => model.Name).ToHashSet(StringComparer.Ordinal);
        new GeneratedDeclarationWalker(commandProperties, context).VisitApplication(application);
        foreach (var command in commands)
        {
            var properties = command.Properties.ToLookup(property => property.Name, StringComparer.Ordinal);
            foreach (var property in command.Properties.Where(property => property.IsGenerated))
            {
                var declaredConcepts = concepts[property.Type.Name].ToArray();
                if (property.Type.IsOptional || property.Type.IsCollection || (declaredConcepts.Length == 0 ? primitives.Contains(property.Type.Name) || compositeTypes.Contains(property.Type.Name) || readModels.Contains(property.Type.Name) : declaredConcepts.Length != 1 || declaredConcepts[0].Type != "Uuid"))
                {
                    context.Error(DiagnosticCodes.InvalidGeneratedType, "A generated property must be a required, noncollection concept backed by Uuid.", property.Location);
                }
            }

            switch (command.Response)
            {
                case ScalarCommandResponseSyntax scalar:
                    ValidateSource(scalar.Source, null, properties, readModels, context);
                    break;
                case RecordCommandResponseSyntax record:
                    var fields = record.Fields.ToArray();
                    if (fields.Length == 0) context.Error(DiagnosticCodes.InvalidCommandResponse, "A response block requires at least one field.", record.Location);
                    var names = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var field in fields)
                    {
                        if (!names.Add(field.Name)) context.Error(DiagnosticCodes.DuplicateResponseField, $"Duplicate response field '{field.Name}'.", field.Location);
                        ValidateSource(field.Source, field.Type, properties, readModels, context);
                    }

                    break;
            }
        }
    }

    static void ValidateSource(PropertyResponseSourceSyntax source, TypeRefSyntax? explicitType, ILookup<string, PropertySyntax> properties, HashSet<string> readModels, ParserContext context)
    {
        var matches = properties[source.Property].ToArray();
        if (matches.Length != 1)
        {
            context.Error(DiagnosticCodes.InvalidResponseSource, $"Response source '{source.Property}' must reference one direct command property.", source.Location);
            return;
        }

        var type = matches[0].Type;
        if (type.IsCollection || readModels.Contains(type.Name)) context.Error(DiagnosticCodes.InvalidResponseShape, "Collection and whole-read-model responses are not supported.", source.Location);
        if (explicitType is not null && (explicitType.Name != type.Name || explicitType.IsCollection != type.IsCollection || explicitType.IsOptional != type.IsOptional))
        {
            context.Error(DiagnosticCodes.InvalidResponseShape, "An explicit response field type must match its source's declared type, collection shape and optionality.", explicitType.Location);
        }
    }

    sealed class GeneratedDeclarationWalker(HashSet<PropertySyntax> commandProperties, ParserContext context) : ScreenplaySyntaxWalker
    {
        public override void VisitProperty(PropertySyntax syntax)
        {
            if (syntax.IsGenerated && !commandProperties.Contains(syntax)) context.Error(DiagnosticCodes.GeneratedPropertyOutsideCommand, "Generated properties can only be declared on commands.", syntax.Location);
            base.VisitProperty(syntax);
        }
    }
}

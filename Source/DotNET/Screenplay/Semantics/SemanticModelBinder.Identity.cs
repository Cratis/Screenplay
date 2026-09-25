// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticProperty BindProperty(SemanticAddress owner, PropertySyntax property, bool isIdentifier)
        {
            var address = SemanticAddress.ForProperty(owner, property.Name);
            var id = Resolve(address, property.Location);
            return new(id, property.Name, BindTypeReference(property.Type), isIdentifier);
        }

        SemanticProperty BindEventProperty(SemanticAddress owner, EventContractRevision revision, PropertySyntax property)
        {
            var address = SemanticAddress.ForEventProperty(owner, revision, property.Name);
            return new(Resolve(address, property.Location), property.Name, BindTypeReference(property.Type), false);
        }

        SemanticTypeReference BindTypeReference(TypeRefSyntax type) => type.Name switch
        {
            "Uuid" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid, type.IsCollection, type.IsOptional),
            "String" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, type.IsCollection, type.IsOptional),
            "Int" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber, type.IsCollection, type.IsOptional),
            "Decimal" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DecimalNumber, type.IsCollection, type.IsOptional),
            "Bool" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean, type.IsCollection, type.IsOptional),
            "Date" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Date, type.IsCollection, type.IsOptional),
            "DateTime" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime, type.IsCollection, type.IsOptional),
            _ when _concepts.TryGetValue(type.Name, out var concept) => SemanticTypeReference.ForConcept(concept.Id, type.IsCollection, type.IsOptional),
            _ when _types.TryGetValue(type.Name, out var composite) => SemanticTypeReference.ForCompositeType(composite.Id, type.IsCollection, type.IsOptional),
            _ => throw new InvalidSemanticContract($"Type reference '{type.Name}' is unresolved during semantic binding.")
        };

        string ShortName(string value) => value[(value.LastIndexOf('.') + 1)..];

        SemanticPrimitiveType Primitive(string value) => value switch
        {
            "Uuid" => SemanticPrimitiveType.Uuid,
            "String" => SemanticPrimitiveType.Text,
            "Int" => SemanticPrimitiveType.WholeNumber,
            "Decimal" => SemanticPrimitiveType.DecimalNumber,
            "Bool" => SemanticPrimitiveType.Boolean,
            "Date" => SemanticPrimitiveType.Date,
            "DateTime" => SemanticPrimitiveType.DateTime,
            _ => throw new InvalidSemanticContract($"Primitive type '{value}' is unsupported during semantic binding.")
        };

        SemanticId Resolve(SemanticAddress address, SourceLocation location)
        {
            var assignment = documents.IdentityCatalog.ResolveSemanticAssignment(address);
            Map(assignment.Id, assignment.Origin, location);
            return assignment.Id;
        }

        SemanticId ResolveSlice(
            SemanticAddress address,
            SourceLocation location,
            SourceLocation? descriptionLocation,
            int? descriptionRawLength)
        {
            var assignment = documents.IdentityCatalog.ResolveSemanticAssignment(address);
            Map(assignment.Id, assignment.Origin, location);
            if (descriptionLocation is { } resolvedLocation && descriptionRawLength is { } resolvedLength)
            {
                Map(assignment.Id, assignment.Origin, resolvedLocation, SemanticSourceMapRole.Description, resolvedLength);
            }

            return assignment.Id;
        }

        void Map(
            SemanticId id,
            SemanticIdentityOrigin origin,
            SourceLocation location,
            SemanticSourceMapRole role = SemanticSourceMapRole.Declaration,
            int length = 0)
        {
            if (DocumentAt(location) is not { } document)
            {
                return;
            }

            try
            {
                var offset = OffsetAt(document.Text, location);
                var span = SemanticSourceSpan.Create(document.Id, offset, length, location.Line, location.Column, location.Line, location.Column + length);
                _sourceMapEntries.Add(new(id, span, origin) { Role = role });
            }
            catch (InvalidSemanticContract exception)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, exception.Message, location);
            }
        }

        SemanticSourceDocument? DocumentAt(SourceLocation location)
        {
            if (location.Path is null && documents.Documents.Length == 1)
            {
                return documents.Documents[0];
            }

            var document = documents.Documents.FirstOrDefault(value =>
                string.Equals(value.DisplayPath, location.Path, StringComparison.OrdinalIgnoreCase));
            if (document is null)
            {
                Error(
                    DiagnosticCodes.UnknownSemanticSourceDocument,
                    $"Source location path '{location.Path ?? "<none>"}' does not identify one supplied semantic document.",
                    location);
            }

            return document;
        }

        int OffsetAt(string text, SourceLocation location)
        {
            var line = 1;
            var offset = 0;
            while (line < location.Line && offset < text.Length)
            {
                if (text[offset] == '\r')
                {
                    offset++;
                    if (offset < text.Length && text[offset] == '\n')
                    {
                        offset++;
                    }

                    line++;
                }
                else if (text[offset++] == '\n')
                {
                    line++;
                }
            }

            if (line != location.Line)
            {
                throw new InvalidSemanticContract("A semantic syntax location is outside its source document.");
            }

            var lineStart = offset;
            while (offset < text.Length && text[offset] is not ('\r' or '\n'))
            {
                offset++;
            }

            var lineLength = offset - lineStart;
            if (location.Column > lineLength + 1)
            {
                throw new InvalidSemanticContract("A semantic syntax column is outside its source line.");
            }

            return lineStart + location.Column - 1;
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly List<SemanticImplementationRequirement> _implementationRequirements = [];

        internal ImmutableArray<SemanticImplementationRequirement> ImplementationRequirements =>
            [.. _implementationRequirements.OrderBy(value => value.Source.Span.Document.ToString(), StringComparer.Ordinal)
                .ThenBy(value => value.Source.Span.Start)
                .ThenBy(value => value.Role)
                .ThenBy(value => value.Member, StringComparer.Ordinal)];

        void RequireImplementation(
            SemanticImplementationRole role,
            SemanticAddress owner,
            FileReferenceSyntax? file,
            CodeBlockSyntax? code,
            string? member = null)
        {
            if (file is null && code is null)
            {
                return;
            }

            var location = code?.Location ?? file!.Location;
            var document = DocumentAt(location);
            if (document is null)
            {
                return;
            }

            var assignment = documents.IdentityCatalog.ResolveSemanticAssignment(owner);
            var offset = OffsetAt(document.Text, location);
            var span = SemanticSourceSpan.Create(document.Id, offset, 0, location.Line, location.Column, location.Line, location.Column);
            var source = new SemanticSourceMapEntry(assignment.Id, span, assignment.Origin);
            var content = code?.Code ?? file!.Path;
            var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
            _implementationRequirements.Add(new(role, owner, member, code?.Language, file?.Path, hash, source));
        }
    }
}

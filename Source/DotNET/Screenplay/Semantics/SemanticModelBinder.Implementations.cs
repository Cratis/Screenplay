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

        static string Hash(string content) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

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
            var resolved = code is not null || (file is not null && documents.AttachmentContents.ContainsKey(file.Path));
            var content = code?.Code ?? (file is not null && documents.AttachmentContents.TryGetValue(file.Path, out var supplied) ? supplied : null);
            var hash = resolved ? Hash(content!) : string.Empty;

            // Distinct named members are order-independent; repeated identical members have no semantic
            // discriminator, so only those repetitions receive an ordinal among identical attachments.
            var repeated = _implementationRequirements.Count(value => value.Role == role && Equals(value.Owner, owner) &&
                (value.Member == member || value.Member?.StartsWith($"{member}#", StringComparison.Ordinal) == true));
            var distinctMember = repeated == 0 ? member : $"{member}#{repeated}";
            var identity = Hash($"{assignment.Id}|{role}|{distinctMember?.Length ?? 0}:{distinctMember}");
            _implementationRequirements.Add(new(role, owner, distinctMember, code?.Language, file?.Path, hash, source)
            {
                RequirementId = identity,
                ContextVersion = 1,
                ResultVersion = 1,
                RequiredCapability = role == SemanticImplementationRole.ReducerTransition ? "pure" : "provider-defined",
                AttachmentResolution = resolved ? SemanticAttachmentResolution.Resolved : SemanticAttachmentResolution.UnresolvedFile
            });
        }
    }
}

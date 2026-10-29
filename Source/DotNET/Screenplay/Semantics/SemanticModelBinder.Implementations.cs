// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly List<SemanticImplementationRequirement> _implementationRequirements = [];

        internal ImmutableArray<SemanticImplementationRequirement> ImplementationRequirements
        {
            get
            {
                var ambiguous = _implementationRequirements.Where(value => value.Role == SemanticImplementationRole.CommandHandler)
                    .GroupBy(value => value.RequirementId, StringComparer.Ordinal).Where(group => group.Count() > 1)
                    .Select(group => group.Key).ToHashSet(StringComparer.Ordinal);

                return [.. _implementationRequirements.Select(value => value with { IsAmbiguous = value.Role == SemanticImplementationRole.CommandHandler && ambiguous.Contains(value.RequirementId) })
                    .OrderBy(value => value.Source.Span.Document.ToString(), StringComparer.Ordinal)
                    .ThenBy(value => value.Source.Span.Start)
                    .ThenBy(value => value.Role)
                    .ThenBy(value => value.Member, StringComparer.Ordinal)];
            }
        }

        static bool IsImperativeValidation(ValidateSyntax validation) => validation is CodeValidateSyntax or FileValidateSyntax;

        static string Hash(string content) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

        static SemanticImplementationBodySpan FileBodySpan(string content)
        {
            // Keep hashing the supplied content, but map a UTF-8 BOM to the editor's BOM-less text.
            var text = content.AsSpan(content.StartsWith('\uFEFF') ? 1 : 0);
            var line = 1;
            var column = 1;
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '\r')
                {
                    if (index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }

                    line++;
                    column = 1;
                }
                else if (text[index] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return new(0, text.Length, 1, 1, line, column);
        }

        SemanticImplementationRequirement? RequirePendingImplementation(SemanticAddress owner, HandlerSyntax handler)
        {
            var location = handler.Location;
            var document = DocumentAt(location);
            if (document is null) return null;
            var assignment = documents.IdentityCatalog.ResolveSemanticAssignment(owner);
            var offset = OffsetAt(document.Text, location);
            var span = SemanticSourceSpan.Create(document.Id, offset, 0, location.Line, location.Column, location.Line, location.Column);
            var source = new SemanticSourceMapEntry(assignment.Id, span, assignment.Origin);
            var requirement = new SemanticImplementationRequirement(SemanticImplementationRole.CommandHandler, owner, null, null, null, string.Empty, source)
            {
                RequirementId = ImplementationRequirementIdentity.Create(assignment.Id, SemanticImplementationRole.CommandHandler, null),
                ContextVersion = 1,
                ResultVersion = 1,
                RequiredCapability = "provider-defined",
                AttachmentResolution = SemanticAttachmentResolution.Pending,
                Hints = [.. handler.Implementation!.Hints.Select(hint => hint.Text)],
                IsProvisional = assignment.Origin == SemanticIdentityOrigin.LegacyBootstrap
            };
            _implementationRequirements.Add(requirement);
            return requirement;
        }

        SemanticImplementationRequirement? RequireValidationImplementation(SemanticImplementationRole role, SemanticAddress owner, ValidateSyntax validation, string member) => validation switch
        {
            CodeValidateSyntax code => RequireImplementation(role, owner, null, code.Code, member),
            FileValidateSyntax file => RequireImplementation(role, owner, file.File, null, member),
            _ => null
        };

        SemanticImplementationRequirement? RequireImplementation(
            SemanticImplementationRole role,
            SemanticAddress owner,
            FileReferenceSyntax? file,
            CodeBlockSyntax? code,
            string? member = null)
        {
            if (file is null && code is null)
            {
                return null;
            }

            var location = code?.Location ?? file!.Location;
            var document = DocumentAt(location);
            if (document is null)
            {
                return null;
            }

            var assignment = documents.IdentityCatalog.ResolveSemanticAssignment(owner);
            var offset = OffsetAt(document.Text, location);
            var span = SemanticSourceSpan.Create(document.Id, offset, 0, location.Line, location.Column, location.Line, location.Column);
            var source = new SemanticSourceMapEntry(assignment.Id, span, assignment.Origin);
            var key = file is not null && AttachmentFiles.TryNormalize(file.Path, out var normalized, out _) ? normalized : null;
            var content = code?.Code ?? (key is not null && documents.AttachmentContents.TryGetValue(key, out var supplied) ? supplied : null);
            var resolved = DescribedCommandAdmission.HasImplementationContent(role, content);
            var hash = resolved ? Hash(content!) : string.Empty;
            SemanticImplementationBodySpan? bodySpan = null;
            if (code?.BodyStart is { } bodyStart && code.BodyEnd is { } bodyEnd)
            {
                bodySpan = new(code.BodyStartOffset, code.BodyEndOffset, bodyStart.Line, bodyStart.Column, bodyEnd.Line, bodyEnd.Column);
            }
            else if (code is null && resolved)
            {
                bodySpan = FileBodySpan(content!);
            }

            // Distinct named members are order-independent; repeated identical members have no semantic
            // discriminator, so only those repetitions receive an ordinal among identical attachments.
            var repeated = _implementationRequirements.Count(value => value.Role == role && Equals(value.Owner, owner) &&
                (value.Member == member || value.Member?.StartsWith($"{member}#", StringComparison.Ordinal) == true));
            var distinctMember = repeated == 0 ? member : $"{member}#{repeated}";
            var identity = ImplementationRequirementIdentity.Create(assignment.Id, role, distinctMember);
            var language = DescribedCommandAdmission.ImplementationLanguage(role, file, code);
            var requirement = new SemanticImplementationRequirement(role, owner, distinctMember, language, file?.Path, hash, source)
            {
                RequirementId = identity,
                ContextVersion = 1,
                ResultVersion = 1,
                RequiredCapability = role is SemanticImplementationRole.ReducerTransition or SemanticImplementationRole.RulePredicate or
                    SemanticImplementationRole.CommandValidation or SemanticImplementationRole.ConceptValidation or SemanticImplementationRole.PolicyPredicate ? "pure" : "provider-defined",
                AttachmentResolution = resolved ? SemanticAttachmentResolution.Resolved : SemanticAttachmentResolution.UnresolvedFile,
                BodySpan = bodySpan,
                BodyLines = code is not null && bodySpan.HasValue ? code.BodyLines : []
            };
            _implementationRequirements.Add(requirement);
            return requirement;
        }
    }
}

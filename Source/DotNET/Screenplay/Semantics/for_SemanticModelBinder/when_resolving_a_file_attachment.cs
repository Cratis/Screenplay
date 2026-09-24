// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_resolving_a_file_attachment : given.a_semantic_binder
{
    const string Source = "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n          file Handlers/Register.cs";

    SemanticImplementationRequirement _first;
    SemanticImplementationRequirement _moved;
    SemanticImplementationRequirement _changed;
    SemanticImplementationRequirement _unresolved;
    SemanticImplementationRequirement _multiline;

    void Because()
    {
        _first = BindFile("Handlers/Register.cs", "return 1;", "old/application.play");
        _moved = BindFile("Handlers/Moved.cs", "return 1;", "new/application.play");
        _changed = BindFile("Handlers/Register.cs", "return 2;", "old/application.play");
        _unresolved = BindFile("Handlers/Register.cs", null, "old/application.play");
        _multiline = BindFile("Handlers/Register.cs", "a\r\nb", "old/application.play");
    }

    [Fact] void should_keep_the_requirement_identity_across_document_and_attachment_moves() => _first.RequirementId.ShouldEqual(_moved.RequirementId);
    [Fact] void should_keep_the_content_revision_across_attachment_moves() => _first.ContentHash.ShouldEqual(_moved.ContentHash);
    [Fact] void should_change_the_revision_when_the_supplied_content_changes() => _first.ContentHash.ShouldNotEqual(_changed.ContentHash);
    [Fact] void should_hash_the_supplied_content() => _first.ContentHash.ShouldEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("return 1;"))).ToLowerInvariant());
    [Fact] void should_not_hash_a_path_when_contents_are_missing() => _unresolved.ContentHash.ShouldBeEmpty();
    [Fact] void should_type_the_missing_attachment() => _unresolved.AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.UnresolvedFile);
    [Fact] void should_type_the_resolved_attachment() => _first.AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
    [Fact] void should_map_the_resolved_file_from_its_first_character() => _first.BodySpan!.Value.ShouldEqual(new(0, "return 1;".Length, 1, 1, 1, 10));
    [Fact] void should_not_claim_a_span_for_an_unresolved_file() => _unresolved.BodySpan.HasValue.ShouldBeFalse();
    [Fact] void should_not_rebase_file_lines_on_the_play_document() => _first.BodyLines.IsEmpty.ShouldBeTrue();
    [Fact] void should_map_crlf_in_a_resolved_file() => _multiline.BodySpan!.Value.ShouldEqual(new(0, 4, 1, 1, 2, 2));

    SemanticImplementationRequirement BindFile(string path, string? content, string displayPath)
    {
        var source = Source.Replace("Handlers/Register.cs", path, StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application-document"), "application-document", displayPath, source);
        var contents = content is null ? null : ImmutableDictionary.Create<string, string>(StringComparer.Ordinal).Add(path, content);
        var set = SemanticDocumentSet.Create([document], catalog, contents);
        var syntax = new ScreenplayCompiler().Parse(source, displayPath).Value!;
        return _binder.Bind("Projects", syntax, set).ImplementationRequirements.Single();
    }
}

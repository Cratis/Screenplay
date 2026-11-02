// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_AttachmentFiles;

public class when_reloading_a_reference_manifest : Specification
{
    string _root = null!;
    ImmutableArray<FileReferenceSyntax> _references;
    AttachmentFileResult _first = null!;
    AttachmentFileResult _denied = null!;
    int _checks;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "screenplay-manifest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "Handler.cs"), "implementation");
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n          file Handler.cs";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "application.play", source);
        _references = AttachmentFiles.References([document]);
    }

    void Because()
    {
        _first = AttachmentFiles.LoadReferences(_root, _references, _ =>
        {
            _checks++;
            return true;
        });
        _denied = AttachmentFiles.LoadReferences(_root, _references, _ =>
        {
            _checks++;
            throw new UnauthorizedAccessException();
        });
    }

    [Fact] void should_load_the_initial_attachment() => _first.Contents["Handler.cs"].ShouldEqual("implementation");
    [Fact] void should_verify_file_access_on_every_load() => _checks.ShouldEqual(2);
    [Fact] void should_not_reuse_a_previous_success_when_access_is_denied() => _denied.Contents.ShouldBeEmpty();
    [Fact] void should_keep_the_denied_access_diagnostic() => _denied.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AttachmentUnreadable);

    void Destroy() => Directory.Delete(_root, true);
}

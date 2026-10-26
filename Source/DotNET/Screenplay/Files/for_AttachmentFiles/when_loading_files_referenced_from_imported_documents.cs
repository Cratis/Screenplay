// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Files.for_AttachmentFiles;

public class when_loading_files_referenced_from_imported_documents : Specification
{
    string _root = null!;
    AttachmentFileResult _result = null!;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "screenplay-attachments-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "projects"));
        File.WriteAllText(Path.Combine(_root, "projects", "Reducer.cs"), "return null;");
    }

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var documents = new (string Path, string Text)[]
        {
            ("application.play", "domain Projects\n"),
            ("projects/projects.play", "module Projects\n  import \"registration.play\"\n"),
            ("projects/registration.play", "feature Registration\n  import \"register.play\"\n"),
            ("projects/register.play", "slice StateChange Register\n  command Register\n    handler\n      file projects/Reducer.cs\n")
        };
        _result = AttachmentFiles.Load(_root, [.. documents.Select((document, index) => SemanticSourceDocument.Create(catalog.ResolveDocument($"file-{index}"), $"file-{index}", document.Path, document.Text))]);
    }

    void Destroy() => Directory.Delete(_root, true);

    [Fact] void should_load_the_file_the_imported_slice_references() => _result.Contents["projects/Reducer.cs"].ShouldEqual("return null;");
    [Fact] void should_not_warn() => _result.Diagnostics.ShouldBeEmpty();
}

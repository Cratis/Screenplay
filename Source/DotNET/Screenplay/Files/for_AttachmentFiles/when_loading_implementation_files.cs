// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Files.for_AttachmentFiles;

public class when_loading_implementation_files : Specification
{
    string _root = null!;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "screenplay-attachments-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    void should_hash_the_contents_and_keep_the_identity_when_moved()
    {
        Write("First.cs", "return 1;");
        var first = Bind("First.cs");
        Write("Moved.cs", "return 1;");
        var moved = Bind("Moved.cs");
        Write("First.cs", "return 2;");
        var changed = Bind("First.cs");
        first.Requirement.ContentHash.ShouldEqual(moved.Requirement.ContentHash);
        first.Requirement.RequirementId.ShouldEqual(moved.Requirement.RequirementId);
        first.Requirement.ContentHash.ShouldNotEqual(changed.Requirement.ContentHash);
        first.Requirement.ContentHash.ShouldEqual(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("return 1;"))).ToLowerInvariant());
    }

    [Fact]
    void should_normalize_current_directory_and_backslash()
    {
        Write("X.cs", "same");
        Bind("./X.cs").Requirement.ContentHash.ShouldEqual(Bind("X.cs").Requirement.ContentHash);
        Bind(@".\X.cs").Requirement.ContentHash.ShouldEqual(Bind("X.cs").Requirement.ContentHash);
    }

    [Theory]
    [InlineData("/tmp/elsewhere.cs", DiagnosticCodes.AttachmentPathRefused)]
    [InlineData("C:\\outside.cs", DiagnosticCodes.AttachmentPathRefused)]
    [InlineData(@"\\server\share.cs", DiagnosticCodes.AttachmentPathRefused)]
    [InlineData("../outside.cs", DiagnosticCodes.AttachmentPathRefused)]
    [InlineData("missing.cs", DiagnosticCodes.AttachmentMissing)]
    void should_refuse_inaccessible_paths(string path, string code) => AssertRefused(path, code);

    [Fact]
    void should_refuse_an_oversized_file()
    {
        File.WriteAllBytes(Path.Combine(_root, "huge.cs"), new byte[AttachmentFiles.MaximumFileBytes + 1]);
        AssertRefused("huge.cs", DiagnosticCodes.AttachmentTooLarge);
    }

    [Fact]
    void should_apply_the_aggregate_size_limit()
    {
        var source = new StringBuilder("module Projects\n  feature Registration\n    slice StateChange Register\n");
        for (var index = 0; index < 5; index++)
        {
            File.WriteAllBytes(Path.Combine(_root, $"attachment{index}.cs"), new byte[AttachmentFiles.MaximumFileBytes]);
            source.Append($"      command Command{index}\n        handler\n          file attachment{index}.cs\n");
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "application.play", source.ToString());
        var loaded = AttachmentFiles.Load(_root, [document]);
        loaded.Contents.Count.ShouldEqual(4);
        loaded.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AttachmentTooLarge);
    }

    [Fact]
    void should_not_read_declaration_only_references()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "application.play", "concept ProjectId : Uuid\n  file missing.cs");
        var loaded = AttachmentFiles.Load(_root, [document]);
        loaded.Contents.ShouldBeEmpty();
        loaded.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    void should_refuse_a_symbolic_link_to_a_file()
    {
        Write("real.cs", "text");
        try
        {
            File.CreateSymbolicLink(Path.Combine(_root, "linked.cs"), Path.Combine(_root, "real.cs"));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            return;
        }

        AssertRefused("linked.cs", DiagnosticCodes.AttachmentLinkRefused);
    }

    [Fact]
    void should_refuse_a_symbolic_link_to_a_directory()
    {
        Directory.CreateDirectory(Path.Combine(_root, "real"));
        Write("real/file.cs", "text");
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(_root, "linked"), Path.Combine(_root, "real"));
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            return;
        }

        AssertRefused("linked/file.cs", DiagnosticCodes.AttachmentLinkRefused);
    }

    [Fact]
    void should_keep_workspace_bytes_and_revision_when_supplying_contents()
    {
        Write("X.cs", "text");
        var source = Source("X.cs");
        var document = WorkspaceDocument.Create("source", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var before = ScreenplayWorkspaceSerializer.Serialize(workspace);
        var loaded = AttachmentFiles.Load(_root, [SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, source)]);
        var supplied = workspace.WithAttachmentContents(loaded.Contents, loaded.Diagnostics);
        workspace.Compilation.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.UnresolvedFile);
        supplied.Compilation.ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
        supplied.Revision.ShouldEqual(workspace.Revision);
        ScreenplayWorkspaceSerializer.Serialize(supplied).ShouldContainOnly(before);
    }

    void AssertRefused(string path, string code)
    {
        var result = Bind(path);
        result.Requirement.AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.UnresolvedFile);
        result.Requirement.ContentHash.ShouldBeEmpty();
        result.Diagnostics.Single(diagnostic => diagnostic.Code == code).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

    (SemanticImplementationRequirement Requirement, System.Collections.Immutable.ImmutableArray<Diagnostic> Diagnostics) Bind(string path)
    {
        var source = Source(path);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "application.play", source);
        var loaded = AttachmentFiles.Load(_root, [document]);
        var syntax = new ScreenplayCompiler().Parse(source, document.DisplayPath).Value!;
        var result = new SemanticModelBinder().Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog, loaded.Contents));
        return (result.ImplementationRequirements.Single(), loaded.Diagnostics);
    }

    static string Source(string path) => $"module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n          file {path}";

    void Write(string path, string text)
    {
        var full = Path.Combine(_root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    void Destroy() => Directory.Delete(_root, true);
}

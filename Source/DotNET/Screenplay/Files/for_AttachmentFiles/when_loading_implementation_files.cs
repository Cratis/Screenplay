// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
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

    [Theory]
    [InlineData("CON.cs")]
    [InlineData("aux")]
    [InlineData("folder/COM9.txt")]
    [InlineData("Lpt1")]
    void should_refuse_reserved_device_names(string path) => AssertRefused(path, DiagnosticCodes.AttachmentPathRefused);

    [Fact]
    void should_refuse_a_missing_model_root()
    {
        Directory.Delete(_root);
        Bind("missing.cs").Diagnostics.ShouldNotBeEmpty();
        Directory.CreateDirectory(_root);
    }

    [Fact]
    async Task should_refuse_a_fifo_without_blocking()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var process = Process.Start(new ProcessStartInfo("mkfifo", Path.Combine(_root, "pipe.cs")) { UseShellExecute = false });
        if (process is null)
        {
            return;
        }

        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        if (process.ExitCode != 0)
        {
            return;
        }

        var result = await Task.Run(() => Bind("pipe.cs")).WaitAsync(TimeSpan.FromSeconds(5));
        result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.AttachmentUnreadable).Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

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

    [Theory]
    [InlineData(SemanticImplementationRole.CommandHandler, "      command Register\n        handler\n          file Role.cs")]
    [InlineData(SemanticImplementationRole.RulePredicate, "      command Register\n        code String\n        validate\n          code rule IsValid message \"invalid\"\n            file Role.cs")]
    [InlineData(SemanticImplementationRole.QueryPerformer, "      readmodel Item\n        id Uuid\n      query GetItem => Item\n        performer\n          file Role.cs")]
    [InlineData(SemanticImplementationRole.ConstraintPredicate, "      constraint UniqueItem\n        file Role.cs")]
    [InlineData(SemanticImplementationRole.ReactionEffect, "      event Registered\n        id Uuid\n      reaction Notify\n        when Registered\n          file Role.cs")]
    [InlineData(SemanticImplementationRole.ReducerTransition, "      event Registered\n        id Uuid\n      readmodel Item\n        id Uuid\n      reducer ItemReducer => Item\n        on Registered\n          file Role.cs")]
    void should_resolve_each_slice_implementation_role(SemanticImplementationRole role, string body)
    {
        Write("Role.cs", "implementation");
        var source = "module Projects\n  feature Registration\n    slice StateChange Register\n" + body;
        ResolvedRole(source, role);
    }

    [Fact]
    void should_resolve_policy_implementation()
    {
        Write("Role.cs", "implementation");
        ResolvedRole("policy IsAuthorized\n  file Role.cs", SemanticImplementationRole.PolicyPredicate);
    }

    void ResolvedRole(string source, SemanticImplementationRole role)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "application.play", source);
        var loaded = AttachmentFiles.Load(_root, [document]);
        var syntax = new ScreenplayCompiler().Parse(source, document.DisplayPath).Value!;
        var bound = new SemanticModelBinder().Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog, loaded.Contents));
        bound.ImplementationRequirements.Single(requirement => requirement.Role == role).AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
        loaded.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    void should_normalize_host_keys_and_refuse_conflicting_aliases()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = Source("Role.cs");
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "application.play", source);
        var contents = System.Collections.Immutable.ImmutableDictionary<string, string>.Empty.Add("./Role.cs", "implementation");
        var syntax = new ScreenplayCompiler().Parse(source, document.DisplayPath).Value!;
        new SemanticModelBinder().Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog, contents))
            .ImplementationRequirements.Single().AttachmentResolution.ShouldEqual(SemanticAttachmentResolution.Resolved);
        Assert.Throws<InvalidSemanticContract>(() => SemanticDocumentSet.Create([document], catalog, contents.Add("Role.cs", "different")));
    }

    [Fact]
    void should_not_carry_old_attachment_warnings_into_a_transaction()
    {
        const string before = "module Projects\n  feature Registration\n    slice StateView Register\n      event Registered\n        id Uuid\n      readmodel Item\n        id Uuid\n      reducer ItemReducer => Item\n        on Registered\n          file missing.cs";
        var document = WorkspaceDocument.Create("source", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(before));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var loaded = AttachmentFiles.Load(_root, [SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, before)]);
        workspace = workspace.WithAttachmentContents(loaded.Contents, loaded.Diagnostics);
        workspace.AttachmentDiagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AttachmentMissing);
        var replacement = new ScreenplayCompiler().Parse(before.Replace("missing.cs", "moved.cs", StringComparison.Ordinal), "application.play").Value!;
        var result = workspace.ProposeAuthoring(new WorkspaceAuthoringRequest
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents = [new ReplaceWorkspaceSyntaxDocument(document.Id, replacement)]
        });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        result.Workspace!.AttachmentDiagnostics.ShouldBeEmpty();
        result.Workspace.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AttachmentMissing).ShouldBeFalse();
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

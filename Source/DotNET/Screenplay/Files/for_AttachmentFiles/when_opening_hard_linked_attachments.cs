// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Files.for_AttachmentFiles;

[PerFactSpecificationLifecycle]
public partial class when_opening_hard_linked_attachments : Specification
{
    string _root = null!;
    string _outside = null!;

    void Establish()
    {
        if (OperatingSystem.IsWindows()) return;
        _root = Path.Combine(Path.GetTempPath(), "screenplay-attachments-" + Guid.NewGuid().ToString("N"));
        _outside = Path.Combine(Path.GetTempPath(), "screenplay-attachments-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_outside);
        File.WriteAllText(Path.Combine(_root, "handler.cs"), "regular contents");
        File.WriteAllText(Path.Combine(_outside, "handler.cs"), "other contents");
    }

    [Fact]
    void should_refuse_an_attachment_with_more_than_one_hard_link()
    {
        if (OperatingSystem.IsWindows()) return;
        var result = Load(path =>
        {
            File.Delete(path);
            var created = Link(Path.Combine(_outside, "handler.cs"), path);
            Assert.True(created == 0, $"Hard link creation failed with errno {Marshal.GetLastPInvokeError()}.");
        });
        result.Contents.ShouldBeEmpty();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.AttachmentLinkRefused);
        diagnostic.Message.ShouldEqual("File attachment 'handler.cs' has more than one hard link.");
    }

    [Fact]
    void should_load_an_attachment_with_one_link()
    {
        if (OperatingSystem.IsWindows()) return;
        var result = Load();
        result.Contents["handler.cs"].ShouldEqual("regular contents");
        result.Diagnostics.ShouldBeEmpty();
    }

    AttachmentFileResult Load(Action<string>? beforeOpen = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(
            catalog.ResolveDocument("source"),
            "source",
            "application.play",
            "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n          file handler.cs");
        return AttachmentFiles.Load(_root, [document], _ => true, beforeOpen);
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("libc", EntryPoint = "link", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Link(string existingPath, string path);

    void Destroy()
    {
        if (OperatingSystem.IsWindows()) return;
        Directory.Delete(_root, true);
        Directory.Delete(_outside, true);
    }
}

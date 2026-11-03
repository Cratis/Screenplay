// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Files.for_AttachmentFiles;

public partial class when_opening_changed_files : Specification
{
    string _root = null!;
    string _outside = null!;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "screenplay-attachments-" + Guid.NewGuid().ToString("N"));
        _outside = Path.Combine(Path.GetTempPath(), "screenplay-attachments-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "nested", "implementation"));
        Directory.CreateDirectory(_outside);
        File.WriteAllText(Path.Combine(_root, "nested", "implementation", "handler.cs"), "regular contents");
        File.WriteAllText(Path.Combine(_outside, "handler.cs"), "other contents");
    }

    [Fact]
    void should_load_an_ordinary_regular_file_unchanged()
    {
        var result = AttachmentFiles.Load(_root, [Document("nested/implementation/handler.cs")]);
        result.Contents["nested/implementation/handler.cs"].ShouldEqual("regular contents");
        result.Diagnostics.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_not_follow_a_link_in_the_final_component(bool duringLoad)
    {
        if (OperatingSystem.IsWindows()) return;
        var path = Path.Combine(_root, "nested", "implementation", "handler.cs");
        void Replace(string _)
        {
            File.Delete(path);
            File.CreateSymbolicLink(path, Path.Combine(_outside, "handler.cs"));
        }

        if (!duringLoad) Replace(path);
        var result = AttachmentFiles.Load(_root, [Document("nested/implementation/handler.cs")], _ => true, duringLoad ? Replace : null);
        AssertRefused(result, duringLoad ? DiagnosticCodes.AttachmentUnreadable : DiagnosticCodes.AttachmentLinkRefused);
        if (duringLoad) result.Diagnostics.Single().Message.ShouldContain("cannot be opened");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_not_follow_a_link_in_a_directory_component(bool duringLoad)
    {
        if (OperatingSystem.IsWindows()) return;
        var path = Path.Combine(_root, "nested", "implementation");
        void Replace(string _)
        {
            Directory.Move(path, path + ".saved");
            Directory.CreateSymbolicLink(path, _outside);
        }

        if (!duringLoad) Replace(path);
        var result = AttachmentFiles.Load(_root, [Document("nested/implementation/handler.cs")], _ => true, duringLoad ? Replace : null);
        AssertRefused(result, duringLoad ? DiagnosticCodes.AttachmentUnreadable : DiagnosticCodes.AttachmentLinkRefused);
        if (duringLoad) result.Diagnostics.Single().Message.ShouldContain("cannot be opened");
    }

    [Fact]
    void should_not_read_outside_contents_when_the_model_root_is_replaced()
    {
        if (OperatingSystem.IsWindows()) return;
        Directory.CreateDirectory(Path.Combine(_outside, "nested", "implementation"));
        File.WriteAllText(Path.Combine(_outside, "nested", "implementation", "handler.cs"), "other contents");
        var result = AttachmentFiles.Load(_root, [Document("nested/implementation/handler.cs")], _ => true, _ =>
        {
            Directory.Move(_root, _root + ".saved");
            Directory.CreateSymbolicLink(_root, _outside);
        });
        if (result.Contents.TryGetValue("nested/implementation/handler.cs", out var content))
        {
            content.ShouldEqual("regular contents");
            result.Diagnostics.ShouldBeEmpty();
        }
        else
        {
            AssertRefused(result, DiagnosticCodes.AttachmentUnreadable);
        }
    }

    [Fact]
    void should_report_a_file_removed_before_opening()
    {
        if (OperatingSystem.IsWindows()) return;
        var result = AttachmentFiles.Load(_root, [Document("nested/implementation/handler.cs")], _ => true, File.Delete);
        AssertRefused(result, DiagnosticCodes.AttachmentUnreadable);
        result.Diagnostics.Single().Message.ShouldContain("cannot be opened");
    }

    [Fact]
    async Task should_refuse_a_file_changed_to_a_fifo_without_blocking()
    {
        if (OperatingSystem.IsWindows()) return;
        var path = Path.Combine(_root, "nested", "implementation", "handler.cs");
        var result = await Task.Run(
            () => AttachmentFiles.Load(_root, [Document("nested/implementation/handler.cs")], _ => true, _ =>
            {
                File.Delete(path);
                var created = MkFifo(path, 0x180);
                Assert.True(created == 0, $"mkfifo failed with errno {Marshal.GetLastPInvokeError()}.");
            }),
            TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        AssertRefused(result, DiagnosticCodes.AttachmentUnreadable);
        result.Diagnostics.Single().Message.ShouldContain("is not a regular file");
    }

    [Fact]
    void should_check_the_size_of_the_opened_file()
    {
        if (OperatingSystem.IsWindows()) return;
        var result = AttachmentFiles.Load(
            _root,
            [Document("nested/implementation/handler.cs")],
            _ => true,
            path => File.WriteAllBytes(path, new byte[AttachmentFiles.MaximumFileBytes + 1]));
        AssertRefused(result, DiagnosticCodes.AttachmentTooLarge);
    }

    static void AssertRefused(AttachmentFileResult result, string code)
    {
        result.Contents.ShouldBeEmpty();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(code);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

    static SemanticSourceDocument Document(string path)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        return SemanticSourceDocument.Create(
            catalog.ResolveDocument("source"),
            "source",
            "application.play",
            $"module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n          file {path}");
    }

    [LibraryImport("libc", EntryPoint = "mkfifo", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static partial int MkFifo(string path, uint mode);

    void Destroy()
    {
        Directory.Delete(_root, true);
        if (Directory.Exists(_root + ".saved")) Directory.Delete(_root + ".saved", true);
        Directory.Delete(_outside, true);
    }
}

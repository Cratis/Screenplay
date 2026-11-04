// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Files.for_AttachmentFiles;

// Every fact changes the folder it loads from, so each one needs its own folder.
[PerFactSpecificationLifecycle]
public partial class when_opening_windows_attachments : Specification
{
    string _root = null!;
    string _outside = null!;

    void Establish()
    {
        if (!OperatingSystem.IsWindows()) return;
        _root = Path.Combine(Path.GetTempPath(), "screenplay-attachments-" + Guid.NewGuid().ToString("N"));
        _outside = Path.Combine(Path.GetTempPath(), "screenplay-attachments-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "nested", "implementation"));
        Directory.CreateDirectory(_outside);
        File.WriteAllText(Path.Combine(_root, "nested", "implementation", "handler.cs"), "regular contents");
        File.WriteAllText(Path.Combine(_outside, "handler.cs"), "outside contents");
    }

    [Fact]
    void should_refuse_an_opened_attachment_resolved_outside_the_model_root()
    {
        if (!OperatingSystem.IsWindows()) return;
        var replaced = false;
        var result = Load(_ =>
        {
            var directory = Path.Combine(_root, "nested", "implementation");
            Directory.Move(directory, directory + ".saved");
            CreateJunction(directory, _outside);
            replaced = true;
        });
        replaced.ShouldBeTrue();
        result.Contents.ShouldBeEmpty();
        result.Contents.Values.ShouldNotContain("outside contents");
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.AttachmentLinkRefused);
        diagnostic.Message.ShouldEqual("File attachment 'nested/implementation/handler.cs' crosses a symbolic link or reparse point.");
    }

    [Fact]
    void should_hold_the_model_root_while_loading_an_attachment()
    {
        if (!OperatingSystem.IsWindows()) return;
        var result = Load(_ => Catch.Exception(() => Directory.Move(_root, _root + ".saved")).ShouldBeOfExactType<IOException>());
        result.Contents["nested/implementation/handler.cs"].ShouldEqual("regular contents");
        result.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    void should_check_the_size_of_the_opened_attachment()
    {
        if (!OperatingSystem.IsWindows()) return;
        var result = Load(path => File.WriteAllBytes(path, new byte[AttachmentFiles.MaximumFileBytes + 1]));
        result.Contents.ShouldBeEmpty();
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AttachmentTooLarge);
    }

    [Fact]
    void should_refuse_an_attachment_with_more_than_one_hard_link()
    {
        if (!OperatingSystem.IsWindows()) return;
        var result = Load(path =>
        {
            File.Delete(path);
            Assert.True(CreateHardLink(path, Path.Combine(_outside, "handler.cs"), 0), $"Hard link creation failed with error {Marshal.GetLastPInvokeError()}.");
        });
        result.Contents.ShouldBeEmpty();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.AttachmentLinkRefused);
        diagnostic.Message.ShouldEqual("File attachment 'nested/implementation/handler.cs' has more than one hard link.");
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [LibraryImport("kernel32.dll", EntryPoint = "CreateHardLinkW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateHardLink(string path, string existingPath, nint securityAttributes);

    static void CreateJunction(string path, string target)
    {
        using var process = Process.Start(new ProcessStartInfo("cmd.exe")
        {
            ArgumentList = { "/c", "mklink", "/J", path, target },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        })!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(10000), "Directory link creation did not finish.");
        Assert.True(process.ExitCode == 0, $"Directory link creation failed: {output} {error}");
    }

    AttachmentFileResult Load(Action<string> beforeOpen)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(
            catalog.ResolveDocument("source"),
            "source",
            "application.play",
            "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n          file nested/implementation/handler.cs");
        return AttachmentFiles.Load(_root, [document], _ => true, beforeOpen);
    }

    void Destroy()
    {
        if (!OperatingSystem.IsWindows()) return;
        var junction = Path.Combine(_root, "nested", "implementation");
        if (Directory.Exists(junction) && File.GetAttributes(junction).HasFlag(FileAttributes.ReparsePoint)) Directory.Delete(junction);
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
        if (Directory.Exists(_root + ".saved")) Directory.Delete(_root + ".saved", true);
        if (Directory.Exists(_outside)) Directory.Delete(_outside, true);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Files.for_AttachmentFiles;

public class when_opening_windows_attachments : Specification
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
        diagnostic.Code.ShouldEqual(DiagnosticCodes.AttachmentPathRefused);
        diagnostic.Message.ShouldEqual("File attachment 'nested/implementation/handler.cs' does not match its name on disk; short (8.3) names and trailing dots or spaces are not supported.");
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
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
        if (Directory.Exists(_root + ".saved")) Directory.Delete(_root + ".saved", true);
        Directory.Delete(_outside, true);
    }
}

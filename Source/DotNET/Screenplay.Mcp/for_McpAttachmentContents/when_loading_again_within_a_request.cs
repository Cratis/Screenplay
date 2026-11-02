// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Mcp.for_McpAttachmentContents;

public class when_loading_again_within_a_request : given.an_attachment_request
{
    AttachmentFileResult _first = null!;
    AttachmentFileResult _changed = null!;
    AttachmentFileResult _changedPath = null!;
    AttachmentFileResult _missing = null!;
    AttachmentFileResult _restored = null!;
    AttachmentFileResult _oversized = null!;
    AttachmentFileResult _linked = null!;
    AttachmentFileResult _denied = null!;
    AttachmentFileResult _afterDenial = null!;
    bool _canDenyAccess;

    void Because()
    {
        _first = Load();
        var path = Path.Combine(RootPath, "Handler.cs");
        File.WriteAllText(path, "second");
        _changed = Load();
        File.WriteAllText(Path.Combine(RootPath, "Changed.cs"), "changed");
        _changedPath = McpAttachmentContents.Load(Root, [Document("application.play", Source("Changed.cs"))], Request);
        File.Delete(path);
        _missing = Load();
        File.WriteAllText(path, "restored");
        _restored = Load();
        File.WriteAllBytes(path, new byte[AttachmentFiles.MaximumFileBytes + 1]);
        _oversized = Load();
        File.WriteAllText(path, "first");
        CheckLinkReplacement(path);
        CheckDeniedAccess(path);
    }

    [Fact]
    void should_read_changed_attachment_contents()
    {
        _first.Contents["Handler.cs"].ShouldEqual("first");
        _changed.Contents["Handler.cs"].ShouldEqual("second");
    }

    [Fact]
    void should_use_changed_source_and_attachment_paths()
    {
        _changedPath.Contents.Keys.ShouldEqual(["Changed.cs"]);
        _changedPath.Contents["Changed.cs"].ShouldEqual("changed");
    }

    [Fact]
    void should_recheck_missing_files_and_their_reappearance()
    {
        Refused(_missing, DiagnosticCodes.AttachmentMissing);
        _restored.Contents["Handler.cs"].ShouldEqual("restored");
    }

    [Fact]
    void should_recheck_file_size() => Refused(_oversized, DiagnosticCodes.AttachmentTooLarge);

    [Fact]
    void should_refuse_a_file_replaced_by_a_link()
    {
        if (OperatingSystem.IsWindows()) return;
        Refused(_linked, DiagnosticCodes.AttachmentLinkRefused);
    }

    [Fact]
    void should_recheck_denied_access()
    {
        if (OperatingSystem.IsWindows()) return;
        Assert.SkipWhen(!_canDenyAccess, "The current reader can bypass Unix permission denial.");
        Refused(_denied, DiagnosticCodes.AttachmentUnreadable);
        _afterDenial.Contents["Handler.cs"].ShouldEqual("first");
    }

    static bool CanRead(string path)
    {
        try
        {
            using var probe = File.OpenRead(path);
            return probe.CanRead;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    static void Refused(AttachmentFileResult result, string code)
    {
        result.Contents.ShouldBeEmpty();
        result.Diagnostics.Single().Code.ShouldEqual(code);
    }

    AttachmentFileResult Load() => McpAttachmentContents.Load(Root, Documents, Request);

    void CheckLinkReplacement(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        var target = Path.Combine(RootPath, "Target.cs");
        File.Move(path, target);
        try
        {
            File.CreateSymbolicLink(path, target);
            _linked = Load();
        }
        finally
        {
            File.Delete(path);
            File.Move(target, path);
        }
    }

    void CheckDeniedAccess(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        var mode = File.GetUnixFileMode(path);
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.None);
            _canDenyAccess = !CanRead(path);
            if (_canDenyAccess) _denied = Load();
        }
        finally
        {
            File.SetUnixFileMode(path, mode);
        }
        _afterDenial = Load();
    }
}

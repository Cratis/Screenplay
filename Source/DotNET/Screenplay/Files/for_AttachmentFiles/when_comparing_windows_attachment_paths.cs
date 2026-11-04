// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_AttachmentFiles;

public class when_comparing_windows_attachment_paths : Specification
{
    [Theory]
    [InlineData(@"C:\models\root", "nested/handler.cs", @"C:\models\root\nested\handler.cs", true)]
    [InlineData(@"C:\models\ROOT", "nested/handler.cs", @"c:\MODELS\root\NESTED\HANDLER.CS", true)]
    [InlineData(@"C:\models\root", "handler.cs", @"C:\models\root2\handler.cs", false)]
    [InlineData(@"\\?\C:\models\root", "handler.cs", @"C:\models\root\handler.cs", true)]
    [InlineData(@"C:\models\root", "handler.cs", @"\\?\C:\models\root\handler.cs", true)]
    [InlineData(@"\\server\share\root", "nested/handler.cs", @"\\server\share\root\nested\handler.cs", true)]
    [InlineData(@"\\?\UNC\server\share\root", "handler.cs", @"\\SERVER\SHARE\root\handler.cs", true)]
    [InlineData(@"\\server\share\root", "handler.cs", @"\\?\UNC\server\share\root\handler.cs", true)]
    [InlineData(@"\\?\UNC\server\share\root", "handler.cs", @"\\?\UNC\server\other\root\handler.cs", false)]
    [InlineData(@"C:\models\root\\", "nested/handler.cs", @"C:\models\root\nested\handler.cs", true)]
    [InlineData(@"\\server\share\root\", "handler.cs", @"\\?\UNC\server\share\root\handler.cs", true)]
    [InlineData(@"C:\", "handler.cs", @"\\?\C:\handler.cs", true)]
    [InlineData(@"C:\models\root", "nested/handler.cs", @"C:\outside\handler.cs", false)]
    [InlineData(@"C:\models\root", "nested/handler.cs", @"C:\models\root\other\handler.cs", false)]
    [InlineData(@"C:\models\root", "handler.cs", @"D:\models\root\handler.cs", false)]
    void should_require_the_exact_normalized_attachment_path(string root, string key, string finalPath, bool expected) =>
        AttachmentFiles.WindowsAttachmentMatchesPath(root, key, finalPath).ShouldEqual(expected);

    [Theory]
    [InlineData("LONGDI~1/handler.cs", @"C:\root\LongDirectory\handler.cs")]
    [InlineData("nested./handler.cs", @"C:\root\nested\handler.cs")]
    [InlineData("nested /handler.cs", @"C:\root\nested\handler.cs")]
    [InlineData("nested/handler.cs", @"C:\outside\handler.cs")]
    void should_classify_a_name_mismatch_without_a_reparse_point_as_a_refused_path(string key, string finalPath) =>
        AttachmentFiles.WindowsAttachmentRefusalCode(false, AttachmentFiles.WindowsAttachmentMatchesPath(@"C:\root", key, finalPath)).ShouldEqual(DiagnosticCodes.AttachmentPathRefused);

    [Fact]
    void should_classify_a_reparse_point_as_a_refused_link() =>
        AttachmentFiles.WindowsAttachmentRefusalCode(true, false).ShouldEqual(DiagnosticCodes.AttachmentLinkRefused);

    [Fact]
    void should_not_refuse_a_literal_tilde_in_a_matching_name() =>
        AttachmentFiles.WindowsAttachmentMatchesPath(@"C:\root", "notes~draft.cs", @"C:\root\notes~draft.cs").ShouldBeTrue();
}

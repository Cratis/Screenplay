// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}

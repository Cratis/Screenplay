// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_DiffCommand;

public class when_comparing_versions_with_persisted_identities : given.two_model_versions
{
    int _exitCode;

    void Establish()
    {
        ToolCommands.Run(["rename", "Projects.Registration.RegisterProject.ProjectRegistered", "ProjectEnrolled", Root], TextWriter.Null, TextWriter.Null).ShouldEqual(0);
        Directory.Delete(After, recursive: true);
        Copy(Root, After);
        var error = new StringWriter();
        ToolCommands.Run(["rename", "Projects.Registration.List.Projects", "ProjectList", After], TextWriter.Null, error);
        error.ToString().ShouldBeEmpty();
    }

    void Because() => _exitCode = ToolCommands.Run(["diff", Root, After], Output, Error);

    // A copy keeps the owner-only access the model root demands of its .screenplay metadata.
    static void Copy(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var directory in Directory.EnumerateDirectories(from, "*", SearchOption.AllDirectories))
        {
            var target = Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, directory)));
            if (!OperatingSystem.IsWindows()) target.UnixFileMode = File.GetUnixFileMode(directory);
        }

        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            File.Copy(file, target);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(target, File.GetUnixFileMode(file));
        }
    }

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_match_by_persisted_identity() => Output.ToString().ShouldContain("matched by persisted identity.");
    [Fact] void should_report_the_rename() => Output.ToString().ShouldContain("Renamed ReadModel Projects.Registration.List.Projects -> Projects.Registration.List.ProjectList");
    [Fact] void should_not_report_a_removal() => Output.ToString().ShouldNotContain("Removed ReadModel");
}

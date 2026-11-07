// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_MigrateCommand;

/// <summary>
/// Writing stops at the first file that cannot be written, and the command says which files it wrote and which it did not.
/// </summary>
public class when_a_file_cannot_be_written : given.a_screenplay_5_model
{
    static string Toolbar(string feature) =>
        $"""
        module Sales
          feature {feature}
            toolbar actions
              button refresh title "Refresh" runs refresh
        """;

    int _exitCode;
    readonly List<string> _attempted = [];

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "b.play"), Toolbar("Billing"));
        File.WriteAllText(Path.Combine(Root, "c.play"), Toolbar("Customers"));
    }

    void Because() => _exitCode = MigrateCommand.Run([Root, "--from", "5"], Output, Error, (path, text) =>
    {
        _attempted.Add(Path.GetFileName(path));
        if (Path.GetFileName(path) == "b.play")
        {
            throw new IOException("The disk is full.");
        }

        File.WriteAllText(path, text);
    });

    [Fact] void should_fail() => _exitCode.ShouldEqual(1);
    [Fact] void should_stop_at_the_file_it_could_not_write() => _attempted.ShouldEqual(["application.play", "b.play"]);
    [Fact] void should_name_the_file_and_the_reason() => Error.ToString().ShouldContain("Could not write 'b.play': The disk is full.");
    [Fact] void should_say_what_it_wrote() => Error.ToString().ShouldContain("Written before the failure: application.play.");
    [Fact] void should_say_what_it_did_not_write() => Error.ToString().ShouldContain("Not written: c.play.");
    [Fact] void should_have_written_the_first_file() => File.ReadAllText(Document).ShouldContain("outlet Navigation of navigation in navigation");
    [Fact] void should_leave_the_last_file_as_it_was() => File.ReadAllText(Path.Combine(Root, "c.play")).ShouldContain("    toolbar actions\n");
    [Fact] void should_still_print_the_report() => Output.ToString().ShouldContain("Migrating 3 files");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_testing_native_paths : given.a_model
{
    [Fact]
    void should_preserve_parent_directory_imports()
    {
        var nested = Path.Combine(Root, "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "root.play"), "import \"../application.play\"");
        ModelTest.Run([Path.Combine(nested, "root.play"), "--filter", "M.F.Register.Correct"], Output, Error).ShouldEqual(0);
        Output.ToString().ShouldContain("1 selected, 1 executed");
    }

    [Fact]
    void should_accept_uppercase_extensions_and_their_imports()
    {
        File.Move(Path.Combine(Root, "application.play"), Path.Combine(Root, "application.PLAY"));
        File.WriteAllText(Path.Combine(Root, "root.PLAY"), "import \"application.PLAY\"");
        ModelTest.Run([Path.Combine(Root, "root.PLAY"), "--filter", "M.F.Register.Correct"], Output, Error).ShouldEqual(0);
        Error.ToString().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_run_under_a_symbolically_linked_directory(bool singleFile)
    {
        var directory = Path.Combine(Root, "model");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(Root, "application.play"), Path.Combine(directory, "application.play"));
        var link = Path.Combine(Root, "linked-model");
        Directory.CreateSymbolicLink(link, directory);
        try
        {
            var target = singleFile ? Path.Combine(link, "application.play") : link;
            ModelTest.Run([target, "--filter", "M.F.Register.Correct"], Output, Error).ShouldEqual(0);
            Output.ToString().ShouldContain("1 selected, 1 executed");
            Error.ToString().ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(link);
        }
    }

    [Fact]
    void should_report_an_invalid_portable_path_as_an_input_error()
    {
        File.Move(Path.Combine(Root, "application.play"), Path.Combine(Root, "CON.play"));
        ModelTest.Run([Path.Combine(Root, "CON.play")], Output, Error).ShouldEqual(2);
        Error.ToString().ShouldContain("reserved device name");
    }
}

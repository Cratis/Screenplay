// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_scoping_a_file_with_parent_imports : given.a_model
{
    string _target;
    int _exitCode;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(Root, "entry"));
        _target = Path.Combine(Root, "entry", "root.play");
        File.WriteAllText(_target, "module M\n  feature F\n    import \"../slice.play\"");
        File.WriteAllText(Path.Combine(Root, "slice.play"), "slice StateChange Clean\n  event Added\n    missingType");
    }

    void Because() => _exitCode = ModelCheck.Run([_target, "--scope", "M.F.Clean"], Output, Error);

    [Fact] void should_fail_for_the_imported_error() => _exitCode.ShouldEqual(1);
    [Fact] void should_retain_the_original_compilation_path() => Output.ToString().ShouldContain("../slice.play(3,");
    [Fact] void should_count_the_same_error_in_both_verdicts() => Output.ToString().ShouldContain("Whole application: 1 error(s), 0 warning(s) (0 outside the reported set)");
}

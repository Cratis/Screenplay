// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_a_non_play_file_path : given.a_disk_application
{
    string _path;

    void Establish()
    {
        _path = Path.Combine(Root, "application.txt");
        File.WriteAllText(_path, "module M\n  feature F\n    slice StateView View");
    }

    void Because() => Resolved = ScopedDiagnostics.TryValidate(_path, "M.F.View", CompletenessChecks.None, out Result, out Error);

    [Fact] void should_not_resolve_the_scope() => Resolved.ShouldBeFalse();
    [Fact] void should_not_return_a_successful_result() => Result.ShouldBeNull();
    [Fact] void should_identify_the_invalid_path() => Error!.Kind.ShouldEqual(ScopeSelectionErrorKind.InvalidPath);
    [Fact] void should_explain_the_extension_requirement() => Error!.Message.ShouldEqual($"'{_path}' is not a .play file");
}

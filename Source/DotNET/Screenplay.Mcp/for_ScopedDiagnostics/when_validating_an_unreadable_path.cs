// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_an_unreadable_path : given.a_disk_application
{
    string _path;
    FileStream _lock;

    void Establish()
    {
        _path = Path.Combine(Root, "application.play");
        File.WriteAllText(_path, "module M\n  feature F\n    slice StateView View");
        _lock = File.Open(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    void Because() => Resolved = ScopedDiagnostics.TryValidate(_path, "M.F.View", CompletenessChecks.None, out Result, out Error);
    void Destroy() => _lock.Dispose();

    [Fact] void should_not_throw_for_an_io_failure() => Resolved.ShouldBeFalse();
    [Fact] void should_not_return_a_successful_result() => Result.ShouldBeNull();
    [Fact] void should_identify_the_unreadable_path() => Error!.Kind.ShouldEqual(ScopeSelectionErrorKind.UnreadablePath);
    [Fact] void should_name_the_path_that_could_not_be_checked() => Error!.Message.ShouldContain($"Could not check '{_path}':");
}

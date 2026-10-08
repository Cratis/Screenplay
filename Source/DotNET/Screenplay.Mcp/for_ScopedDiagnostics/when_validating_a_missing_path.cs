// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_a_missing_path : given.a_disk_application
{
    string _path;

    void Establish() => _path = Path.Combine(Root, "missing.play");
    void Because() => Resolved = ScopedDiagnostics.TryValidate(_path, "M.F.View", CompletenessChecks.None, out Result, out Error);

    [Fact] void should_not_resolve_the_scope() => Resolved.ShouldBeFalse();
    [Fact] void should_not_return_an_empty_successful_result() => Result.ShouldBeNull();
    [Fact] void should_identify_the_invalid_path() => Error!.Kind.ShouldEqual(ScopeSelectionErrorKind.InvalidPath);
    [Fact] void should_name_the_missing_path() => Error!.Message.ShouldEqual($"'{_path}' does not exist");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_protected_read_path_does_not_resolve : given.security_documents
{
    void Establish() => Source(AdmittedProtected.Replace("value = value\n    slice", "value = balance.outstanding\n    slice", StringComparison.Ordinal));
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_have_an_unresolved_read_path() => _normal.Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.UnknownReadPath);
    [Fact] void should_bind_the_safe_subset() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_remove_the_reading_slice_and_its_read_closure() => _result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Write", "M/F/Lookup", "M/F/Inspect"]);
    [Fact] void should_keep_only_the_unrelated_slice() => Slices.ShouldEqual(["Kept"]);
    [Fact] void should_attribute_the_error_to_the_reading_slice() => _result.Exclusions[0].Cause.ShouldEqual(SemanticExclusionCause.Root);
    [Fact] void should_close_the_other_reader_as_a_dependent() => _result.Exclusions.Single(value => value.Address == "M/F/Inspect").Cause.ShouldEqual(SemanticExclusionCause.Dependent);
}

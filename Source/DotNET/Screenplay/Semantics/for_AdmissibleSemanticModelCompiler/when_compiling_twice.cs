// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_compiling_twice : given.documents
{
    AdmissibleCompilationResult _again;

    void Establish() => Source(Unsupported + "    slice StateView Dependent\n      query All => View[]\n");
    void Because()
    {
        _result = _compiler.CompileAdmissible("Example", _documents);
        _again = _compiler.CompileAdmissible("Example", _documents);
    }

    [Fact] void should_return_the_same_causes() => _again.Exclusions.Select(value => value.Cause).ShouldEqual(_result.Exclusions.Select(value => value.Cause));
    [Fact] void should_return_the_same_dependencies() => _again.Exclusions.SelectMany(value => value.DependsOn).ShouldEqual(_result.Exclusions.SelectMany(value => value.DependsOn));
    [Fact] void should_return_the_same_order() => _again.Exclusions.Select(value => value.Address).ShouldEqual(_result.Exclusions.Select(value => value.Address));
    [Fact] void should_return_the_same_diagnostics() => _again.Exclusions.SelectMany(value => value.Diagnostics).ShouldEqual(_result.Exclusions.SelectMany(value => value.Diagnostics));
    [Fact] void should_return_the_same_locations() => _again.Exclusions.Select(value => value.Location).ShouldEqual(_result.Exclusions.Select(value => value.Location));
}

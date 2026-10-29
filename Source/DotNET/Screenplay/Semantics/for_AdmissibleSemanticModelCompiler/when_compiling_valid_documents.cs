// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_compiling_valid_documents : given.documents
{
    void Establish() => Source(Valid);
    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact] void should_succeed() => _result.Compilation.Success.ShouldBeTrue();
    [Fact] void should_exclude_nothing() => _result.Exclusions.ShouldBeEmpty();
    [Fact] void should_return_the_same_model() => Serialization.SemanticModelSerializer.Serialize(_result.Compilation.Value!.Model).ShouldEqual(Serialization.SemanticModelSerializer.Serialize(_normal.Value!.Model));
    [Fact] void should_return_the_same_diagnostics() => _result.Compilation.Diagnostics.ShouldEqual(_normal.Diagnostics);
}

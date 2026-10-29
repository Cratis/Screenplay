// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_compiling_unprunable_documents : given.documents
{
    [Theory]
    [InlineData("numbers exact\n" + Valid)]
    [InlineData("module\n")]
    [InlineData("module M\n  feature F\n    slice StateChange Broken\n      command Save\n        first String identifier\n        second String identifier\n")]
    void should_preserve_the_strict_failure(string source)
    {
        Source(source);
        var result = _compiler.CompileAdmissible("Example", _documents);
        _normal.Success.ShouldBeFalse();
        result.Compilation.Success.ShouldBeFalse();
        result.Compilation.Diagnostics.ShouldEqual(_normal.Diagnostics);
        result.Exclusions.ShouldBeEmpty();
    }
}

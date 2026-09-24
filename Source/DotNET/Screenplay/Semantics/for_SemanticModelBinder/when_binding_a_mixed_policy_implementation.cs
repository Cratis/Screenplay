// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_mixed_policy_implementation : given.a_semantic_binder
{
    [Theory]
    [InlineData("file Policies/Access.cs")]
    [InlineData("```csharp\n  return true;\n  ```")]
    void should_reject_require_combined_with_implementation(string implementation)
    {
        var source = $"policy Access\n  require authenticated\n  {implementation}\nmodule Portal\n  feature Reports\n    slice StateChange FileReport\n      command FileReport\n        authorize Access\n";
        var parsed = new ScreenplayCompiler().Parse(source);
        parsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.MixedPolicyImplementation);
        var bound = Bind(source);
        bound.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.MixedPolicyImplementation);
        bound.Success.ShouldBeFalse();
        bound.ImplementationRequirements.ShouldBeEmpty();
    }
}

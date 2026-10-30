// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_a_protected_read_cannot_be_protected : given.security_documents
{
    [Theory]
    [InlineData("payload-key")]
    [InlineData("by-parts")]
    void should_attribute_the_refusal_and_close_every_reader(string reason)
    {
        Source(reason == "by-parts" ? CompositeProtected : AdmittedProtected.Replace("from Recorded", "from Recorded key value", StringComparison.Ordinal));
        var result = _compiler.CompileAdmissible("Example", _documents);

        _normal.Diagnostics.ShouldContain(value => value.Code == DiagnosticCodes.UnprotectableDecisionRead && value.Arguments["reason"].Value.GetString() == reason);
        result.Compilation.Success.ShouldBeTrue();
        result.Exclusions.Select(value => value.Address).ShouldEqual(["M/F/Write", "M/F/Lookup", "M/F/Inspect"]);
        result.Exclusions.Where(value => value.Address != "M/F/Lookup").ShouldEachConformTo(value => value.Cause == SemanticExclusionCause.Root);
        result.Exclusions.Single(value => value.Address == "M/F/Lookup").Cause.ShouldEqual(SemanticExclusionCause.Dependent);
        result.Compilation.Value.Model.Application.Modules.Single().Features.Single().Slices.Select(value => value.Name).ShouldEqual(["Kept"]);
    }
}

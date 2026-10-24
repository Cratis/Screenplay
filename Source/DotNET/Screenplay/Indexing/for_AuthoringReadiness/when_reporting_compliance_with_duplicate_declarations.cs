// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Indexing.for_AuthoringReadiness;

public class when_reporting_compliance_with_duplicate_declarations : Specification
{
    [Theory]
    [InlineData("concept Protected : String pii\nconcept Protected : String pii", "Protected")]
    [InlineData("concept Protected : String\nconcept Protected : String pii", "Protected")]
    [InlineData("concept Protected : String\nconcept Protected : String secret\n  secret scope subject", "Protected")]
    [InlineData("concept Protected : String pii\ntype Details\n  value String\ntype Details\n  value Protected", "Details[]")]
    [InlineData("concept Protected : String pii\ntype Details\n  nested Details\ntype Details\n  value Protected", "Details")]
    void should_preserve_duplicate_diagnostics_and_report_readiness_in_the_snapshot(string declarations, string type)
    {
        var source = $"{declarations}\nmodule M\n  feature F\n    slice StateChange S\n      event Changed\n        id Uuid subject\n        value {type}";
        var snapshot = new AuthoringSnapshot(new Dictionary<string, string> { ["application.play"] = source });
        Assert.Contains(snapshot.Compilation.Diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateDeclaration);
        var index = snapshot.Index;
        index.Readiness.ModelSyntaxOnly.ShouldBeTrue();
        index.Readiness.ModelExecutionReadiness.ShouldContain("concept compliance");
        var declaration = index.Declarations.Single(declaration => declaration.Kind == "Event" && declaration.Name == "Changed");
        index.Readiness.SyntaxOnly(declaration.Syntax).ShouldBeTrue();
        index.Readiness.ExecutionReadiness(declaration.Syntax).ShouldContain("PLAY0268");
    }
}

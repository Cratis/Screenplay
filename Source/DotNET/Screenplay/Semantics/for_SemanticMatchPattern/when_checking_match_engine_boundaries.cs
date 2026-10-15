// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_checking_match_engine_boundaries
{
    [Fact]
    void should_centralize_engine_construction_and_never_catch_a_timeout_as_a_verdict()
    {
        var root = Path.Combine(PortableMatchVectors.Root(), "Source/DotNET/Screenplay/Semantics");
        string[] paths = ["SemanticModelBinder.Validations.cs", "SemanticModelValidator.Validations.cs", "Execution/SemanticValidationRules.cs", "Execution/SemanticExecutionPlan.cs", "Generation/RuleConsistency.cs", "Generation/FiniteGenerationRules.cs"];
        paths.Length.ShouldEqual(6);
        foreach (var path in paths)
        {
            var source = File.ReadAllText(Path.Combine(root, path));
            source.ShouldNotContain("new Regex(");
            source.ShouldNotContain("RegexOptions.");
            source.ShouldNotContain("RegexMatchTimeoutException");
            source.ShouldNotContain("TimeSpan.From");
        }
        var factory = File.ReadAllText(Path.Combine(root, "SemanticMatchPattern.cs"));
        factory.ShouldContain("RegexOptions.NonBacktracking | RegexOptions.CultureInvariant, Regex.InfiniteMatchTimeout");
        factory.ShouldNotContain("RegexOptions.ECMAScript");
        factory.ShouldNotContain("TimeSpan.From");
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticMatchPattern;

public class when_checking_match_engine_boundaries
{
    [Fact]
    void should_never_construct_a_platform_regex_on_a_matches_path()
    {
        var root = Path.Combine(PortableMatchVectors.Root(), "Source/DotNET/Screenplay/Semantics");
        string[] consumers = ["SemanticModelBinder.Validations.cs", "SemanticModelValidator.Validations.cs", "SemanticValidationRules.cs", "SemanticValidationRules.Checks.cs", "SemanticValidationRules.Types.cs", "Execution/SemanticExecutionPlan.cs", "Generation/RuleConsistency.cs", "Generation/FiniteGenerationRules.cs"];
        var matcher = Directory.GetFiles(root, "*.cs").Where(path => Path.GetFileName(path).StartsWith("Match", StringComparison.Ordinal) || Path.GetFileName(path).StartsWith("PortableMatch", StringComparison.Ordinal) || Path.GetFileName(path) == "SemanticMatchPattern.cs");
        var paths = consumers.Select(path => Path.Combine(root, path)).Concat(matcher).ToArray();
        paths.Length.ShouldBeGreaterThan(15);
        foreach (var path in paths)
        {
            var source = File.ReadAllText(path);
            source.ShouldNotContain("System.Text.RegularExpressions");
            source.ShouldNotContain("new Regex(");
            source.ShouldNotContain("RegexOptions.");
            source.ShouldNotContain("RegexMatchTimeoutException");
            source.ShouldNotContain("TimeSpan.From");
        }
    }
}

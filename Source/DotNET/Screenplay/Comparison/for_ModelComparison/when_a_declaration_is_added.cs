// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_a_declaration_is_added : given.two_models
{
    void Establish()
    {
        ChangeSource(Source.Replace("      command Register\n", "      command Additional\n      command Register\n", StringComparison.Ordinal));
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_the_added_command() => _result.Declarations.Any(change => change.Declaration.Kind == "Command" && change.Change == DeclarationChangeKind.Added).ShouldBeTrue();
    [Fact] void should_report_semantic_change() => _result.HasSemanticChange.ShouldEqual(true);
}

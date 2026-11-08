// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_Storylines;

public class when_a_storyline_is_not_valid : given.a_storyline_model
{
    const string Storylines =
        """
        storyline 9Bad
          command RequestReturn

        storyline ReturnAnItem
          aggregate Returns
          command RequestReturn "Asks"
            event ReturnRequested
          command
          event ReturnRequested

        storyline ReturnAnItem
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Establish() => _result = new ScreenplayCompiler().Compile(Model + Storylines);

    [Fact] void should_report_the_invalid_declaration() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidStorylineDeclaration).ShouldEqual(1);
    [Fact] void should_report_each_invalid_step_and_nested_line() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidStorylineStep).ShouldEqual(3);
    [Fact] void should_report_the_duplicate_name() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateStoryline).ShouldEqual(1);
    [Fact] void should_keep_the_valid_steps() => _result.Value!.Storylines.ElementAt(1).Steps.Select(step => step.Target).ShouldEqual(["RequestReturn", "ReturnRequested"]);
}

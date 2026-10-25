// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_a_slice_publishes_what_another_slice_appends;

public class and_each_slice_states_only_its_own_facts : given.a_publishing_model
{
    CompilationResult<SemanticCompilation> _compilation;
    SemanticSpecificationRun _command;
    SemanticSpecificationRun _publication;
    SemanticSpecificationRun _mismatched;

    void Establish() => _compilation = Compile(Model);

    void Because()
    {
        _command = Run(_compilation, "SettingAPrice");
        _publication = Run(_compilation, "APriceIsPublished");
        _mismatched = Run(_compilation, "ThePublicationMustMatch");
    }

    [Fact] void should_compile() => Assert.True(_compilation.Success, string.Join('\n', _compilation.Diagnostics.Select(_ => _.Message)));
    [Fact] void should_pass_the_command_without_the_publication() => Assert.True(_command.Passed, string.Join('\n', _command.Failures));
    [Fact] void should_pass_the_publication_in_its_own_slice() => Assert.True(_publication.Passed, string.Join('\n', _publication.Failures));
    [Fact] void should_fail_a_publication_that_differs() => _mismatched.Passed.ShouldBeFalse();
}

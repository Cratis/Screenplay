// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// A projection with <c>variant</c> blocks builds one read model per variant, named after the variant - not
/// one named after the projection's own identity. A command's <c>reads</c> must resolve a variant name exactly
/// as it would an ordinary projection's read model, and the identity name itself must not be mistaken for one.
/// </summary>
public class when_compiling_a_command_that_reads_a_projection_variant : given.a_compiler
{
    const string Source =
        """
        module Issues
          feature WorkTracking
            slice StateChange StartReview
              command StartReview
                issueId Uuid
                reads PullRequestItem by issueId

                produces ReviewStarted
                  issueId = issueId

              event IssueCreated
                title String

              event PullRequestCreated
                pullRequestUrl String

              event ReviewStarted
                issueId Uuid

            slice StateView WorkItems
              projection WorkItem
                variant BacklogItem
                  enters on IssueCreated

                variant PullRequestItem
                  enters on PullRequestCreated
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_report_the_variant_as_an_unknown_read_model() =>
        _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnknownReadModel).ShouldBeFalse();
}

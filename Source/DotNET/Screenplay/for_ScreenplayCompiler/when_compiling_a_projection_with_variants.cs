// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// A <c>variant</c> block compiles into a <see cref="ProjectionVariantSyntax"/>. Everything at the projection's
/// own level (outside every variant) is a shared handler; each variant carries only what it declares itself.
/// </summary>
public class when_compiling_a_projection_with_variants : given.a_compiler
{
    const string Source =
        """
        projection WorkItem
          from TitleChanged
            title = title

          variant BacklogItem
            enters on IssueCreated

          variant DevelopmentItem
            enters on IssueStarted

          variant PullRequestItem
            enters on PullRequestCreated

            from BuildCompleted
              buildStatus = status
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_parse_no_read_model_for_the_ad_hoc_identity() => _result.Value!.ReadModel.ShouldBeNull();
    [Fact] void should_parse_the_shared_handler_at_the_projection_level() => _result.Value!.Blocks.OfType<FromSyntax>().Single().Events.Single().Event.ShouldEqual("TitleChanged");
    [Fact] void should_parse_every_variant() => Variants.Select(_ => _.Name).ShouldContainOnly("BacklogItem", "DevelopmentItem", "PullRequestItem");
    [Fact] void should_parse_the_entering_event_of_each_variant() => Variants.Select(_ => _.EntersOn.Single().Event).ShouldContainOnly("IssueCreated", "IssueStarted", "PullRequestCreated");

    [Fact]
    void should_parse_the_variants_own_block()
    {
        var pullRequestItem = Variants.Single(_ => _.Name == "PullRequestItem");
        pullRequestItem.Blocks.OfType<FromSyntax>().Single().Events.Single().Event.ShouldEqual("BuildCompleted");
    }

    [Fact]
    void should_parse_no_extra_blocks_for_a_variant_with_only_its_entering_event()
    {
        var backlogItem = Variants.Single(_ => _.Name == "BacklogItem");
        backlogItem.Blocks.ShouldBeEmpty();
    }

    IEnumerable<ProjectionVariantSyntax> Variants => _result.Value!.Blocks.OfType<ProjectionVariantSyntax>();
}

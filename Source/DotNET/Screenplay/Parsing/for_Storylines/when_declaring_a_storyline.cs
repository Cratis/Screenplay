// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_Storylines;

public class when_declaring_a_storyline : given.a_storyline_model
{
    const string Storyline =
        """
        storyline ReturnAnItem "A customer returns an item"
          description "From the request to the refund"
          module Returns
          feature Returns.Requests
          command RequestReturn "The customer asks to return the item"
          event ReturnRequested
          readmodel PendingReturn
          query AllPending
          slice Refunds.RequestReturn
          event RefundIssued "Money goes back"
        """;

    CompilationResult<ApplicationSyntax> _result;
    StorylineSyntax _storyline;

    void Establish() => _result = new ScreenplayCompiler().Compile(Model + Storyline);

    void Because() => _storyline = _result.Value!.Storylines.Single();

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_name() => _storyline.Name.ShouldEqual("ReturnAnItem");
    [Fact] void should_keep_the_title() => _storyline.Title.ShouldEqual("A customer returns an item");
    [Fact] void should_keep_the_description() => _storyline.Description.ShouldEqual("From the request to the refund");
    [Fact] void should_keep_every_step_in_order() => _storyline.Steps.Select(step => step.Target).ShouldEqual(["Returns", "Returns.Requests", "RequestReturn", "ReturnRequested", "PendingReturn", "AllPending", "Refunds.RequestReturn", "RefundIssued"]);
    [Fact] void should_keep_the_kind_of_each_step() => _storyline.Steps.Select(step => step.ElementKind).ShouldEqual([StorylineElementKind.Module, StorylineElementKind.Feature, StorylineElementKind.Command, StorylineElementKind.Event, StorylineElementKind.ReadModel, StorylineElementKind.Query, StorylineElementKind.Slice, StorylineElementKind.Event]);
    [Fact] void should_keep_a_step_note() => _storyline.Steps.ElementAt(2).Note.ShouldEqual("The customer asks to return the item");
    [Fact] void should_leave_a_missing_note_null() => _storyline.Steps.First().Note.ShouldBeNull();
    [Fact] void should_print_the_storyline_the_way_it_was_written() => new ScreenplayPrinter().Print(_result.Value!).ShouldContain(Storyline.Replace("\r\n", "\n"));
}

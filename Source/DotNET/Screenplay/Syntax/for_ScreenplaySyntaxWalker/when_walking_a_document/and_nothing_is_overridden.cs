// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker.when_walking_a_document;

public class and_nothing_is_overridden : given.the_invoicing_document
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new walker().VisitApplication(_document));

    [Fact] void should_walk_the_whole_document_without_failing() => _error.ShouldBeNull();

    class walker : ScreenplaySyntaxWalker;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Ui.for_ExecutableScreens;

public class when_admitting_screens_with_an_unresolved_reference : Specification
{
    const string Source = """
        module Lending
          feature Catalog
            slice StateChange AddBook
              command AddBook
                bookId Uuid identifier
                title String
                produces BookAdded
                  for bookId
                  title = title
              event BookAdded
                title String
              screen AddBook
                title "Add a book"
                action AddBook
                  navigate to Nowhere
        """;

    ExecutableScreensResult _result;

    void Because() => _result = ExecutableScreens.Admit(ComparedModel.FromSources("Lending", new Dictionary<string, string> { ["application.play"] = Source }).Workspace);

    [Fact] void should_refuse() => _result.Success.ShouldBeFalse();
    [Fact] void should_name_the_reference() => _result.Refusals.ShouldContainOnly(["Screen 'AddBook' in 'Lending.Catalog.AddBook' leads to 'Nowhere', which does not resolve to exactly one screen of the executable model."]);
}

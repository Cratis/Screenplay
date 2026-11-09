// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Ui.for_ExecutableScreens;

public class when_admitting_a_model_without_screens : Specification
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
        """;

    Workspaces.ScreenplayWorkspace _workspace;
    ExecutableScreensResult _result;

    void Establish() => _workspace = ComparedModel.FromSources("Lending", new Dictionary<string, string> { ["application.play"] = Source }).Workspace;

    void Because() => _result = ExecutableScreens.Admit(_workspace);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_compilation() => _result.Compilation.ShouldBeSame(_workspace.Compilation.Value);
}

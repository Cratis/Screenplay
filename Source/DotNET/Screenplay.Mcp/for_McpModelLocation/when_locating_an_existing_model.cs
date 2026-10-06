// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpModelLocation;

public class when_locating_an_existing_model : given_a_project
{
    string _located = null!;

    void Establish()
    {
        Directory.CreateDirectory(PathOf("Source"));
        Play("Model/application.play");
        Play("Model/Billing/invoices.play");
        Play("node_modules/pkg/ignored.play");
    }

    void Because() => _located = McpModelLocation.Project(Project);

    [Fact] void should_use_the_folder_holding_the_model() => _located.ShouldEqual(PathOf("Model"));
    [Fact] void should_not_create_a_fallback_folder() => Directory.Exists(PathOf("Screenplay")).ShouldBeFalse();
}

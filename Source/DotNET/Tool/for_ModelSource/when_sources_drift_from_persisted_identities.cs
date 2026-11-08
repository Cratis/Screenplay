// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelSource;

public class when_sources_drift_from_persisted_identities : Tool.given.a_renderable_model
{
    int _exitCode;

    void Establish()
    {
        ToolCommands.Run(["rename", "Projects.Registration.List.Projects", "ProjectList", Root], TextWriter.Null, TextWriter.Null).ShouldEqual(0);
        var path = Path.Combine(Root, "application.play");
        File.WriteAllText(path, File.ReadAllText(path).Replace("ProjectList", "Catalogue", StringComparison.Ordinal));
    }

    void Because() => _exitCode = ToolCommands.Run(["export", Root], Output, Error);

    [Fact] void should_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_refuse_to_rederive_identities() => Error.ToString().ShouldContain("IdentityReconciliationRequired");
    [Fact] void should_write_no_model() => Output.ToString().ShouldBeEmpty();
}

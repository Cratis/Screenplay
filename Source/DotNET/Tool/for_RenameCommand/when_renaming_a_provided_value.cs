// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_RenameCommand;

public class when_renaming_a_provided_value : Tool.given.a_renderable_model
{
    int _exitCode;
    string _source;

    void Establish() => Write(Root, """
        provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId

        module Fleet
          feature Quality
            slice StateView Vessels
              available when $context.vessel.hasReported == true
              query VesselByKey => Vessel optional
                by vesselId Uuid
              readmodel Vessel
                vesselId Uuid
                hasReported Bool
              event VesselRegistered
                vesselId Uuid
              projection Vessels => Vessel
                from VesselRegistered key vesselId
                  vesselId = vesselId
        """);

    void Because()
    {
        _exitCode = ToolCommands.Run(["rename", "vessel", "ship", Root], Output, Error);
        _source = File.ReadAllText(Path.Combine(Root, "application.play"));
    }

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_rename_the_declaration() => _source.ShouldContain("provide ship Vessel");
    [Fact] void should_rename_the_read() => _source.ShouldContain("$context.ship.hasReported");
    [Fact] void should_report_the_kind() => Output.ToString().ShouldContain("Provide");
}

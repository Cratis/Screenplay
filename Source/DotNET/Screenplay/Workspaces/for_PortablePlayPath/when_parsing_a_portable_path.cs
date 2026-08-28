// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_PortablePlayPath;

public class when_parsing_a_portable_path : Specification
{
    PortablePlayPath _path;

    void Because() => _path = PortablePlayPath.Parse("Projects\\Registration/Cafe\u0301.play");

    [Fact] void should_normalize_separators() => _path.Value.ShouldNotContain("\\");
    [Fact] void should_normalize_unicode_to_nfc() => _path.Value.ShouldEqual("Projects/Registration/Café.play");
    [Fact] void should_use_the_normalized_value_as_text() => _path.ToString().ShouldEqual(_path.Value);
}

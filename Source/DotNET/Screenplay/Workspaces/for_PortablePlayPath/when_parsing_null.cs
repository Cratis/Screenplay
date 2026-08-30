// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_PortablePlayPath;

public class when_parsing_null : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => PortablePlayPath.Parse(null!));

    [Fact] void should_throw_the_documented_exception() => _exception.ShouldBeOfExactType<InvalidPortablePlayPath>();
}

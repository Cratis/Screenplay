// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution.given;

public static class response_scenarios
{
    public const string Mixed = """
        concept Id : Uuid
        module M
          feature F
            slice StateChange Responses
              command Echo
                name String
                returns name
              command Allocate
                id Id generated identifier
                returns id
              specification Wrong
                when Echo
                  name = "hello"
                then returns "different"
              specification MissingFixture
                when Allocate
                then returns "11111111-1111-1111-1111-111111111111"
        """;

    public const string Example = """
        module M
          feature F
            slice StateChange Scalar
              command Echo
                name String
                returns name
              example Greeting : Echo
                name = "hello"
              specification Wrong
                when Greeting
                then returns "different"
        """;

    public const string Source = """
        module M
          feature F
            slice StateChange Scalar
              command Echo
                name String
                returns name
              specification Wrong
                when Echo
                  name = "hello"
                then returns "different"
            slice StateChange Record
              command EchoRecord
                name String
                returns
                  greeting = name
              specification Wrong
                when EchoRecord
                  name = "hello"
                then returns
                  greeting = "different"
            slice StateView Query
              readmodel Greeting
                name String
              query All => Greeting[]
              specification Wrong
                given readmodel Greeting
                  name = "hello"
                when query All
                then result
                  name = "different"
        """;
}

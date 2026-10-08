// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_checking_sensitive_destinations : given.a_compiler
{
    [Theory]
    [InlineData("eventsource Secrets\n  identifier SecretId", 3)]
    [InlineData("module M\n  feature F\n    slice StateChange S\n      event E\n      command C\n        secret SecretId\n        produces E\n          for secret", 9)]
    [InlineData("type Destination\n  secret SecretId\nmodule M\n  feature F\n    slice StateChange S\n      event E\n      command C\n        destination Destination\n        produces E\n          for destination.secret", 11)]
    [InlineData("module M\n  feature F\n    slice Automation S\n      event E\n        secret SecretId\n      event Recorded\n      reaction R\n        when E\n          produces Recorded\n            for secret", 11)]
    [InlineData("module M\n  feature F\n    slice Automation S\n      event Recorded\n      reaction R\n        when External\n          secret SecretId\n          produces Recorded\n            for secret", 10)]
    [InlineData("trigger External\n  secret SecretId\nmodule M\n  feature F\n    slice Automation S\n      event Recorded\n      reaction R\n        when External\n          produces Recorded\n            for secret", 11)]
    public void should_reject_sensitive_event_source_destinations(string source, int line)
    {
        var result = _compiler.Compile("concept SecretId : Uuid @sensitive\n" + source);
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.PiiNotSupportedOnIdentifier);
        diagnostic.Code.ShouldEqual(DiagnosticCodes.PiiNotSupportedOnIdentifier);
        diagnostic.Location.Line.ShouldEqual(line);
        diagnostic.Message.ShouldEqual("Concept 'SecretId' is @sensitive and cannot be an event source identifier - use a surrogate Uuid identifier and keep the @sensitive value as a property");
    }

    [Theory]
    [InlineData("@pii @sensitive")]
    [InlineData("@sensitive @pii")]
    public void should_reject_combined_classifications_once(string attributes)
    {
        var result = _compiler.Compile($"concept SecretId : Uuid {attributes}\neventsource Secrets\n  identifier SecretId");
        result.Diagnostics.Single().Message.ShouldEqual("Concept 'SecretId' is @pii and cannot be an event source identifier - use a surrogate Uuid identifier and keep the @pii value as a property");
    }

    [Fact]
    public void should_keep_protected_values_as_ordinary_properties()
    {
        var result = _compiler.Compile("""
            concept Secret : String @sensitive
            concept Personal : String @pii @sensitive
            module M
              feature F
                slice StateChange S
                  command C
                    id Uuid identifier
                    secret Secret
                    personal Personal
            """);
        result.Diagnostics.ShouldBeEmpty();
    }
}

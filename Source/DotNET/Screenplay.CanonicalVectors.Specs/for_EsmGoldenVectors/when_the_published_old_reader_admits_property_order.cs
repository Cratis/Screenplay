// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_the_published_old_reader_admits_property_order : given.golden_files
{
    JsonElement _evidence;
    EsmMinorGoldenVector _vector;
    SemanticReadModel _readModel;

    async Task Because()
    {
        _vector = EsmGoldenVectors.GetMinor(SemanticVersion.V36_1);
        _readModel = SemanticModelSerializer.Deserialize(_vector.Bytes.AsSpan()).Application.Modules[0].Features[0].Slices.SelectMany(slice => slice.ReadModels).Single();
        var configuration = new DirectoryInfo(Path.GetDirectoryName(typeof(when_the_published_old_reader_admits_property_order).Assembly.Location)!).Parent!.Name;
        var probe = Path.GetFullPath(Path.Combine(_goldenFolder, "../../../../Screenplay.CanonicalVectors.Specs/LegacyReader/bin", configuration, "net10.0/LegacyReader.dll"));
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(probe);
        start.ArgumentList.Add(Path.Combine(_goldenFolder, "full-esm-v36.1.json"));
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(timeout.Token);
        if (process.ExitCode != 0) throw new InvalidSemanticContract(await error);
        using var evidence = JsonDocument.Parse(await output);
        _evidence = evidence.RootElement.Clone();
    }

    [Fact] void should_exercise_a_composite_read_model_key() => _readModel.Properties.Count(property => property.IsIdentifier).ShouldEqual(2);
    [Fact] void should_accept_with_the_unmodified_published_5_3_1_reader() => _evidence.GetProperty("Version").GetString().ShouldEqual("36.0");
    [Fact] void should_preserve_the_wire_revision_across_typed_promotion() => _evidence.GetProperty("Revision").GetString().ShouldEqual(_vector.SemanticRevision.ToString());
    [Fact]
    void should_preserve_every_opaque_wire_byte()
    {
        byte[] expected = [.. _vector.Bytes];
        Convert.FromBase64String(_evidence.GetProperty("Bytes").GetString()!).ShouldEqual(expected);
    }
    [Fact] void should_inventory_the_member_as_unknown() => _evidence.GetProperty("Name").GetString().ShouldEqual("propertyOrder");
    [Fact] void should_attribute_it_to_the_read_model() => _evidence.GetProperty("Owner").GetString().ShouldEqual(_readModel.Id.ToString());
    [Fact] void should_preserve_the_opaque_order_value() => Convert.FromBase64String(_evidence.GetProperty("Value").GetString()!).ShouldEqual(JsonSerializer.SerializeToUtf8Bytes(_readModel.PropertyOrder.Select(id => id.ToString())));

    [Fact]
    void should_leave_the_complete_known_contract_including_composite_keys_unchanged() =>
        Convert.FromBase64String(_evidence.GetProperty("Projection").GetString()!).ShouldEqual(global::Cratis.Screenplay.CanonicalVectors.Specs.given.property_order_projection.Bytes(_vector));
}

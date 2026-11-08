// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Reads and writes the UI contract as JSON.
/// </summary>
public static class UiContractJson
{
    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Writes a contract as UTF-8 JSON ending with a newline.
    /// </summary>
    /// <param name="contract">The contract.</param>
    /// <returns>The JSON bytes; the same contract always gives the same bytes.</returns>
    public static byte[] Write(UiContract contract) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(contract, _options).ReplaceLineEndings("\n") + "\n");

    /// <summary>
    /// Reads a contract from JSON.
    /// </summary>
    /// <param name="json">The JSON bytes.</param>
    /// <returns>The contract.</returns>
    /// <exception cref="InvalidDataException">The JSON is not a contract this library reads.</exception>
    public static UiContract Read(ReadOnlySpan<byte> json)
    {
        var contract = JsonSerializer.Deserialize<UiContract>(json, _options) ?? throw new InvalidDataException("The JSON holds no UI contract.");
        if (contract.Contract != UiContract.Version) throw new InvalidDataException($"The UI contract is '{contract.Contract}'; this library reads '{UiContract.Version}'.");
        return contract;
    }
}

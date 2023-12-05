﻿// ReSharper disable MemberCanBeInternal
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Aer.QdrantClient.Http.Models.Primitives.Vectors;

/// <summary>
/// Represents a named vectors collection.
/// </summary>
public sealed class NamedVectors : VectorBase
{
    /// <summary>
    /// The name to vector mapping.
    /// </summary>
    public required Dictionary<string, float[]> Vectors { init; get; } = new();

    /// <inheritdoc/>
    public override float[] Default
    {
        get
        {
            EnsureNotEmpty();

            if (Vectors.TryGetValue(DefaultVectorName, out float[] defaultVectorValue))
            {
                return defaultVectorValue;
            }

            throw new InvalidOperationException(
                $"Can't find default vector with name {DefaultVectorName}");
        }
    }

    /// <inheritdoc/>
    public override float[] FirstOrDefault()
    {
        EnsureNotEmpty();

        return Vectors.First().Value;
    }

    /// <inheritdoc/>
    public override bool ContainsVector(string vectorName)
    {
        EnsureNotEmpty();

        return Vectors.ContainsKey(vectorName);
    }

    /// <inheritdoc/>
    public override float[] this[string vectorName]
    {
        get
        {
            EnsureNotEmpty();

            if (Vectors.TryGetValue(vectorName, out var vectorValues))
            {
                return vectorValues;
            }

            throw new KeyNotFoundException($"Named vector {vectorName} for point is not found");
        }
    }

    private void EnsureNotEmpty()
    {
        if (Vectors.Count is 0)
        {
            throw new InvalidOperationException(
                $"Named vectors collection for point is empty");
        }
    }
}

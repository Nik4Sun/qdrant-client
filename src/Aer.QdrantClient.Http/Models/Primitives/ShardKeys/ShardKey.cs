﻿namespace Aer.QdrantClient.Http.Models.Primitives;

/// <summary>
/// Represents a point shard key value.
/// </summary>
public abstract class ShardKey
{
    /// <summary>
    /// Returns <c>true</c> if this is an integer shard key, <c>false</c> otherwise.
    /// </summary>
    public abstract bool IsInteger();

    /// <summary>
    /// Returns <c>true</c> if this is a string shard key, <c>false</c> otherwise.
    /// </summary>
    public abstract bool IsString();

    /// <summary>
    /// Gets the integer shard key value.
    /// </summary>
    public abstract ulong GetInteger();

    /// <summary>
    /// Gets the string shard key value.
    /// </summary>
    public abstract string GetString();

    #region Operators

    /// <summary>
    /// Performs an implicit conversion from <see cref="ulong"/> to <see cref="ShardKey"/>.
    /// </summary>
    /// <param name="shardKey">The shard key value.</param>
    public static implicit operator ShardKey(ulong shardKey)
    {
        return Integer(shardKey);
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="int"/> to <see cref="ShardKey"/>.
    /// </summary>
    /// <param name="shardKey">The shard key value.</param>
    public static implicit operator ShardKey(int shardKey)
    {
        return Integer(checked((ulong)shardKey));
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="String"/> to <see cref="ShardKey"/>.
    /// </summary>
    /// <param name="shardKey">The shard key value.</param>
    public static implicit operator ShardKey(string shardKey)
    {
        return String(shardKey);
    }

    #endregion

    #region Factory methods

    /// <summary>
    /// Creates a <see cref="ShardKey"/> instance from string shard key value.
    /// </summary>
    /// <param name="shardKeyValue">Shard key value.</param>
    public static ShardKey String(string shardKeyValue)
        => new StringShardKey(shardKeyValue);

    /// <summary>
    /// Creates a <see cref="ShardKey"/> instance from integer shard key value.
    /// </summary>
    /// <param name="shardKeyValue">Shard key value.</param>
    public static ShardKey Integer(ulong shardKeyValue)
        => new IntegerShardKey(shardKeyValue);

    #endregion
}

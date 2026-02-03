using System.Numerics.Tensors;
using System.Runtime.InteropServices;

namespace Atomic.Net.MonoGame.Core;

public static class TensorSparse
{
    private readonly static byte[] _cacheLeft = new byte[Constants.MaxSceneEntities];
    private readonly static byte[] _cacheRight = new byte[Constants.MaxSceneEntities];
    private readonly static byte[] _cacheResult = new byte[Constants.MaxSceneEntities];


    /// <summary>
    /// Bitwise SIMD ANDs together 2 sparse arrays, and writes
    /// the values out the a result array.
    ///
    /// NOTE this does NOT clear the result array before hand, so old values will persist
    /// </summary>
    public static void And(
        SparseArray<bool> left, SparseArray<bool> right, SparseArray<bool> result
    )
    {
        MemoryMarshal.Cast<bool, byte>(left.Values).CopyTo(_cacheLeft);
        MemoryMarshal.Cast<bool, byte>(right.Values).CopyTo(_cacheRight);

        TensorPrimitives.BitwiseAnd(_cacheLeft, _cacheRight, _cacheResult);

        for(uint n = 0; n < left.Capacity; n++)
        {
            if (_cacheResult[n] == 1)
            {
                result.Set(n, true);
            }
        }
    }

    /// <summary>
    /// Bitwise SIMD ORs together 2 sparse arrays, and writes
    /// the values out the a result array.
    ///
    /// NOTE this does NOT clear the result array before hand, so old values will persist
    /// </summary>
    public static void Or(
        SparseArray<bool> left, SparseArray<bool> right, SparseArray<bool> result
    )
    {
        MemoryMarshal.Cast<bool, byte>(left.Values).CopyTo(_cacheLeft);
        MemoryMarshal.Cast<bool, byte>(right.Values).CopyTo(_cacheRight);

        TensorPrimitives.BitwiseOr(_cacheLeft, _cacheRight, _cacheResult);

        for(uint n = 0; n < left.Capacity; n++)
        {
            if (_cacheResult[n] == 1)
            {
                result.Set(n, true);
            }
        }
    }

    /// <summary>
    /// Bitwise SIMD ANDs together 2 partitioned sparse arrays, and writes
    /// the values out the a result array.
    ///
    /// NOTE this does NOT clear the result array before hand, so old values will persist
    /// </summary>
    public static void And(
        PartitionedSparseArray<bool> left, 
        PartitionedSparseArray<bool> right, 
        PartitionedSparseArray<bool> result
    )
    {
        // Process global partition
        And(left.Global, right.Global, result.Global);
        
        // Process scene partition
        And(left.Scene, right.Scene, result.Scene);
    }

    /// <summary>
    /// Bitwise SIMD ORs together 2 partitioned sparse arrays, and writes
    /// the values out the a result array.
    ///
    /// NOTE this does NOT clear the result array before hand, so old values will persist
    /// </summary>
    public static void Or(
        PartitionedSparseArray<bool> left, 
        PartitionedSparseArray<bool> right, 
        PartitionedSparseArray<bool> result
    )
    {
        // Process global partition
        Or(left.Global, right.Global, result.Global);
        
        // Process scene partition
        Or(left.Scene, right.Scene, result.Scene);
    }
}


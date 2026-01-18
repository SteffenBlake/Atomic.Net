using Atomic.Net.MonoGame.Core;

namespace Atomic.Net.MonoGame.Transform;

/// <summary>
/// SIMD-friendly backing storage for cached parent world transforms. Initialized to identity matrix.
/// </summary>
public sealed class ParentWorldTransformStore : 
    ISingleton<ParentWorldTransformStore>,
    IEventHandler<ResetEvent>
{
    internal static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

        Instance ??= new();
    }

    public static ParentWorldTransformStore Instance { get; private set; } = null!;

    public readonly float[] M11 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public readonly float[] M12 = new float[Constants.MaxEntities];
    public readonly float[] M13 = new float[Constants.MaxEntities];
    public readonly float[] M14 = new float[Constants.MaxEntities];
    public readonly float[] M21 = new float[Constants.MaxEntities];
    public readonly float[] M22 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public readonly float[] M23 = new float[Constants.MaxEntities];
    public readonly float[] M24 = new float[Constants.MaxEntities];
    public readonly float[] M31 = new float[Constants.MaxEntities];
    public readonly float[] M32 = new float[Constants.MaxEntities];
    public readonly float[] M33 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];
    public readonly float[] M34 = new float[Constants.MaxEntities];
    public readonly float[] M41 = new float[Constants.MaxEntities];
    public readonly float[] M42 = new float[Constants.MaxEntities];
    public readonly float[] M43 = new float[Constants.MaxEntities];
    public readonly float[] M44 = [.. Enumerable.Repeat(1f, Constants.MaxEntities)];

    public void OnEvent(ResetEvent e)
    {
        Array.Fill(M11, 1f);
        Array.Fill(M12, 0f);
        Array.Fill(M13, 0f);
        Array.Fill(M14, 0f);

        Array.Fill(M21, 0f);
        Array.Fill(M22, 1f);
        Array.Fill(M23, 0f);
        Array.Fill(M24, 0f);

        Array.Fill(M31, 0f);
        Array.Fill(M32, 0f);
        Array.Fill(M33, 1f);
        Array.Fill(M34, 0f);

        Array.Fill(M41, 0f);
        Array.Fill(M42, 0f);
        Array.Fill(M43, 1f);
        Array.Fill(M44, 0f);
    }
}

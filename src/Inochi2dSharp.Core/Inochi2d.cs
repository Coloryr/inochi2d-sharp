using System.Numerics;
using Inochi2dSharp.Core;

namespace Inochi2dSharp;

public static class Inochi2d
{
    public const string Version = "0.8.7";

    public static Mesh ScreenSpaceMesh { get; private set; }

    public static void Init()
    {
        InSetupComposite();
    }

    public static void InSetupComposite()
    {
        ScreenSpaceMesh ??= Mesh.FromMeshData(new MeshData()
        {
            Vertices =
            [
                new Vector2(-1, -1),
                new Vector2(-1,  1),
                new Vector2(1,  -1),
                new Vector2(1,   1)
            ],
            Uvs =
            [
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(1, 1)
            ],
            Indices =
            [
                0, 1, 2,
                2, 1, 3
            ]
        });
    }

    public static void InCleanupComposite()
    {
        ScreenSpaceMesh = null!;
    }
}

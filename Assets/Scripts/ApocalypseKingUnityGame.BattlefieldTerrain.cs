using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class ApocalypseKingUnityGame
{
    private float SampleBattlefieldGroundHeightWorld(float worldX, float worldZ)
    {
        float combatFlatten = Mathf.Exp(-(worldZ * worldZ) / 210f - (worldX * worldX) / 520f);
        float castleFlatten = Mathf.Exp(-Mathf.Pow((Mathf.Abs(worldX) - 56f) / 15f, 2f));

        float nx = worldX * 0.042f;
        float nz = worldZ * 0.036f;
        float rolling = Mathf.Sin(nx * 1.15f + 0.6f) * 0.24f
                      + Mathf.Sin(nz * 0.95f + 1.4f) * 0.20f
                      + Mathf.Sin((nx + nz) * 0.72f + 2.1f) * 0.16f;
        float detail = (Noise(worldX * 2.8f + 41f) - 0.5f) * 0.10f
                     + (Noise(worldZ * 2.5f + 19f) - 0.5f) * 0.08f;
        float micro = (Noise(worldX * 7.4f + worldZ * 5.1f) - 0.5f) * 0.045f
                    + (Noise(worldX * 11.2f - worldZ * 4.6f + 88f) - 0.5f) * 0.028f;

        float height = (rolling + detail + micro) * (1f - combatFlatten * 0.52f - castleFlatten * 0.38f);
        return Mathf.Clamp(height, -0.04f, 0.48f);
    }

    private void CreateGround()
    {
        Color grassTint = new Color(0.66f, 0.78f, 0.50f, 1f);
        Material grassMaterial = Resources.Load<Texture>(GrassDetailTextureResourcePath) != null
            ? GetTexturedOpaqueMaterial(GrassDetailTextureResourcePath, grassTint, new Vector2(26f, 34f), 0.11f)
            : GetTexturedOpaqueMaterial(GrassTextureResourcePath, grassTint, new Vector2(24f, 32f), 0.10f);
        CreateUndulatingGrassTerrain("GrasslandTerrain", new Vector2(150f, 210f), grassMaterial);
    }

    private GameObject CreateUndulatingGrassTerrain(string name, Vector2 worldSize, Material material)
    {
        const int segX = 96;
        const int segZ = 132;
        float halfX = worldSize.x * 0.5f;
        float halfZ = worldSize.y * 0.5f;

        var mesh = new Mesh { name = name + "_Mesh" };
        int vertexCount = (segX + 1) * (segZ + 1);
        var vertices = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];

        for (int z = 0; z <= segZ; z++)
        {
            for (int x = 0; x <= segX; x++)
            {
                int index = z * (segX + 1) + x;
                float worldX = Mathf.Lerp(-halfX, halfX, x / (float)segX);
                float worldZ = Mathf.Lerp(-halfZ, halfZ, z / (float)segZ);
                float height = SampleBattlefieldGroundHeightWorld(worldX, worldZ);
                vertices[index] = new Vector3(worldX, height, worldZ);
                uvs[index] = new Vector2(x / (float)segX * 24f, z / (float)segZ * 32f);
            }
        }

        int triangleCount = segX * segZ * 6;
        var triangles = new int[triangleCount];
        int t = 0;
        for (int z = 0; z < segZ; z++)
        {
            for (int x = 0; x < segX; x++)
            {
                int bottomLeft = z * (segX + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + segX + 1;
                int topRight = topLeft + 1;
                triangles[t++] = bottomLeft;
                triangles[t++] = topLeft;
                triangles[t++] = topRight;
                triangles[t++] = bottomLeft;
                triangles[t++] = topRight;
                triangles[t++] = bottomRight;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var terrainObject = new GameObject(name);
        terrainObject.transform.SetParent(decorRoot, false);
        terrainObject.transform.localPosition = Vector3.zero;
        terrainObject.transform.localRotation = Quaternion.identity;
        terrainObject.transform.localScale = Vector3.one;

        var meshFilter = terrainObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var meshRenderer = terrainObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = true;
        return terrainObject;
    }
}

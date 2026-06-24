using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class VRVisualFixTools
{
    private const string ArrowPrefabPath =
        "Assets/Prefabs/Arrow.prefab";

    private const string MaterialFolder =
        "Assets/Materials_URP";

    private const string ArrowMaterialPath =
        "Assets/Materials_URP/M_Arrow_URP.mat";

    [MenuItem("Tools/VR Project/Fix Arrow Material Automatically")]
    public static void FixArrowMaterialAutomatically()
    {
        try
        {
            EnsureMaterialFolder();

            Shader urpLitShader =
                Shader.Find("Universal Render Pipeline/Lit");

            if (urpLitShader == null)
            {
                throw new InvalidOperationException(
                    "Shader-ul Universal Render Pipeline/Lit nu a fost găsit.");
            }

            Material arrowMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    ArrowMaterialPath);

            if (arrowMaterial == null)
            {
                arrowMaterial = new Material(urpLitShader)
                {
                    name = "M_Arrow_URP"
                };

                AssetDatabase.CreateAsset(
                    arrowMaterial,
                    ArrowMaterialPath);
            }
            else
            {
                arrowMaterial.shader = urpLitShader;
            }

            Texture2D baseMap = LoadTexture(
                "Arrows_Arrows_AlbedoTransparency");

            Texture2D metallicMap = LoadTexture(
                "Arrows_Arrows_MetallicSmoothness");

            Texture2D normalMap = LoadTexture(
                "Arrows_Arrows_Normal",
                configureAsNormalMap: true);

            Texture2D occlusionMap = LoadTexture(
                "Arrows_Arrows_AO");

            AssignTexture(
                arrowMaterial,
                "_BaseMap",
                baseMap);

            if (arrowMaterial.HasProperty("_BaseColor"))
            {
                arrowMaterial.SetColor(
                    "_BaseColor",
                    Color.white);
            }

            AssignTexture(
                arrowMaterial,
                "_MetallicGlossMap",
                metallicMap);

            if (metallicMap != null)
            {
                arrowMaterial.EnableKeyword(
                    "_METALLICSPECGLOSSMAP");
            }

            AssignTexture(
                arrowMaterial,
                "_BumpMap",
                normalMap);

            if (normalMap != null)
            {
                arrowMaterial.EnableKeyword(
                    "_NORMALMAP");
            }

            AssignTexture(
                arrowMaterial,
                "_OcclusionMap",
                occlusionMap);

            if (occlusionMap != null)
            {
                arrowMaterial.EnableKeyword(
                    "_OCCLUSIONMAP");
            }

            if (arrowMaterial.HasProperty("_Metallic"))
            {
                arrowMaterial.SetFloat(
                    "_Metallic",
                    1.0f);
            }

            if (arrowMaterial.HasProperty("_Smoothness"))
            {
                arrowMaterial.SetFloat(
                    "_Smoothness",
                    0.45f);
            }

            EditorUtility.SetDirty(arrowMaterial);
            AssetDatabase.SaveAssets();

            ApplyMaterialToArrowPrefab(
                arrowMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject arrowPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ArrowPrefabPath);

            Selection.activeObject = arrowPrefab;
            EditorGUIUtility.PingObject(arrowPrefab);

            Debug.Log(
                "[VR Visual Fix] Materialul săgeții a fost reparat și aplicat.");

            EditorUtility.DisplayDialog(
                "Arrow material fixed",
                "Materialul URP a fost creat și aplicat automat tuturor pieselor vizuale ale săgeții.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Arrow material fix failed",
                exception.Message +
                "\n\nVerifică Unity Console pentru detalii.",
                "OK");
        }
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "Materials_URP");
        }
    }

    private static Texture2D LoadTexture(
        string exactTextureName,
        bool configureAsNormalMap = false)
    {
        string texturePath =
            FindTexturePath(exactTextureName);

        if (configureAsNormalMap)
        {
            ConfigureTextureAsNormalMap(
                texturePath);
        }

        Texture2D texture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                texturePath);

        if (texture == null)
        {
            throw new InvalidOperationException(
                $"Textura nu a putut fi încărcată: {texturePath}");
        }

        return texture;
    }

    private static string FindTexturePath(
        string exactTextureName)
    {
        string[] textureGuids =
            AssetDatabase.FindAssets(
                exactTextureName + " t:Texture2D");

        foreach (string guid in textureGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            string fileName =
                Path.GetFileNameWithoutExtension(path);

            if (string.Equals(
                fileName,
                exactTextureName,
                StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        throw new InvalidOperationException(
            "Nu am găsit textura: " +
            exactTextureName);
    }

    private static void ConfigureTextureAsNormalMap(
        string texturePath)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(texturePath)
            as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException(
                "Nu am putut accesa import settings pentru: " +
                texturePath);
        }

        if (importer.textureType !=
            TextureImporterType.NormalMap)
        {
            importer.textureType =
                TextureImporterType.NormalMap;

            importer.SaveAndReimport();
        }
    }

    private static void AssignTexture(
        Material material,
        string propertyName,
        Texture texture)
    {
        if (
            texture != null &&
            material.HasProperty(propertyName)
        )
        {
            material.SetTexture(
                propertyName,
                texture);
        }
    }

    private static void ApplyMaterialToArrowPrefab(
        Material arrowMaterial)
    {
        GameObject prefabRoot =
            PrefabUtility.LoadPrefabContents(
                ArrowPrefabPath);

        if (prefabRoot == null)
        {
            throw new InvalidOperationException(
                "Prefab-ul săgeții nu a putut fi deschis: " +
                ArrowPrefabPath);
        }

        try
        {
            Transform arrowVisual =
                prefabRoot
                    .GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(
                        item => item.name == "ArrowVisual");

            Transform visualRoot =
                arrowVisual != null
                    ? arrowVisual
                    : prefabRoot.transform;

            Renderer[] renderers =
                visualRoot
                    .GetComponentsInChildren<Renderer>(true)
                    .Where(
                        renderer =>
                            renderer is MeshRenderer ||
                            renderer is SkinnedMeshRenderer)
                    .ToArray();

            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Nu am găsit niciun Mesh Renderer în ArrowVisual.");
            }

            foreach (Renderer renderer in renderers)
            {
                Material[] slots =
                    renderer.sharedMaterials;

                if (
                    slots == null ||
                    slots.Length == 0
                )
                {
                    slots = new Material[1];
                }

                for (
                    int index = 0;
                    index < slots.Length;
                    index++
                )
                {
                    slots[index] = arrowMaterial;
                }

                renderer.sharedMaterials = slots;
                EditorUtility.SetDirty(renderer);
            }

            PrefabUtility.SaveAsPrefabAsset(
                prefabRoot,
                ArrowPrefabPath);

            Debug.Log(
                $"[VR Visual Fix] Material aplicat pe {renderers.Length} renderer(e).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(
                prefabRoot);
        }
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VRBowVisualFixTools
{
    private const string MaterialFolder =
        "Assets/Materials_URP";

    private const string BowMaterialPath =
        "Assets/Materials_URP/M_Bow_URP.mat";

    private const string BowVisualName =
        "BowVisual";

    [MenuItem("Tools/VR Project/Add Real Bow Visual Automatically")]
    public static void AddRealBowVisualAutomatically()
    {
        try
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Oprește Play Mode înainte să modifici arcul.");
            }

            Scene scene = SceneManager.GetActiveScene();

            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Nu există nicio scenă activă.");
            }

            if (!scene.name.Contains("ForestArcheryScene"))
            {
                throw new InvalidOperationException(
                    "Deschide mai întâi ForestArcheryScene.");
            }

            GameObject bowRoot = FindSceneObject(scene, "Bow");

            if (bowRoot == null)
            {
                throw new InvalidOperationException(
                    "Nu am găsit obiectul funcțional numit Bow.");
            }

            GameObject bowPrefab = FindWoodenBowPrefab();

            Material bowMaterial = CreateOrUpdateBowMaterial();

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Real Bow Visual");

            Transform existingVisual =
                bowRoot.transform.Find(BowVisualName);

            if (existingVisual != null)
            {
                Undo.DestroyObjectImmediate(
                    existingVisual.gameObject);
            }

            GameObject bowVisual =
                PrefabUtility.InstantiatePrefab(
                    bowPrefab,
                    bowRoot.transform) as GameObject;

            if (bowVisual == null)
            {
                throw new InvalidOperationException(
                    "Wooden Bow.prefab nu a putut fi instanțiat.");
            }

            Undo.RegisterCreatedObjectUndo(
                bowVisual,
                "Create BowVisual");

            bowVisual.name = BowVisualName;

            Transform visualTransform =
                bowVisual.transform;

            visualTransform.localPosition =
                Vector3.zero;

            // Modelul real este vertical, dar planul arcului
            // trebuie aliniat cu direcția săgeții din baseline.
            visualTransform.localRotation =
                Quaternion.Euler(0f, 90f, 0f);

            visualTransform.localScale =
                Vector3.one;

            SetLayerRecursively(
                bowVisual,
                bowRoot.layer);

            DisableDuplicatePhysics(
                bowVisual);

            ApplyMaterial(
                bowVisual,
                bowMaterial);

            FitVisualToFunctionalBow(
                bowRoot,
                bowVisual);

            HideOldCylinderRenderer(
                bowRoot);

            EditorUtility.SetDirty(
                bowRoot);

            EditorUtility.SetDirty(
                bowVisual);

            EditorSceneManager.MarkSceneDirty(
                scene);

            EditorSceneManager.SaveScene(
                scene);

            Undo.CollapseUndoOperations(
                undoGroup);

            Selection.activeGameObject =
                bowVisual;

            EditorGUIUtility.PingObject(
                bowVisual);

            Debug.Log(
                "[VR Bow Fix] Arcul real a fost adăugat. " +
                "Cilindrul a fost ascuns, iar mecanica originală a fost păstrată.");

            EditorUtility.DisplayDialog(
                "Real bow added",
                "Arcul real a fost adăugat automat.\n\n" +
                "Au fost păstrate:\n" +
                "• Rigidbody-ul original\n" +
                "• collider-ele originale\n" +
                "• grab-ul pentru mâini și controllere\n" +
                "• BowDrawController\n" +
                "• ArrowSpawnPoint\n" +
                "• punctele corzii\n\n" +
                "Cilindrul a fost doar ascuns vizual.",
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Bow visual fix failed",
                exception.Message +
                "\n\nVerifică Unity Console.",
                "OK");
        }
    }

    [MenuItem("Tools/VR Project/Flip Real Bow Visual")]
    public static void FlipRealBowVisual()
    {
        Scene scene =
            SceneManager.GetActiveScene();

        GameObject bowRoot =
            FindSceneObject(scene, "Bow");

        if (bowRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Bow not found",
                "Nu am găsit obiectul Bow.",
                "OK");

            return;
        }

        Transform bowVisual =
            bowRoot.transform.Find(
                BowVisualName);

        if (bowVisual == null)
        {
            EditorUtility.DisplayDialog(
                "BowVisual not found",
                "Rulează mai întâi Add Real Bow Visual Automatically.",
                "OK");

            return;
        }

        Undo.RecordObject(
            bowVisual,
            "Flip BowVisual");

        bowVisual.Rotate(
            0f,
            180f,
            0f,
            Space.Self);

        EditorSceneManager.MarkSceneDirty(
            scene);

        EditorSceneManager.SaveScene(
            scene);

        Debug.Log(
            "[VR Bow Fix] BowVisual a fost rotit cu 180 de grade.");
    }

    [MenuItem("Tools/VR Project/Restore Cylinder Bow")]
    public static void RestoreCylinderBow()
    {
        Scene scene =
            SceneManager.GetActiveScene();

        GameObject bowRoot =
            FindSceneObject(scene, "Bow");

        if (bowRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Bow not found",
                "Nu am găsit obiectul Bow.",
                "OK");

            return;
        }

        Transform bowVisual =
            bowRoot.transform.Find(
                BowVisualName);

        if (bowVisual != null)
        {
            Undo.DestroyObjectImmediate(
                bowVisual.gameObject);
        }

        Transform oldBowMesh =
            FindChildRecursive(
                bowRoot.transform,
                "BowMesh");

        if (oldBowMesh != null)
        {
            Renderer[] oldRenderers =
                oldBowMesh.GetComponentsInChildren<Renderer>(
                    true);

            foreach (Renderer renderer in oldRenderers)
            {
                Undo.RecordObject(
                    renderer,
                    "Restore old bow renderer");

                renderer.enabled = true;
            }
        }

        EditorSceneManager.MarkSceneDirty(
            scene);

        EditorSceneManager.SaveScene(
            scene);

        Debug.Log(
            "[VR Bow Fix] Arcul real a fost eliminat și cilindrul restaurat.");
    }

    private static GameObject FindSceneObject(
        Scene scene,
        string exactName)
    {
        return Resources
            .FindObjectsOfTypeAll<GameObject>()
            .Where(
                item =>
                    item.scene == scene &&
                    item.name == exactName &&
                    item.hideFlags == HideFlags.None)
            .OrderByDescending(
                item => item.activeInHierarchy)
            .FirstOrDefault();
    }

    private static GameObject FindWoodenBowPrefab()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "Wooden Bow t:Prefab");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (
                path.EndsWith(
                    "Free medieval weapons/Prefabs/Wooden Bow.prefab",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        path);

                if (prefab != null)
                {
                    return prefab;
                }
            }
        }

        throw new InvalidOperationException(
            "Nu am găsit Assets/Free medieval weapons/Prefabs/Wooden Bow.prefab.");
    }

    private static Material CreateOrUpdateBowMaterial()
    {
        EnsureMaterialFolder();

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (shader == null)
        {
            throw new InvalidOperationException(
                "Shader-ul Universal Render Pipeline/Lit nu a fost găsit.");
        }

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                BowMaterialPath);

        if (material == null)
        {
            material =
                new Material(shader)
                {
                    name = "M_Bow_URP"
                };

            AssetDatabase.CreateAsset(
                material,
                BowMaterialPath);
        }
        else
        {
            material.shader =
                shader;
        }

        Texture2D baseMap =
            LoadTexture(
                "Wooden Bow_1_Wooden Bow_1_AlbedoTransparency");

        Texture2D metallicMap =
            LoadTexture(
                "Wooden Bow_1_Wooden Bow_1_MetallicSmoothness");

        Texture2D normalMap =
            LoadTexture(
                "Wooden Bow_1_Wooden Bow_1_Normal",
                configureAsNormalMap: true);

        Texture2D occlusionMap =
            LoadTexture(
                "Wooden Bow_1_Wooden Bow_1_AO");

        AssignTexture(
            material,
            "_BaseMap",
            baseMap);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                Color.white);
        }

        AssignTexture(
            material,
            "_MetallicGlossMap",
            metallicMap);

        if (metallicMap != null)
        {
            material.EnableKeyword(
                "_METALLICSPECGLOSSMAP");
        }

        AssignTexture(
            material,
            "_BumpMap",
            normalMap);

        if (normalMap != null)
        {
            material.EnableKeyword(
                "_NORMALMAP");
        }

        AssignTexture(
            material,
            "_OcclusionMap",
            occlusionMap);

        if (occlusionMap != null)
        {
            material.EnableKeyword(
                "_OCCLUSIONMAP");
        }

        // Lemnul nu trebuie să pară metalic.
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat(
                "_Metallic",
                0f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat(
                "_Smoothness",
                0.35f);
        }

        EditorUtility.SetDirty(
            material);

        AssetDatabase.SaveAssets();

        return material;
    }

    private static void ApplyMaterial(
        GameObject bowVisual,
        Material material)
    {
        Renderer[] renderers =
            bowVisual.GetComponentsInChildren<Renderer>(
                true);

        if (renderers.Length == 0)
        {
            throw new InvalidOperationException(
                "Wooden Bow.prefab nu conține niciun Renderer.");
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
                slots =
                    new Material[1];
            }

            for (
                int index = 0;
                index < slots.Length;
                index++
            )
            {
                slots[index] =
                    material;
            }

            renderer.sharedMaterials =
                slots;

            EditorUtility.SetDirty(
                renderer);
        }
    }

    private static void FitVisualToFunctionalBow(
        GameObject bowRoot,
        GameObject bowVisual)
    {
        BoxCollider functionalCollider =
            bowRoot.GetComponent<BoxCollider>();

        float targetHeight =
            functionalCollider != null
                ? functionalCollider.size.y
                : 0.86f;

        Vector3 targetCenter =
            functionalCollider != null
                ? functionalCollider.center
                : Vector3.zero;

        Renderer[] renderers =
            bowVisual.GetComponentsInChildren<Renderer>(
                true);

        Bounds initialBounds =
            CalculateBoundsInLocalSpace(
                bowRoot.transform,
                renderers);

        if (initialBounds.size.y < 0.0001f)
        {
            throw new InvalidOperationException(
                "Nu am putut calcula înălțimea modelului de arc.");
        }

        float uniformScale =
            targetHeight /
            initialBounds.size.y;

        bowVisual.transform.localScale =
            Vector3.one * uniformScale;

        Bounds scaledBounds =
            CalculateBoundsInLocalSpace(
                bowRoot.transform,
                renderers);

        Vector3 correction =
            targetCenter -
            scaledBounds.center;

        bowVisual.transform.localPosition +=
            correction;
    }

    private static Bounds CalculateBoundsInLocalSpace(
        Transform reference,
        Renderer[] renderers)
    {
        bool initialized =
            false;

        Bounds localBounds =
            new Bounds();

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
            {
                continue;
            }

            Bounds worldBounds =
                renderer.bounds;

            Vector3 center =
                worldBounds.center;

            Vector3 extents =
                worldBounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldCorner =
                            center +
                            Vector3.Scale(
                                extents,
                                new Vector3(x, y, z));

                        Vector3 localCorner =
                            reference.InverseTransformPoint(
                                worldCorner);

                        if (!initialized)
                        {
                            localBounds =
                                new Bounds(
                                    localCorner,
                                    Vector3.zero);

                            initialized =
                                true;
                        }
                        else
                        {
                            localBounds.Encapsulate(
                                localCorner);
                        }
                    }
                }
            }
        }

        if (!initialized)
        {
            throw new InvalidOperationException(
                "Nu există renderere active în modelul arcului.");
        }

        return localBounds;
    }

    private static void HideOldCylinderRenderer(
        GameObject bowRoot)
    {
        Transform oldBowMesh =
            FindChildRecursive(
                bowRoot.transform,
                "BowMesh");

        if (oldBowMesh == null)
        {
            Debug.LogWarning(
                "[VR Bow Fix] Nu am găsit BowMesh. " +
                "Arcul real a fost adăugat, dar cilindrul nu a fost ascuns automat.");

            return;
        }

        Renderer[] oldRenderers =
            oldBowMesh.GetComponentsInChildren<Renderer>(
                true);

        foreach (Renderer renderer in oldRenderers)
        {
            Undo.RecordObject(
                renderer,
                "Hide old cylinder renderer");

            // Ascundem doar aspectul.
            // Collider-ul BowMesh rămâne activ pentru interacțiune.
            renderer.enabled =
                false;

            EditorUtility.SetDirty(
                renderer);
        }
    }

    private static void DisableDuplicatePhysics(
        GameObject bowVisual)
    {
        Collider[] colliders =
            bowVisual.GetComponentsInChildren<Collider>(
                true);

        foreach (Collider collider in colliders)
        {
            collider.enabled =
                false;
        }

        Rigidbody[] rigidbodies =
            bowVisual.GetComponentsInChildren<Rigidbody>(
                true);

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.useGravity =
                false;

            rigidbody.isKinematic =
                true;

            rigidbody.detectCollisions =
                false;
        }
    }

    private static Transform FindChildRecursive(
        Transform root,
        string exactName)
    {
        foreach (
            Transform child in
            root.GetComponentsInChildren<Transform>(true)
        )
        {
            if (child.name == exactName)
            {
                return child;
            }
        }

        return null;
    }

    private static void SetLayerRecursively(
        GameObject root,
        int layer)
    {
        root.layer =
            layer;

        foreach (
            Transform child in
            root.transform
        )
        {
            SetLayerRecursively(
                child.gameObject,
                layer);
        }
    }

    private static void EnsureMaterialFolder()
    {
        if (!AssetDatabase.IsValidFolder(
            MaterialFolder))
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
        string path =
            FindTexturePath(
                exactTextureName);

        if (configureAsNormalMap)
        {
            ConfigureTextureAsNormalMap(
                path);
        }

        Texture2D texture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                path);

        if (texture == null)
        {
            throw new InvalidOperationException(
                "Textura nu a putut fi încărcată: " +
                path);
        }

        return texture;
    }

    private static string FindTexturePath(
        string exactTextureName)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                exactTextureName +
                " t:Texture2D");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            string fileName =
                Path.GetFileNameWithoutExtension(
                    path);

            if (
                string.Equals(
                    fileName,
                    exactTextureName,
                    StringComparison.OrdinalIgnoreCase)
            )
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
            AssetImporter.GetAtPath(
                texturePath) as TextureImporter;

        if (importer == null)
        {
            throw new InvalidOperationException(
                "Nu am putut accesa import settings pentru: " +
                texturePath);
        }

        if (
            importer.textureType !=
            TextureImporterType.NormalMap
        )
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
}

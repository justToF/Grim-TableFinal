using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ConvertHqExplosionsToUrp
{
    private const string TargetFolder = "Assets/HQ Explosions Pack FREE/Materials";
    private const string PrefabFolder = "Assets/HQ Explosions Pack FREE/Prefabs";
    private const string TargetShaderName = "Universal Render Pipeline/Particles/Unlit";

    [MenuItem("Tools/Grim Table/Convert HQ Explosions To URP")]
    public static void Convert()
    {
        Shader urpShader = Shader.Find(TargetShaderName);
        if (urpShader == null)
        {
            Debug.LogError("URP shader not found: " + TargetShaderName);
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { TargetFolder });
        int convertedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            Texture baseTex = null;
            if (mat.HasProperty("_Explosion_Tex"))
                baseTex = mat.GetTexture("_Explosion_Tex");
            else if (mat.HasProperty("_BaseMap"))
                baseTex = mat.GetTexture("_BaseMap");
            else if (mat.mainTexture != null)
                baseTex = mat.mainTexture;

            mat.shader = urpShader;

            if (baseTex != null && mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", baseTex);

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", 1f);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", 1f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            mat.renderQueue = (int)RenderQueue.Transparent;

            EditorUtility.SetDirty(mat);
            convertedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        int fixedPrefabs = FixExplosionPrefabsFlipbook();
        Debug.Log("HQ Explosions conversion complete. Materials converted: " + convertedCount + ", prefabs fixed: " + fixedPrefabs);
    }

    private static int FixExplosionPrefabsFlipbook()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        int fixedCount = 0;

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            bool changed = false;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in systems)
            {
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer == null || renderer.sharedMaterial == null) continue;

                Material mat = renderer.sharedMaterial;
                string matPath = AssetDatabase.GetAssetPath(mat);
                bool isExplosionMaterial = mat.name.ToLower().Contains("explosion");
                bool inPackMaterials = !string.IsNullOrEmpty(matPath) && matPath.StartsWith(TargetFolder);
                if (!isExplosionMaterial && !inPackMaterials) continue;

                var uv = ps.textureSheetAnimation;
                uv.enabled = true;
                uv.mode = ParticleSystemAnimationMode.Grid;
                uv.animation = ParticleSystemAnimationType.WholeSheet;
                uv.numTilesX = 8;
                uv.numTilesY = 8;
                uv.frameOverTime = new ParticleSystem.MinMaxCurve(1f);

                renderer.enableGPUInstancing = false;
                renderer.SetActiveVertexStreams(null);

                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                fixedCount++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return fixedCount;
    }
}

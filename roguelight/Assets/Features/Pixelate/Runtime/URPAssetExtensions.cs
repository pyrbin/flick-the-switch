using System.Reflection;
using UnityEngine.Rendering.Universal;

public static class URPAssetExtensions
{
    public static Option<T> GetRenderFeature<T>(this UniversalRenderPipelineAsset urpAsset)
        where T : ScriptableRendererFeature
    {
        var type = urpAsset.GetType();
        var propertyInfo = type.GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);

        if (propertyInfo == null) return Option<T>.None;

        var scriptableRenderData = (ScriptableRendererData[])propertyInfo.GetValue(urpAsset);

        if (scriptableRenderData == null || scriptableRenderData.Length <= 0) return Option<T>.None;

        foreach(var renderData in scriptableRenderData)
        {
            foreach(var rendererFeature in renderData.rendererFeatures)
            {
                var feature = rendererFeature as T;
                if (feature == null) continue;

                return feature;
            }
        }

        return Option<T>.None;
    }
}

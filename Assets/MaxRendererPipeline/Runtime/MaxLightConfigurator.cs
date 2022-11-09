using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;


namespace MaxSRP
{
    public class MaxLightConfigurator
    {
        public class ShaderProperties
        {
            public static int AmbientColor = Shader.PropertyToID("_AmbientColor");

            // 方向光
            public static int DirectionalLightDirection = Shader.PropertyToID("_MaxDirectionalLightDirection");
            public static int DirectionalLightColor = Shader.PropertyToID("_MaxDirectionalLightColor");

            // 点光源
            public static int OtherLightPositionAndRanges = Shader.PropertyToID("_MaxOtherLightPositionAndRanges");
            public static int OtherLightColors = Shader.PropertyToID("_MaxOtherLightColors");
        }

        private int _mainLightIndex = -1;
        private const int MAX_VISIBLE_OTHER_LIGHTS = 32;
        private Vector4[] _otherLightPositionAndRanges = new Vector4[MAX_VISIBLE_OTHER_LIGHTS];
        private Vector4[] _otherLightColors = new Vector4[MAX_VISIBLE_OTHER_LIGHTS];

        private static int CompareLightRenderMode(LightRenderMode m1, LightRenderMode m2)
        {
            if (m1 == m2)
            {
                return 0;
            }
            if (m1 == LightRenderMode.ForcePixel)
            {
                return -1;
            }
            if (m2 == LightRenderMode.ForcePixel)
            {
                return 1;
            }
            if (m1 == LightRenderMode.Auto)
            {
                return -1;
            }
            if (m2 == LightRenderMode.Auto)
            {
                return 1;
            }
            return 0;
        }
        /// <summary>
        /// 如果有多个平行光，按LightRenderMode、intensity对其排序
        /// </summary>
        private static int CompareLight(Light l1, Light l2)
        {
            if (l1.renderMode == l2.renderMode)
            {
                return (int)Mathf.Sign(l2.intensity - l1.intensity);
            }
            var ret = CompareLightRenderMode(l1.renderMode, l2.renderMode);
            if (ret == 0)
            {
                ret = (int)Mathf.Sign(l2.intensity - l1.intensity);
            }
            return ret;
        }
        private static int GetMainLightIndex(NativeArray<VisibleLight> lights)
        {
            Light mainLight = null;
            var mainLightIndex = -1;
            var index = 0;
            foreach (var light in lights)
            {
                if (light.lightType == LightType.Directional)
                {
                    var lightComp = light.light;
                    if (lightComp.renderMode == LightRenderMode.ForceVertex)
                    {
                        continue;
                    }
                    if (!mainLight)
                    {
                        mainLight = lightComp;
                        mainLightIndex = index;
                    }
                    else
                    {
                        if (CompareLight(mainLight, lightComp) > 0)
                        {
                            mainLight = lightComp;
                            mainLightIndex = index;
                        }
                    }
                }
                index++;
            }
            return mainLightIndex;
        }

        public LightData SetupMultiShaderLightingParams(ref CullingResults cullingResults)
        {
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            _mainLightIndex = GetMainLightIndex(visibleLights);
            if (_mainLightIndex >= 0)
            {
                var mainLight = visibleLights[_mainLightIndex];
                var forward = -(Vector4)mainLight.light.gameObject.transform.forward;
                Shader.SetGlobalVector(ShaderProperties.DirectionalLightDirection, forward);
                Shader.SetGlobalColor(ShaderProperties.DirectionalLightColor, mainLight.finalColor);
            }
            else
            {
                Shader.SetGlobalColor(ShaderProperties.DirectionalLightColor, new Color(0, 0, 0, 0));
            }

            SetupOtherLightDatas(ref cullingResults);

            Shader.SetGlobalColor(ShaderProperties.AmbientColor, RenderSettings.ambientLight);

            LightData ld = new LightData()
            {
                mainLight = _mainLightIndex >= 0 && _mainLightIndex < visibleLights.Length? visibleLights[_mainLightIndex] :default(VisibleLight),
                mainLightIndex = _mainLightIndex
            };
            return ld;
        }

        private void SetPointLightData(int index, ref VisibleLight light)
        {
            Vector4 positionAndRange = light.light.gameObject.transform.position;
            positionAndRange.w = light.range;
            _otherLightPositionAndRanges[index] = positionAndRange;
            _otherLightColors[index] = light.finalColor;
        }

        //设置非平行光源的GPU数据
        private void SetupOtherLightDatas(ref CullingResults cullingResults)
        {
            var visibleLights = cullingResults.visibleLights;
            var lightMapIndex = cullingResults.GetLightIndexMap(Allocator.Temp);
            var otherLightIndex = 0;
            var visibleLightIndex = 0;
            foreach (var l in visibleLights)
            {
                var visibleLight = l;
                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        lightMapIndex[visibleLightIndex] = -1;
                        break;
                    case LightType.Point:
                        lightMapIndex[visibleLightIndex] = otherLightIndex;
                        SetPointLightData(otherLightIndex, ref visibleLight);
                        otherLightIndex++;
                        break;
                    default:
                        lightMapIndex[visibleLightIndex] = -1;
                        break;
                }
                visibleLightIndex++;
            }
            for (var i = visibleLightIndex; i < lightMapIndex.Length; i++)
            {
                lightMapIndex[i] = -1;
            }
            cullingResults.SetLightIndexMap(lightMapIndex);
            Shader.SetGlobalVectorArray(ShaderProperties.OtherLightPositionAndRanges, _otherLightPositionAndRanges);
            Shader.SetGlobalVectorArray(ShaderProperties.OtherLightColors, _otherLightColors);
        }
    }

    public struct LightData
    {
        public int mainLightIndex;
        public VisibleLight mainLight;

        public bool HasMainLight()
        {
            return mainLightIndex >= 0;
        }
    }
}

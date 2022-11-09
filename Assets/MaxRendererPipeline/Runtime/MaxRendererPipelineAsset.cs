using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

namespace MaxSRP
{
    [CreateAssetMenu(menuName = "MaxSRP/MaxRendererPipelineAsset")]
    public class MaxRendererPipelineAsset : RenderPipelineAsset
    {
        [SerializeField]
        private bool _srpBatcher = true;

        [SerializeField]
        private ShadowSetting _shadowSetting = new ShadowSetting();

        public bool enableSrpBatcher
        {
            get
            {
                return _srpBatcher;
            }
        }
        public ShadowSetting shadowSetting
        {
            get
            {
                return _shadowSetting;
            }
        }

        protected override RenderPipeline CreatePipeline()
        {
            return new MaxRenderPipeline(this);
        }
    }


    public class MaxRenderPipeline : RenderPipeline
    {

        private ShaderTagId _shaderTag = new ShaderTagId("MaxForwardBase");
        private MaxLightConfigurator _lightConfigurator = new MaxLightConfigurator();

        private MaxRenderObjectPass _opaquePass = new MaxRenderObjectPass(false);
        private MaxRenderObjectPass _transparentPass = new MaxRenderObjectPass(true);

        private MaxShadowCasterPass _shadowCastPass = new MaxShadowCasterPass();
        private CommandBuffer _command = new CommandBuffer();

        private MaxRendererPipelineAsset _setting;
        public MaxRenderPipeline(MaxRendererPipelineAsset setting)
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = setting.enableSrpBatcher;
            _command.name = "RenderCamera";
            this._setting = setting;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            //遍历摄像机，进行渲染
            foreach(var camera in cameras){
                RenderPerCamera(context,camera);
            }
            //提交渲染命令
            context.Submit();
        }


        private void ClearCameraTarget(ScriptableRenderContext context,Camera camera)
        {
            _command.Clear();
            _command.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,BuiltinRenderTextureType.CameraTarget);
            _command.ClearRenderTarget(true,true,camera.backgroundColor);
            context.ExecuteCommandBuffer(_command);
        }

        private void RenderPerCamera(ScriptableRenderContext context,Camera camera)
        {

            //设置摄像机参数
            context.SetupCameraProperties(camera);
            //对场景进行裁剪
            camera.TryGetCullingParameters(out var cullingParams);
            cullingParams.shadowDistance = Mathf.Min(_setting.shadowSetting.shadowDistance, camera.farClipPlane - camera.nearClipPlane);
            var cullingResults = context.Cull(ref cullingParams);
            var lightData = _lightConfigurator.SetupMultiShaderLightingParams(ref cullingResults);

            var casterSetting = new MaxShadowCasterPass.ShadowCasterSetting();
            casterSetting.cullingResults = cullingResults;
            casterSetting.lightData = lightData;
            casterSetting.shadowSetting = _setting.shadowSetting;

            //投影Pass
            _shadowCastPass.Execute(context, casterSetting);

            //重设摄像机参数
            context.SetupCameraProperties(camera);

            //清除摄像机背景
            ClearCameraTarget(context,camera);

            //非透明物体渲染
            _opaquePass.Execute(context, camera, ref cullingResults);

            context.DrawSkybox(camera);

            //透明物体渲染
            _transparentPass.Execute(context, camera, ref cullingResults);

        }

        private DrawingSettings CreateDrawSettings(Camera camera)
        {
            var sortingSetting = new SortingSettings(camera);
            var drawSetting = new DrawingSettings(_shaderTag,sortingSetting);

            //enable PerObjectLight
            drawSetting.perObjectData |= PerObjectData.LightData;
            drawSetting.perObjectData |= PerObjectData.LightIndices;
            return drawSetting;
        }

    }
}

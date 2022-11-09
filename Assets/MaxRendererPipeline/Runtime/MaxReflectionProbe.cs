using System.Collections;
using UnityEngine;

namespace MaxSRP
{
    public class MaxReflectionProbe : MonoBehaviour
    {
        [SerializeField]
        public Cubemap GroundTruthCubemap;
        [SerializeField]
        public Texture2D BRDFLUT;

        [SerializeField]
        private Cubemap cubemap;

        const string destFolder = "Assets/Scenes/ReflectionProbeCubemaps";

        public void RenderCubeMap()
        {
            cubemap = new Cubemap(64, TextureFormat.RGBA32, false);

            /*Camera staticCam = new Camera
            {
                cameraType = CameraType.Reflection,
                cullingMask = 2
            };*/
            GameObject go = new GameObject("CubemapCamera");
            go.AddComponent<Camera>();
            go.transform.position = this.transform.position;
            go.transform.rotation = Quaternion.identity;
            go.GetComponent<Camera>().RenderToCubemap(cubemap);

            Debug.Log(cubemap.mipmapCount.ToString());

            DestroyImmediate(go);
        }
        public void SaveCubeMap()
        {
            if (cubemap == null)
            {
                Debug.LogError("先渲染Cubemap再保存！");
                return;
            }

            SaveCubemapFace(CubemapFace.NegativeX, "right", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.PositiveX, "left", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.PositiveZ, "front", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.NegativeZ, "back", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.PositiveY, "top", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.NegativeY, "bottom", "jpg", destFolder);
        }

        public void Init()
        {
            Shader.SetGlobalTexture("_BRDFLUT", BRDFLUT);

            RenderCubeMap();
            //SaveCubeMap();
            SubmitReflectionMap();
        }

        public void SubmitReflectionMap()
        {
            Shader.SetGlobalTexture("_IBLSpec", cubemap);
        }

        void SaveCubemapFace(CubemapFace face, string prefix, string type, string destFolder)
        {
            int width = cubemap.width;
            int height = cubemap.height;
            Texture2D tex = new Texture2D(width, height, cubemap.format, 0, false);

            // we need to flip our textures on Y before saving
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, height - 1 - y, cubemap.GetPixel(face, x, y));
                }
            }

            tex.Apply();
            byte[] data = null;
            if (type == "png")
            {
                data = tex.EncodeToPNG();
            }
            else if (type == "jpg")
            {
                data = tex.EncodeToJPG(100);
            }

            if (data != null)
            {
                System.IO.File.WriteAllBytes(destFolder + "/" + prefix + "." + type, data);
            }
            else
            {
                Debug.LogError("[SaveCubemapFace] - failed to encode tex2d");
            }
        }

        public void Clear()
        {
            Destroy(cubemap);
        }

        private void Start()
        {
            Init();
        }

        private void FixedUpdate()
        {
            RenderCubeMap();
            SubmitReflectionMap();
        }
    }
}
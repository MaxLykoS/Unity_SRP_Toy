using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxSRP
{
    public class MaxLightProbe : MonoBehaviour
    {
        //private Material viewMat;

        Vector4[] coefficients = new Vector4[9];
        [SerializeField]
        public Cubemap GroundTruthCubemap;

        private Cubemap cubemap;

        const string destFolder = "Assets/Scenes/LightProbeCubemaps";

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

            DestroyImmediate(go);
        }
        public void SaveCubeMap()
        {
            if (cubemap == null)
            {
                Debug.LogError("œ»‰÷»æCubemap‘Ÿ±£¥Ê£°");
                return;
            }
            SaveCubemapFace(CubemapFace.NegativeX, "right", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.PositiveX, "left", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.PositiveZ, "front", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.NegativeZ, "back", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.PositiveY, "top", "jpg", destFolder);
            SaveCubemapFace(CubemapFace.NegativeY, "bottom", "jpg", destFolder);
        }

        public void ComputeSH()
        {
            SphericalHarmonics.GPU_Project_Uniform_9Coeff(cubemap, coefficients);
        }
        public void ComputeSHCustom()
        {
            SphericalHarmonics.GPU_Project_Uniform_9Coeff(GroundTruthCubemap, coefficients);
        }

        public void Submiit()
        {
            for (int i = 0; i < 9; ++i)
            {
                Shader.SetGlobalVector("c" + i.ToString(), coefficients[i]);
            }
            UnityEditor.SceneView.RepaintAll();
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

        public void Display()
        {
            for (int i = 0; i < 9; ++i)
                Debug.Log(coefficients[i]);
        }

        public void Clear()
        {
            for (int i = 0; i < coefficients.Length; ++i)
                coefficients[i] = Vector3.zero;

            Destroy(cubemap);
        }

        public void UpdateSHDiffuse()
        {
            /*if (viewMat == null)
                viewMat = new Material(Shader.Find("MaxSRP/CoeffVisualizer"));*/

            RenderCubeMap();
            ComputeSH();
            //SaveCubeMap();

            /*for (int i = 0; i < 9; ++i)
            {
                viewMat.SetVector("c" + i.ToString(), coefficients[i]);
                viewMat.SetTexture("input", GroundTruthCubemap);
            }
            viewMat.SetFloat("_Mode", 1.0f);
            RenderSettings.skybox = viewMat;*/

            Submiit();

            for (int i = 0; i < coefficients.Length; ++i)
                coefficients[i] = Vector4.zero;
        }

        private void Awake()
        {
            UpdateSHDiffuse();
        }

        private void FixedUpdate()
        {
            UpdateSHDiffuse();
        }
    }
}
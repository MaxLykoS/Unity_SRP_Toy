Shader "MaxSRP/PBRLitTransparent"
{
    Properties
    {
        _AlbedoMap ("Main Tex", 2D) = "white" {}
        _NormalMap ("Normal Tex", 2D) = "black" {}
        _MetalnessMap("MetalnessMap",2D) = "black" {}
        _Roughness("Roughness",Range(0,1)) = 0.2
        _Metalness("Metalness",Range(0,1)) = 0.2
        _Transparency("Transparency", Range(0,1)) = 0.7
        [Toggle(_RECEIVE_SHADOWS_OFF)] _RECEIVE_SHADOWS_OFF ("Receive Shadows Off?", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "LightMode"="MaxForwardBase" "Queue" = "Transparent"}
        LOD 100
        ZWrite Off

        HLSLINCLUDE
        #pragma enable_cbuffer
        #include "./PBRLitPass.hlsl"
        ENDHLSL    

        Pass
        {
            Name "DEFAULT"

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM   
            #pragma vertex PBRVertex
            #pragma fragment PBRFragmentTransparent

            #pragma shader_feature _RECEIVE_SHADOWS_OFF   
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM

            #pragma vertex ShadowCasterVertex
            #pragma fragment ShadowCasterFragment
        
            ENDHLSL
        }
    }
}
Shader "Custom/Terrain" {
    Properties {
        
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxColourCount = 8;

        int baseColourCount;
        float3 baseColours[maxColourCount]; // Note: array, but square brackets come before name, not variable type
        float baseStartHeights[maxColourCount];

        float minHeight;
        float maxHeight;

        struct Input {
            float3 worldPos;
        };

        // functions have to be called before their use in this language (CG)
        float inverseLerp(float a, float b, float value) {
            return saturate((value-a)/(b-a)); // saturate clamps between 0-1
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
            // Loop through base colours
            for (int i = 0; i < baseColourCount; i++) {
                // Set current color if heightPercent is above baseStartHeight
                float drawStrength = saturate(sign(heightPercent - baseStartHeights[i]));
                // if drawStrength = 0, we would overwrite Albedo with black, but with "o.Albedo * (1 - drawStrength) + ..." we retain previous Albedo
                o.Albedo = o.Albedo * (1 - drawStrength) + baseColours[i] * drawStrength;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}

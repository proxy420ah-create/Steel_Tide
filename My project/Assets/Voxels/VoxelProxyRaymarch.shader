Shader "SteelTide/Voxels/VoxelProxyRaymarch"
{
    Properties
    {
        _BackgroundColor("Background Color", Color) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Off
        ZWrite On
        ZTest LEqual

        Pass
        {
            Name "ProxyRaymarch"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<uint> _VoxelData;
            StructuredBuffer<float4> _MaterialColors;

            int _MaterialCount;
            float3 _VolumeDims;     // xyz dimensions in voxels
            float  _VoxelSize;      // world units per voxel
            float3 _VolumeOrigin;   // world-space min corner
            int    _MaxSteps;
            float4 _BackgroundColor;

            static const uint VX_MATERIAL_MASK = 0x1FFu;

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
            };

            struct FragOutput
            {
                float4 color : SV_Target0;
                float  depth : SV_Depth;
            };

            int3 VolumeDimsInt()
            {
                return int3(_VolumeDims + 0.5);
            }

            bool RayAABB(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax, out float tNear, out float tFar)
            {
                float3 invDir = 1.0 / rayDir;
                float3 t0 = (boxMin - rayOrigin) * invDir;
                float3 t1 = (boxMax - rayOrigin) * invDir;

                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);

                tNear = max(max(tMin.x, tMin.y), tMin.z);
                tFar  = min(min(tMax.x, tMax.y), tMax.z);

                return tNear <= tFar && tFar >= 0.0;
            }

            int VoxelIndex(int3 v, int3 dims)
            {
                return v.x + v.y * dims.x + v.z * dims.x * dims.y;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float4 worldPos = mul(unity_ObjectToWorld, float4(input.positionOS, 1.0));
                output.worldPos = worldPos.xyz;
                output.positionCS = mul(UNITY_MATRIX_VP, worldPos);
                return output;
            }

            FragOutput frag(Varyings input)
            {
                FragOutput o;

                float3 ro = _WorldSpaceCameraPos.xyz;
                float3 rd = normalize(input.worldPos - ro);

                float3 volumeMin = _VolumeOrigin;
                float3 volumeMax = _VolumeOrigin + float3(_VolumeDims) * _VoxelSize;

                float tNear, tFar;
                if (!RayAABB(ro, rd, volumeMin, volumeMax, tNear, tFar))
                {
                    discard;
                }

                float tStart = max(tNear, 0.0);
                float3 localOrigin = ro - _VolumeOrigin;
                float3 localDir = rd;

                float3 startPos = localOrigin + localDir * (tStart + 0.001);
                int3 dims = VolumeDimsInt();
                float3 p = startPos / _VoxelSize;
                int3 voxel = clamp((int3)floor(p), int3(0, 0, 0), dims - int3(1, 1, 1));
                int3 step = (int3)sign(localDir);

                float3 deltaDist = abs(_VoxelSize / localDir);
                float3 sideDist;
                sideDist.x = (localDir.x < 0.0) ? (p.x - (float)voxel.x) * deltaDist.x : (((float)(voxel.x + 1) - p.x) * deltaDist.x);
                sideDist.y = (localDir.y < 0.0) ? (p.y - (float)voxel.y) * deltaDist.y : (((float)(voxel.y + 1) - p.y) * deltaDist.y);
                sideDist.z = (localDir.z < 0.0) ? (p.z - (float)voxel.z) * deltaDist.z : (((float)(voxel.z + 1) - p.z) * deltaDist.z);

                // FIX: Seed tMax with tStart so currentT is camera-relative distance, not entry-relative
                // (VOXEL_PROXY_RAYMARCH_DESIGN.md §4.3 - fixes depth bleed-through bug)
                float3 tMax = tStart + sideDist;
                float3 tDelta = deltaDist;
                float currentT = tStart;
                float3 normal = float3(0, 1, 0);

                bool hit = false;
                float4 hitColor = _BackgroundColor;
                float3 worldHit = float3(0, 0, 0);

                [loop]
                for (int i = 0; i < _MaxSteps; ++i)
                {
                    if (voxel.x < 0 || voxel.y < 0 || voxel.z < 0 ||
                        voxel.x >= dims.x || voxel.y >= dims.y || voxel.z >= dims.z)
                    {
                        break;
                    }

                    uint packed = _VoxelData[VoxelIndex(voxel, dims)];
                    uint mat = packed & VX_MATERIAL_MASK;

                    if (mat != 0u)
                    {
                        uint maxMat = max(1, _MaterialCount) - 1;
                        mat = min(mat, maxMat);

                        float4 baseColor = _MaterialColors[mat];

                        if (mat == 3u)
                        {
                            float3 volumeCenter = float3(dims) * 0.5;
                            float3 voxelCenter = float3(voxel) + 0.5;
                            float distFromCenter = length(voxelCenter - volumeCenter);
                            float maxDist = length(float3(dims) * 0.5);
                            float normalizedDist = distFromCenter / maxDist;
                            float3 coreColor = float3(1.0, 0.5, 0.0);
                            float3 shellColor = float3(0.6, 0.6, 0.55);
                            baseColor.rgb = lerp(coreColor, shellColor, saturate(normalizedDist));
                        }

                        float3 lightDir = normalize(float3(0.5, 0.8, -0.3));
                        float diffuse = max(0.3, dot(normal, lightDir));
                        hitColor = float4(baseColor.rgb * diffuse, baseColor.a);

                        worldHit = ro + rd * currentT;
                        hit = true;
                        break;
                    }

                    float nextT;
                    if (tMax.x < tMax.y)
                    {
                        if (tMax.x < tMax.z)
                        {
                            voxel.x += step.x;
                            nextT = tMax.x;
                            tMax.x += tDelta.x;
                            normal = float3(-step.x, 0, 0);
                        }
                        else
                        {
                            voxel.z += step.z;
                            nextT = tMax.z;
                            tMax.z += tDelta.z;
                            normal = float3(0, 0, -step.z);
                        }
                    }
                    else
                    {
                        if (tMax.y < tMax.z)
                        {
                            voxel.y += step.y;
                            nextT = tMax.y;
                            tMax.y += tDelta.y;
                            normal = float3(0, -step.y, 0);
                        }
                        else
                        {
                            voxel.z += step.z;
                            nextT = tMax.z;
                            tMax.z += tDelta.z;
                            normal = float3(0, 0, -step.z);
                        }
                    }

                    currentT = nextT;
                }

                if (!hit)
                {
                    discard;
                }

                float4 clip = mul(UNITY_MATRIX_VP, float4(worldHit, 1.0));
                float ndcZ = clip.z / clip.w;

                o.color = hitColor;
                o.depth = ndcZ;
                return o;
            }
            ENDHLSL
        }
    }
}

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TableIcon"
{

    Properties
    {
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[ASEEnd]_Texture0("Texture 0", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
		LOD 0

		
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "UniversalMaterialType"="Lit" "Queue"="Transparent+200" "ShaderGraphShader"="true" }

		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
		ZTest Always
		ZWrite Off
		Offset 0 , 0
		ColorMask RGBA
		

		HLSLINCLUDE
		#pragma target 2.0
		#pragma exclude_renderers d3d11_9x 
		ENDHLSL
		
		
        Pass
        {
			
            Name "Sprite Lit"
            Tags { "LightMode"="Universal2D" }
            
            HLSLPROGRAM
            
			#define ASE_SRP_VERSION 999999

            
			#pragma vertex vert
			#pragma fragment frag

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
            #define VARYINGS_NEED_SCREENPOSITION
        
            #define SHADERPASS SHADERPASS_SPRITELIT
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
     
			

			struct VertexInput
			{
				float3 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float4 color : TEXCOORD2;
				float4 screenPosition : TEXCOORD3;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
        
            struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
			};

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			half4 _RendererColor;

			sampler2D _Texture0;
			CBUFFER_START( UnityPerMaterial )
			float4 _Texture0_ST;
			CBUFFER_END


			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);

				o.positionCS = vertexInput.positionCS;
				o.positionWS.xyz =  vertexInput.positionWS;
				o.texCoord0.xyzw =  v.uv0;
				o.color.xyzw =  v.color;
				o.screenPosition.xyzw =  vertexInput.positionNDC;
				
				return o;
			}

			half4 frag( VertexOutput IN   ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float2 uv_Texture0 = IN.texCoord0.xy * _Texture0_ST.xy + _Texture0_ST.zw;
				float4 tex2DNode5 = tex2D( _Texture0, uv_Texture0 );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.BaseColor = tex2DNode5.rgb;
				surfaceDescription.Alpha = tex2DNode5.a;

				half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);
				
				#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(color.rgb, color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(IN.positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, IN.positionWS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
				#endif

				color *= IN.color * _RendererColor;
				return color;
			}

            ENDHLSL
        }

		
        Pass
        {
			
            Name "Sprite Normal"
            Tags { "LightMode"="NormalsRendering" }

            HLSLPROGRAM
            
			#define ASE_SRP_VERSION 999999

            
			#pragma vertex vert
			#pragma fragment frag
        
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
        
            #define SHADERPASS SHADERPASS_SPRITENORMAL
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			

			sampler2D _Texture0;
			CBUFFER_START( UnityPerMaterial )
			float4 _Texture0_ST;
			CBUFFER_END

        
			struct VertexInput
			{
				float3 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float4 tangentWS : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            struct SurfaceDescription
			{
				float3 NormalTS;
				float Alpha;
			};
        
			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord2.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;

	
				float3 positionWS = TransformObjectToWorld(v.vertex);
				float4 tangentWS = float4(TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);

				o.positionCS = TransformWorldToHClip(positionWS);
				o.normalWS.xyz =  -GetViewForwardDir();
				o.tangentWS.xyzw =  tangentWS;
				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float2 uv_Texture0 = IN.ase_texcoord2.xy * _Texture0_ST.xy + _Texture0_ST.zw;
				float4 tex2DNode5 = tex2D( _Texture0, uv_Texture0 );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.NormalTS = float3(0.0f, 0.0f, 1.0f);
				surfaceDescription.Alpha = tex2DNode5.a;
				
				half crossSign = (IN.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
				half3 bitangent = crossSign * cross(IN.normalWS.xyz, IN.tangentWS.xyz);
				half4 color = half4(1.0,1.0,1.0, surfaceDescription.Alpha);
			
				return NormalsRenderingShared(color, surfaceDescription.NormalTS, IN.tangentWS.xyz, bitangent, IN.normalWS);
			}
        
            ENDHLSL
        }

		
        Pass
        {
			
            Name "SceneSelectionPass"
            Tags { "LightMode"="SceneSelectionPass" }
            
            Cull Off
			Blend Off
			ZTest LEqual
			ZWrite On
        
            HLSLPROGRAM
            
			#define ASE_SRP_VERSION 999999

            
			#pragma vertex vert
			#pragma fragment frag

            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
    
            #define SHADERPASS SHADERPASS_DEPTHONLY
	        #define SCENESELECTIONPASS 1
        
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
			

			sampler2D _Texture0;
			CBUFFER_START( UnityPerMaterial )
			float4 _Texture0_ST;
			CBUFFER_END


            struct VertexInput
			{
				float3 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};


			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            int _ObjectId;
            int _PassValue;

            struct SurfaceDescription
			{
				float Alpha;
			};

			
			VertexOutput vert( VertexInput v )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif


				float3 positionWS = TransformObjectToWorld(v.vertex);				
				o.positionCS = TransformWorldToHClip(positionWS);
				return o;
			}

			half4 frag( VertexOutput IN ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
				
				float2 uv_Texture0 = IN.ase_texcoord.xy * _Texture0_ST.xy + _Texture0_ST.zw;
				float4 tex2DNode5 = tex2D( _Texture0, uv_Texture0 );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.Alpha = tex2DNode5.a;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = half4(_ObjectId, _PassValue, 1.0, 1.0);
				return outColor;
			}

            ENDHLSL
        }

		
        Pass
        {
			
            Name "ScenePickingPass"
            Tags { "LightMode"="Picking" }
            
            Cull Back
			Blend Off
			ZTest LEqual
			ZWrite On

        
            HLSLPROGRAM
        
			#define ASE_SRP_VERSION 999999

        
			#pragma vertex vert
			#pragma fragment frag
        
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            
            #define SHADERPASS SHADERPASS_DEPTHONLY
			#define SCENEPICKINGPASS 1
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
        	

			sampler2D _Texture0;
			CBUFFER_START( UnityPerMaterial )
			float4 _Texture0_ST;
			CBUFFER_END

			
            struct VertexInput
			{
				float3 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 ase_texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

            float4 _SelectionID;
            
            struct SurfaceDescription
			{
				float Alpha;
			};
   
   			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.zw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				float3 positionWS = TransformObjectToWorld(v.vertex);
				o.positionCS = TransformWorldToHClip(positionWS);

				return o;
			}

			half4 frag(VertexOutput IN ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
			
				float2 uv_Texture0 = IN.ase_texcoord.xy * _Texture0_ST.xy + _Texture0_ST.zw;
				float4 tex2DNode5 = tex2D( _Texture0, uv_Texture0 );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.Alpha = tex2DNode5.a;

				#if _ALPHATEST_ON
					float alphaClipThreshold = 0.01f;
					#if ALPHA_CLIP_THRESHOLD
						alphaClipThreshold = surfaceDescription.AlphaClipThreshold;
					#endif
					clip(surfaceDescription.Alpha - alphaClipThreshold);
				#endif

				half4 outColor = _SelectionID;
				return outColor;
			}

        
            ENDHLSL
        }

		
        Pass
        {
			
            Name "Sprite Forward"
            Tags { "LightMode"="UniversalForward" }
                
            HLSLPROGRAM
        
			#define ASE_SRP_VERSION 999999

        
			#pragma vertex vert
			#pragma fragment frag
        
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_COLOR
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_COLOR
           
            #define SHADERPASS SHADERPASS_SPRITEFORWARD
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
		
			

			sampler2D _Texture0;
			CBUFFER_START( UnityPerMaterial )
			float4 _Texture0_ST;
			CBUFFER_END


            struct VertexInput
			{
				float3 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv0 : TEXCOORD0;
				float4 color : COLOR;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};


			struct VertexOutput
			{
				float4 positionCS : SV_POSITION;
				float4 texCoord0 : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float4 color : TEXCOORD2;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
            
            struct SurfaceDescription
			{
				float3 BaseColor;
				float Alpha;
				float3 NormalTS;
			};

			#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"

			
			VertexOutput vert( VertexInput v  )
			{
				VertexOutput o;
				ZERO_INITIALIZE(VertexOutput, o);

	
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3( 0, 0, 0 );
				#endif
				float3 vertexValue = defaultVertexValue;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.normal = v.normal;
				v.tangent.xyz = v.tangent.xyz;
	

				float3 positionWS = TransformObjectToWorld(v.vertex);
					
				o.positionCS = TransformWorldToHClip(positionWS);
				o.positionWS.xyz =  positionWS;
				o.texCoord0.xyzw =  v.uv0;
				o.color.xyzw =  v.color;

				return o;
			}

			half4 frag( VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				float2 uv_Texture0 = IN.texCoord0.xy * _Texture0_ST.xy + _Texture0_ST.zw;
				float4 tex2DNode5 = tex2D( _Texture0, uv_Texture0 );
				
				SurfaceDescription surfaceDescription = (SurfaceDescription)0;
				surfaceDescription.BaseColor = tex2DNode5.rgb;
				surfaceDescription.NormalTS = float3(0.0f, 0.0f, 1.0f);
				surfaceDescription.Alpha = tex2DNode5.a;


				half4 color = half4(surfaceDescription.BaseColor, surfaceDescription.Alpha);
			
				#if defined(DEBUG_DISPLAY)
				SurfaceData2D surfaceData;
				InitializeSurfaceData(color.rgb, color.a, surfaceData);
				InputData2D inputData;
				InitializeInputData(IN.positionWS.xy, half2(IN.texCoord0.xy), inputData);
				half4 debugColor = 0;

				SETUP_DEBUG_DATA_2D(inputData, IN.positionWS);

				if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
				{
					return debugColor;
				}
				#endif

				color *= IN.color;
				return color;
			}

        
            ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
	
	
}
/*ASEBEGIN
Version=18935
0;582.2858;1693.286;571.2857;2025.688;468.0487;1.9;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;6;-686.8572,-155.6429;Inherit;True;Property;_Texture0;Texture 0;0;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;5;-427.8572,-154.6429;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;0;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;ece0159bad6633944bf6b818f4dd296c;True;Sprite Lit;0;0;Sprite Lit;0;False;True;2;5;False;-1;10;False;-1;3;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Universal2D;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;ece0159bad6633944bf6b818f4dd296c;True;Sprite Normal;0;1;Sprite Normal;0;False;True;2;5;False;-1;10;False;-1;3;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=NormalsRendering;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;2;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;ece0159bad6633944bf6b818f4dd296c;True;SceneSelectionPass;0;2;SceneSelectionPass;0;False;True;2;5;False;-1;10;False;-1;3;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;0;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=SceneSelectionPass;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;3;0,0;Float;False;False;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;1;New Amplify Shader;ece0159bad6633944bf6b818f4dd296c;True;ScenePickingPass;0;3;ScenePickingPass;0;False;True;2;5;False;-1;10;False;-1;3;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;2;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=0;ShaderGraphShader=true;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;True;0;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=Picking;False;False;0;;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;4;0,0;Float;False;True;-1;2;UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI;0;12;TableIcon;ece0159bad6633944bf6b818f4dd296c;True;Sprite Forward;0;4;Sprite Forward;6;False;True;2;5;False;-1;10;False;-1;3;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;True;2;False;-1;True;7;False;-1;True;True;0;False;-1;0;False;-1;True;5;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;UniversalMaterialType=Lit;Queue=Transparent=Queue=200;ShaderGraphShader=true;True;0;True;17;d3d9;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;xboxseries;ps4;playstation;psp2;n3ds;wiiu;switch;nomrt;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=UniversalForward;False;False;0;;0;0;Standard;2;Vertex Position;1;0;Debug Display;0;0;0;5;True;True;True;True;True;False;;False;0
WireConnection;5;0;6;0
WireConnection;4;0;5;0
WireConnection;4;2;5;4
ASEEND*/
//CHKSM=C38BAD4F8A6866AD9DACB7B20455153355B9D21B
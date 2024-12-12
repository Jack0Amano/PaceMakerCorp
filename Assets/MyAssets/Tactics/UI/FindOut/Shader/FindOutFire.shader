// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "FindOutFire"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		_Texture0("Texture 0", 2D) = "white" {}
		_FPS("FPS", Float) = 25
		_OffsetFrame("OffsetFrame", Float) = 0
		_X("X", Float) = 5
		_Y("Y", Float) = 5

	}

	SubShader
	{
		LOD 0

		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
		
		Stencil
		{
			Ref [_Stencil]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
			CompFront [_StencilComp]
			PassFront [_StencilOp]
			FailFront Keep
			ZFailFront Keep
			CompBack Always
			PassBack Keep
			FailBack Keep
			ZFailBack Keep
		}


		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		
		Pass
		{
			Name "Default"
		CGPROGRAM
			
			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_CLIP_RECT
			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			#include "UnityShaderVariables.cginc"
			#define ASE_NEEDS_FRAG_COLOR

			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				
			};
			
			uniform fixed4 _Color;
			uniform fixed4 _TextureSampleAdd;
			uniform float4 _ClipRect;
			uniform sampler2D _MainTex;
			uniform sampler2D _Texture0;
			uniform float _X;
			uniform float _Y;
			uniform float _FPS;
			uniform float _OffsetFrame;

			
			v2f vert( appdata_t IN  )
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID( IN );
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				OUT.worldPosition = IN.vertex;
				
				
				OUT.worldPosition.xyz +=  float3( 0, 0, 0 ) ;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				float4 color3 = IsGammaSpace() ? float4(1,1,1,1) : float4(1,1,1,1);
				float temp_output_4_0_g1 = _X;
				float temp_output_5_0_g1 = _Y;
				float2 appendResult7_g1 = (float2(temp_output_4_0_g1 , temp_output_5_0_g1));
				float totalFrames39_g1 = ( temp_output_4_0_g1 * temp_output_5_0_g1 );
				float2 appendResult8_g1 = (float2(totalFrames39_g1 , temp_output_5_0_g1));
				float clampResult42_g1 = clamp( 0.0 , 0.0001 , ( totalFrames39_g1 - 1.0 ) );
				float temp_output_35_0_g1 = frac( ( ( floor( ( ( ( _Time.y * _FPS ) % 25.0 ) + _OffsetFrame ) ) + clampResult42_g1 ) / totalFrames39_g1 ) );
				float2 appendResult29_g1 = (float2(temp_output_35_0_g1 , ( 1.0 - temp_output_35_0_g1 )));
				float2 temp_output_15_0_g1 = ( ( IN.texcoord.xy / appendResult7_g1 ) + ( floor( ( appendResult8_g1 * appendResult29_g1 ) ) / appendResult7_g1 ) );
				float4 tex2DNode22 = tex2D( _Texture0, temp_output_15_0_g1 );
				
				half4 color = ( IN.color * color3 *  ( tex2DNode22.a - 0.0 > 0.0 ? 1.0 : tex2DNode22.a - 0.0 <= 0.0 && tex2DNode22.a + 0.0 >= 0.0 ? 0.0 : 0.0 )  * tex2DNode22 );
				
				#ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
				
				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}
	CustomEditor "ASEMaterialInspector"
	

	Fallback "_StencilComp"
}
/*ASEBEGIN
Version=18935
415.4286;72.57143;1266.571;721.8571;2062.874;338.0124;1.818127;True;False
Node;AmplifyShaderEditor.SimpleTimeNode;7;-1811.087,414.7445;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-1804.066,569.2162;Inherit;False;Property;_FPS;FPS;1;0;Create;True;0;0;0;False;0;False;25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;14;-1659.859,598.147;Inherit;False;Constant;_MaxFrame;MaxFrame;2;0;Create;True;0;0;0;False;0;False;25;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-1637.892,463.8947;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;16;-1502.808,620.0881;Inherit;False;Property;_OffsetFrame;OffsetFrame;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleRemainderNode;13;-1500.498,488.4416;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;15;-1337.673,533.4786;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FloorOpNode;17;-1214.11,531.1691;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-1286.481,371.4328;Inherit;False;Property;_X;X;3;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;20;-1282.481,445.4327;Inherit;False;Property;_Y;Y;4;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;18;-1054.547,339.3159;Inherit;False;Flipbook;-1;;1;53c2488c220f6564ca6c90721ee16673;2,71,0,68,0;8;51;SAMPLER2D;0.0;False;13;FLOAT2;0,0;False;4;FLOAT;3;False;5;FLOAT;3;False;24;FLOAT;0;False;2;FLOAT;0;False;55;FLOAT;0;False;70;FLOAT;0;False;5;COLOR;53;FLOAT2;0;FLOAT;47;FLOAT;48;FLOAT;62
Node;AmplifyShaderEditor.TexturePropertyNode;6;-1046.661,110.796;Inherit;True;Property;_Texture0;Texture 0;0;0;Create;True;0;0;0;False;0;False;726ab27d381927148bc7cd7b7cc0144f;7677410efacd75741b8b298164bc50c8;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;22;-757.631,308.0081;Inherit;True;Property;_TextureSample0;Texture Sample 0;5;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;3;-470.2858,-3.214294;Inherit;False;Constant;_Color0;Color 0;0;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;2;-429.2858,-185.2143;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCIf;21;-410.2501,373.3744;Inherit;False;6;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-178.4686,-24.26878;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;-4.072652,-25.45408;Float;False;True;-1;2;ASEMaterialInspector;0;6;FindOutFire;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;True;True;True;True;True;0;True;-9;False;False;False;False;False;False;False;True;True;0;True;-5;255;True;-8;255;True;-7;0;True;-4;0;True;-6;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;2;False;-1;True;0;True;-11;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;True;False;False;False;False;False;True;False;False;False;False;False;False;False;False;False;True;False;True;False;False;False;True;False;False;False;False;True;2;False;0;_StencilComp;1;=;0;Standard;0;0;1;True;False;;False;0
WireConnection;8;0;7;0
WireConnection;8;1;9;0
WireConnection;13;0;8;0
WireConnection;13;1;14;0
WireConnection;15;0;13;0
WireConnection;15;1;16;0
WireConnection;17;0;15;0
WireConnection;18;4;19;0
WireConnection;18;5;20;0
WireConnection;18;2;17;0
WireConnection;22;0;6;0
WireConnection;22;1;18;0
WireConnection;21;0;22;4
WireConnection;4;0;2;0
WireConnection;4;1;3;0
WireConnection;4;2;21;0
WireConnection;4;3;22;0
WireConnection;1;0;4;0
ASEEND*/
//CHKSM=7E8B0DBE761652A854A0DD975F3B71A52E49117F
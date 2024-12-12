// Made with Amplify Shader Editor v1.9.0.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "CompassTarget"
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
		_Degree("Degree", Range( 0 , 360)) = 0

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
			Comp [_StencilComp]
			Pass [_StencilOp]
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
			uniform float _Degree;

			
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
				float2 texCoord76 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 break81 = ( texCoord76 / 0.03 );
				float ifLocalVar38 = 0;
				if( 0.5 > ( _Degree / 360.0 ) )
				ifLocalVar38 = -0.004;
				else if( 0.5 < ( _Degree / 360.0 ) )
				ifLocalVar38 = 1.004;
				float temp_output_25_0 = ( ( _Degree / 360.0 ) - ifLocalVar38 );
				float temp_output_96_0 = ( ( sqrt( ( 0.3 - ( temp_output_25_0 * temp_output_25_0 ) ) ) *  ( ( _Degree / 360.0 ) - 0.0 > 0.5 ? 1.0 : ( _Degree / 360.0 ) - 0.0 <= 0.5 && ( _Degree / 360.0 ) + 0.0 >= 0.5 ? 0.0 : -1.0 )  ) +  ( ( _Degree / 360.0 ) - 0.0 > 0.5 ? 0.23 : ( _Degree / 360.0 ) - 0.0 <= 0.5 && ( _Degree / 360.0 ) + 0.0 >= 0.5 ? 0.0 : 0.77 )  );
				float2 appendResult85 = (float2(( break81.x - ( (  ( 0.5 - 0.0978 > ( _Degree / 360.0 ) ? temp_output_96_0 : 0.5 - 0.0978 <= ( _Degree / 360.0 ) && 0.5 + 0.0978 >= ( _Degree / 360.0 ) ? ( _Degree / 360.0 ) : temp_output_96_0 )  * 33.35 ) - 0.5 ) ) , ( break81.y - 16.15 )));
				
				half4 color = ( IN.color * color3 * tex2D( _Texture0, appendResult85 ) );
				
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
	
	Fallback Off
}
/*ASEBEGIN
Version=19002
248;72.57143;1424.857;705.8571;1731.741;651.5559;1.253498;True;False
Node;AmplifyShaderEditor.RangedFloatNode;95;-3661.456,-472.3732;Inherit;False;Property;_Degree;Degree;1;0;Create;True;0;0;0;False;0;False;0;0;0;360;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;103;-3391.047,-467.5645;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;360;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-3354.897,-634.4604;Inherit;False;Constant;_Float2;Float 2;2;0;Create;True;0;0;0;False;0;False;1.004;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;73;-3277.734,-467.2681;Inherit;False;FLOAT;1;0;FLOAT;0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;39;-3356.897,-705.4601;Inherit;False;Constant;_Float1;Float 1;2;0;Create;True;0;0;0;False;0;False;-0.004;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;38;-3141.402,-745.315;Inherit;False;False;5;0;FLOAT;0.5;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;25;-2907.602,-683.2517;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.707;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-2768.28,-693.1406;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;18;-2642.072,-715.7527;Inherit;False;2;0;FLOAT;0.3;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCIf;45;-2652.842,-507.0557;Inherit;False;6;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;-1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SqrtOpNode;20;-2505.155,-713.4358;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;-2379.153,-714.3116;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCIf;97;-2653.147,-326.8985;Inherit;False;6;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0.23;False;3;FLOAT;0;False;4;FLOAT;0.77;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;-2207.243,-701.2985;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCIf;41;-2020.786,-636.678;Inherit;False;6;0;FLOAT;0.5;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0.0978;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;99;-1658.238,62.14868;Inherit;False;Constant;_Float5;Float 5;4;0;Create;True;0;0;0;False;0;False;33.35;104;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;78;-1661.997,361.6253;Inherit;False;Constant;_Size;Size;2;0;Create;True;0;0;0;False;0;False;0.03;0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;76;-1776.997,229.6251;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;98;-1468.709,11.24139;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;77;-1491.997,248.6252;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;104;-1390.745,423.7164;Inherit;False;Constant;_Float6;Float 6;4;0;Create;True;0;0;0;False;0;False;16.15;16.15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;81;-1364.34,255.2746;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleSubtractOpNode;101;-1343.638,45.24875;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;86;-1215.266,214.6012;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;87;-1213.266,311.6015;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;24.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;5;-922.2486,79.11552;Inherit;True;Property;_Texture0;Texture 0;0;0;Create;True;0;0;0;False;0;False;139b99faa9bd67542a36f9f88ff11a63;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.DynamicAppendNode;85;-1076.34,254.2747;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;6;-620.0976,237.1881;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;3;-474.0001,-49.78571;Inherit;False;Constant;_Color0;Color 0;0;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;2;-428.0001,-226.7857;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;84;-1520.659,148.6164;Inherit;False;Property;_Float0;Float 0;2;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-237.0001,-58.78568;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;6;CompassTarget;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;2;5;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;7;True;_StencilComp;1;True;_StencilOp;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;103;0;95;0
WireConnection;73;0;103;0
WireConnection;38;1;73;0
WireConnection;38;2;39;0
WireConnection;38;4;40;0
WireConnection;25;0;73;0
WireConnection;25;1;38;0
WireConnection;19;0;25;0
WireConnection;19;1;25;0
WireConnection;18;1;19;0
WireConnection;45;0;73;0
WireConnection;20;0;18;0
WireConnection;43;0;20;0
WireConnection;43;1;45;0
WireConnection;97;0;73;0
WireConnection;96;0;43;0
WireConnection;96;1;97;0
WireConnection;41;1;73;0
WireConnection;41;2;96;0
WireConnection;41;3;73;0
WireConnection;41;4;96;0
WireConnection;98;0;41;0
WireConnection;98;1;99;0
WireConnection;77;0;76;0
WireConnection;77;1;78;0
WireConnection;81;0;77;0
WireConnection;101;0;98;0
WireConnection;86;0;81;0
WireConnection;86;1;101;0
WireConnection;87;0;81;1
WireConnection;87;1;104;0
WireConnection;85;0;86;0
WireConnection;85;1;87;0
WireConnection;6;0;5;0
WireConnection;6;1;85;0
WireConnection;4;0;2;0
WireConnection;4;1;3;0
WireConnection;4;2;6;0
WireConnection;1;0;4;0
ASEEND*/
//CHKSM=1EF548DB3F174551D6D8724D3B38E3801F325201
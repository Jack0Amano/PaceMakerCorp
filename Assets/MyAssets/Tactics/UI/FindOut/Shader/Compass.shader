// Made with Amplify Shader Editor v1.9.0.2
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Compass"
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
		_YScale("YScale", Float) = 1
		_YOffset("YOffset", Range( 0 , 1)) = 0

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
			uniform float _YScale;
			uniform float _YOffset;

			
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
				float2 texCoord13 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float ifLocalVar38 = 0;
				if( 0.5 > texCoord13.x )
				ifLocalVar38 = 0.774;
				else if( 0.5 < texCoord13.x )
				ifLocalVar38 = 0.226;
				float temp_output_25_0 = ( texCoord13.x - ifLocalVar38 );
				float temp_output_43_0 = ( sqrt( ( 0.3 - ( temp_output_25_0 * temp_output_25_0 ) ) ) *  ( texCoord13.x - 0.0 > 0.5 ? -1.0 : texCoord13.x - 0.0 <= 0.5 && texCoord13.x + 0.0 >= 0.5 ? 0.0 : 1.0 )  );
				float2 appendResult16 = (float2(( ( ( _Degree / 360.0 ) + 0.53115 ) +  ( 0.5 - 0.0978 > texCoord13.x ? temp_output_43_0 : 0.5 - 0.0978 <= texCoord13.x && 0.5 + 0.0978 >= texCoord13.x ? texCoord13.x : temp_output_43_0 )  ) , ( ( texCoord13.y * _YScale ) + _YOffset )));
				
				half4 color = ( IN.color * color3 * tex2D( _Texture0, appendResult16 ) *  ( texCoord13.x - 0.2737 > 0.5 ? 0.0 : texCoord13.x - 0.2737 <= 0.5 && texCoord13.x + 0.2737 >= 0.5 ? 1.0 : 0.0 )  );
				
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
248;72.57143;1424.857;705.8571;2149.19;75.73039;1;True;False
Node;AmplifyShaderEditor.RangedFloatNode;40;-2828.099,403.6656;Inherit;False;Constant;_Float2;Float 2;2;0;Create;True;0;0;0;False;0;False;0.226;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;39;-2830.099,332.6658;Inherit;False;Constant;_Float1;Float 1;2;0;Create;True;0;0;0;False;0;False;0.774;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;13;-2785.178,520.9069;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ConditionalIfNode;38;-2614.604,292.811;Inherit;False;False;5;0;FLOAT;0.5;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;25;-2410.804,350.8743;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.707;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-2276.482,353.9854;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;18;-2146.274,328.3733;Inherit;False;2;0;FLOAT;0.3;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-1848.495,85.62747;Inherit;False;Property;_Degree;Degree;1;0;Create;True;0;0;0;False;0;False;0;360;0;360;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCIf;45;-1991.846,493.6706;Inherit;False;6;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;-1;False;3;FLOAT;0;False;4;FLOAT;1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SqrtOpNode;20;-2007.357,331.6902;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;49;-1564.55,109.7285;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;360;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;67;-2186.095,930.3979;Inherit;False;Property;_YScale;YScale;2;0;Create;True;0;0;0;False;0;False;1;0.137;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;-1718.157,286.4142;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;68;-2046.87,996.7734;Inherit;False;Property;_YOffset;YOffset;3;0;Create;True;0;0;0;False;0;False;0;0.417;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;48;-1421.55,145.7285;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.53115;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;58;-1930.396,873.2066;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCIf;41;-1485.89,295.1482;Inherit;False;6;0;FLOAT;0.5;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0.0978;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;23;-1228.801,251.8193;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;69;-1739.276,872.1172;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;16;-816.7543,286.7577;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;5;-942.8811,33.36713;Inherit;True;Property;_Texture0;Texture 0;0;0;Create;True;0;0;0;False;0;False;None;d0875be340825a94dba4bdefad941782;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.VertexColorNode;2;-428.0001,-226.7857;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;3;-474.0001,-49.78571;Inherit;False;Constant;_Color0;Color 0;0;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;6;-620.0976,237.1881;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCIf;31;-571.1584,551.0584;Inherit;False;6;0;FLOAT;0;False;1;FLOAT;0.5;False;2;FLOAT;0;False;3;FLOAT;1;False;4;FLOAT;0;False;5;FLOAT;0.2737;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;-237.0001,-58.78568;Inherit;False;4;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;6;Compass;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;2;5;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;7;True;_StencilComp;1;True;_StencilOp;1;False;;1;False;;7;False;;1;False;;1;False;;1;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;38;1;13;1
WireConnection;38;2;39;0
WireConnection;38;4;40;0
WireConnection;25;0;13;1
WireConnection;25;1;38;0
WireConnection;19;0;25;0
WireConnection;19;1;25;0
WireConnection;18;1;19;0
WireConnection;45;0;13;1
WireConnection;20;0;18;0
WireConnection;49;0;24;0
WireConnection;43;0;20;0
WireConnection;43;1;45;0
WireConnection;48;0;49;0
WireConnection;58;0;13;2
WireConnection;58;1;67;0
WireConnection;41;1;13;1
WireConnection;41;2;43;0
WireConnection;41;3;13;1
WireConnection;41;4;43;0
WireConnection;23;0;48;0
WireConnection;23;1;41;0
WireConnection;69;0;58;0
WireConnection;69;1;68;0
WireConnection;16;0;23;0
WireConnection;16;1;69;0
WireConnection;6;0;5;0
WireConnection;6;1;16;0
WireConnection;31;0;13;1
WireConnection;4;0;2;0
WireConnection;4;1;3;0
WireConnection;4;2;6;0
WireConnection;4;3;31;0
WireConnection;1;0;4;0
ASEEND*/
//CHKSM=43A4841AB2942B89C383CEBB9FF39BDBE9F740D1
#ifdef HLSL

float4x4 u_worldViewProjectionMatrix;
float4 u_color;

void main(
	in float3 a_position: POSITION,
#ifdef USE_VERTEXCOLOR
	in float4 a_color: COLOR,
#endif
#ifdef USE_TEXTURE
	in float2 a_texcoord: TEXCOORD,
#endif
#ifdef USE_TEXTURE
	out float2 v_texcoord : TEXCOORD,
#endif
	out float4 v_color : COLOR,
	out float4 sv_position: SV_POSITION
)
{
	// Color
	v_color = u_color;
#ifdef USE_VERTEXCOLOR
	v_color *= a_color;
#endif

	// Texcoord
#ifdef USE_TEXTURE
	v_texcoord = a_texcoord;
#endif

	// Position
	sv_position = mul(float4(a_position.xyz, 1.0), u_worldViewProjectionMatrix);
}

#endif
#ifdef GLSL

// <Semantic Name='POSITION' Attribute='a_position' />
// <Semantic Name='COLOR' Attribute='a_color' />
// <Semantic Name='TEXCOORD' Attribute='a_texcoord' />

uniform mat4 u_worldViewProjectionMatrix;
uniform vec4 u_color;

attribute vec3 a_position;
#ifdef USE_VERTEXCOLOR
attribute vec4 a_color;
#endif
#ifdef USE_TEXTURE
attribute vec2 a_texcoord;
#endif
#ifdef USE_TEXTURE
varying vec2 v_texcoord;
#endif
varying vec4 v_color;

void main()
{
	// Color
	v_color = u_color;
#ifdef USE_VERTEXCOLOR
	v_color *= a_color;
#endif

	// Texcoord
#ifdef USE_TEXTURE
	v_texcoord = a_texcoord;
#endif

	// Position
	gl_Position = u_worldViewProjectionMatrix * vec4(a_position.xyz, 1.0);

	// Fix gl_Position
	OPENGL_POSITION_FIX;
}

#endif

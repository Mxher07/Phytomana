#ifdef HLSL

// Transform
#ifdef USE_INSTANCING
#ifdef USE_LIGHTING
float4x4 u_worldMatrix[MAX_INSTANCES_COUNT];
#endif
#ifdef USE_FOG
float4x4 u_worldViewMatrix[MAX_INSTANCES_COUNT];
#endif
float4x4 u_worldViewProjectionMatrix[MAX_INSTANCES_COUNT];
#else
#ifdef USE_LIGHTING
float4x4 u_worldMatrix;
#endif
#ifdef USE_FOG
float4x4 u_worldViewMatrix;
#endif
float4x4 u_worldViewProjectionMatrix;
#endif

// Material
float4 u_materialColor;

// Emission color
#ifdef USE_EMISSIONCOLOR
float4 u_emissionColor;
#endif

// Light
#ifdef ONE_LIGHT
float3 u_ambientLightColor;
float3 u_diffuseLightColor1;
float3 u_directionToLight1;
#endif
#ifdef TWO_LIGHTS
float3 u_ambientLightColor;
float3 u_diffuseLightColor1;
float3 u_directionToLight1;
float3 u_diffuseLightColor2;
float3 u_directionToLight2;
#endif
#ifdef THREE_LIGHTS
float3 u_ambientLightColor;
float3 u_diffuseLightColor1;
float3 u_directionToLight1;
float3 u_diffuseLightColor2;
float3 u_directionToLight2;
float3 u_diffuseLightColor3;
float3 u_directionToLight3;
#endif

// Fog
#ifdef USE_FOG
float u_fogStart;
float u_fogLength;
#endif

void main(
	in float3 a_position: POSITION,
#ifdef USE_LIGHTING
	in float3 a_normal: NORMAL,
#endif
#ifdef USE_VERTEXCOLOR
	in float4 a_color: COLOR,
#endif
#ifdef USE_TEXTURE
	in float2 a_texcoord: TEXCOORD,
#endif
#ifdef USE_INSTANCING
	in float a_instance: INSTANCE,
#endif
	out float4 v_color : COLOR,
#ifdef USE_TEXTURE
	out float2 v_texcoord : TEXCOORD,
#endif
#ifdef USE_FOG	
	out float v_fog : FOG,
#endif
	out float4 sv_position: SV_POSITION
)
{
	// Texture
#ifdef USE_TEXTURE
	v_texcoord = a_texcoord;
#endif
	
	// Instancing
#ifdef USE_INSTANCING
	int instance = int(a_instance);
#endif

	// Normal
#ifdef USE_LIGHTING
#ifdef USE_INSTANCING
	float3 worldNormal = normalize(mul(float4(a_normal, 0.0), u_worldMatrix[instance]).xyz);
#else
	float3 worldNormal = normalize(mul(float4(a_normal, 0.0), u_worldMatrix).xyz);
#endif
#endif

	// Lighting
#ifndef USE_LIGHTING
	v_color = u_materialColor;
#endif
#ifdef ONE_LIGHT
	float3 lightColor = u_ambientLightColor;
	lightColor += u_diffuseLightColor1 * max(dot(u_directionToLight1, worldNormal), 0.0);
	v_color = float4(lightColor, 1) * u_materialColor;
#endif
#ifdef TWO_LIGHTS
	float3 lightColor = u_ambientLightColor;
	lightColor += u_diffuseLightColor1 * max(dot(u_directionToLight1, worldNormal), 0.0);
	lightColor += u_diffuseLightColor2 * max(dot(u_directionToLight2, worldNormal), 0.0);
	v_color = float4(lightColor, 1) * u_materialColor;
#endif
#ifdef THREE_LIGHTS
	float3 lightColor = u_ambientLightColor;
	lightColor += u_diffuseLightColor1 * max(dot(u_directionToLight1, worldNormal), 0.0);
	lightColor += u_diffuseLightColor2 * max(dot(u_directionToLight2, worldNormal), 0.0);
	lightColor += u_diffuseLightColor3 * max(dot(u_directionToLight3, worldNormal), 0.0);
	v_color = float4(lightColor, 1) * u_materialColor;
#endif

	// Vertex color
#ifdef USE_VERTEXCOLOR
	v_color *= a_color;
#endif

	// Emission color
#ifdef USE_EMISSIONCOLOR
	v_color += u_emissionColor;
#endif
	
	// Fog
#ifdef USE_FOG
#ifdef USE_INSTANCING
	float3 viewPosition = mul(float4(a_position, 1.0), u_worldViewMatrix[instance]).xyz;
#else
	float3 viewPosition = mul(float4(a_position, 1.0), u_worldViewMatrix).xyz;
#endif
	float d = length(viewPosition);
	v_fog = saturate((d - u_fogStart) / u_fogLength);
#endif
	
	// Position
#ifdef USE_INSTANCING
	sv_position = mul(float4(a_position, 1.0), u_worldViewProjectionMatrix[instance]);
#else
	sv_position = mul(float4(a_position, 1.0), u_worldViewProjectionMatrix);
#endif
}

#endif
#ifdef GLSL

// <Semantic Name='POSITION' Attribute='a_position' />
// <Semantic Name='NORMAL' Attribute='a_normal' />
// <Semantic Name='COLOR' Attribute='a_color' />
// <Semantic Name='TEXCOORD' Attribute='a_texcoord' />
// <Semantic Name='INSTANCE' Attribute='a_instance' />

// Transform
#ifdef USE_INSTANCING
#ifdef USE_LIGHTING
uniform mat4 u_worldMatrix[MAX_INSTANCES_COUNT];
#endif
uniform mat4 u_worldViewProjectionMatrix[MAX_INSTANCES_COUNT];
#ifdef USE_FOG
uniform mat4 u_worldViewMatrix[MAX_INSTANCES_COUNT];
#endif
#else
#ifdef USE_LIGHTING
uniform mat4 u_worldMatrix;
#endif
uniform mat4 u_worldViewProjectionMatrix;
#ifdef USE_FOG
uniform mat4 u_worldViewMatrix;
#endif
#endif

uniform vec4 u_materialColor;

// Emission color
#ifdef USE_EMISSIONCOLOR
uniform vec4 u_emissionColor;
#endif

// Material and light
#ifdef ONE_LIGHT
uniform vec3 u_ambientLightColor;
uniform vec3 u_diffuseLightColor1;
uniform vec3 u_directionToLight1;
#endif
#ifdef TWO_LIGHTS
uniform vec3 u_ambientLightColor;
uniform vec3 u_diffuseLightColor1;
uniform vec3 u_directionToLight1;
uniform vec3 u_diffuseLightColor2;
uniform vec3 u_directionToLight2;
#endif
#ifdef THREE_LIGHTS
uniform vec3 u_ambientLightColor;
uniform vec3 u_diffuseLightColor1;
uniform vec3 u_directionToLight1;
uniform vec3 u_diffuseLightColor2;
uniform vec3 u_directionToLight2;
uniform vec3 u_diffuseLightColor3;
uniform vec3 u_directionToLight3;
#endif

// Fog
#ifdef USE_FOG
uniform float u_fogStart;
uniform float u_fogLength;
#endif

// Inputs and outputs
attribute vec3 a_position;
#ifdef USE_LIGHTING
attribute vec3 a_normal;
#endif
#ifdef USE_VERTEXCOLOR
attribute vec4 a_color;
#endif
#ifdef USE_TEXTURE
attribute vec2 a_texcoord;
#endif
#ifdef USE_INSTANCING
attribute float a_instance;
#endif
varying vec4 v_color;
#ifdef USE_TEXTURE
varying vec2 v_texcoord;
#endif
#ifdef USE_FOG
varying float v_fog;
#endif

void main()
{
	// Texture
#ifdef USE_TEXTURE
	v_texcoord = a_texcoord;
#endif
	
	// Instancing
#ifdef USE_INSTANCING
	int instance = int(a_instance);
#endif
	
	// Normal
#ifdef USE_LIGHTING
#ifdef USE_INSTANCING
	vec3 worldNormal = normalize(vec3(u_worldMatrix[instance] * vec4(a_normal, 0.0)));
#else
	vec3 worldNormal = normalize(vec3(u_worldMatrix * vec4(a_normal, 0.0)));
#endif
#endif

	// Lighting
#ifndef USE_LIGHTING
	v_color = u_materialColor;
#endif
#ifdef ONE_LIGHT
	vec3 lightColor = u_ambientLightColor;
	lightColor += u_diffuseLightColor1 * max(dot(u_directionToLight1, worldNormal), 0.0);
	v_color = vec4(lightColor, 1) * u_materialColor;
#endif
#ifdef TWO_LIGHTS
	vec3 lightColor = u_ambientLightColor;
	lightColor += u_diffuseLightColor1 * max(dot(u_directionToLight1, worldNormal), 0.0);
	lightColor += u_diffuseLightColor2 * max(dot(u_directionToLight2, worldNormal), 0.0);
	v_color = vec4(lightColor, 1) * u_materialColor;
#endif
#ifdef THREE_LIGHTS
	vec3 lightColor = u_ambientLightColor;
	lightColor += u_diffuseLightColor1 * max(dot(u_directionToLight1, worldNormal), 0.0);
	lightColor += u_diffuseLightColor2 * max(dot(u_directionToLight2, worldNormal), 0.0);
	lightColor += u_diffuseLightColor3 * max(dot(u_directionToLight3, worldNormal), 0.0);
	v_color = vec4(lightColor, 1) * u_materialColor;
#endif

	// Vertex color
#ifdef USE_VERTEXCOLOR
	v_color *= a_color;
#endif
	
	// Emission color
#ifdef USE_EMISSIONCOLOR
	v_color += u_emissionColor;
#endif

	// Fog
#ifdef USE_FOG
#ifdef USE_INSTANCING
	vec3 viewPosition = vec3(u_worldViewMatrix[instance] * vec4(a_position, 1.0));
#else
	vec3 viewPosition = vec3(u_worldViewMatrix * vec4(a_position, 1.0));
#endif
	float d = length(viewPosition);
	v_fog = clamp((d - u_fogStart) / u_fogLength, 0.0, 1.0);
#endif
	
	// Position
#ifdef USE_INSTANCING
	gl_Position = u_worldViewProjectionMatrix[instance] * vec4(a_position, 1.0);
#else
	gl_Position = u_worldViewProjectionMatrix * vec4(a_position, 1.0);
#endif

	// Fix gl_Position
	OPENGL_POSITION_FIX;
}

#endif

#ifdef HLSL

// Transform
float4x4 u_worldMatrix[MAX_INSTANCES_COUNT];
float4x4 u_worldViewProjectionMatrix[MAX_INSTANCES_COUNT];

#ifdef USE_SKINNING
float4x4 u_jointMatrices[MAX_JOINTS_COUNT];
#endif

// Material
float4 u_materialColor;

// Emission color
float4 u_emissionColor;

// Light
float3 u_ambientLightColor;
float3 u_diffuseLightColor1;
float3 u_directionToLight1;
float3 u_diffuseLightColor2;
float3 u_directionToLight2;

// Fog
float3 u_worldUp;
float u_fogYMultiplier;
float3 u_fogBottomTopDensity;
float2 u_hazeStartDensity;

float fogIntegral(float y)
{
	return smoothstep(u_fogBottomTopDensity.x, u_fogBottomTopDensity.y, y) * (u_fogBottomTopDensity.y - u_fogBottomTopDensity.x) + u_fogBottomTopDensity.x;
}

float calculateFog(float3 position, int instance)
{
	float3 worldPosition = mul(float4(position, 1.0), u_worldMatrix[instance]).xyz;
	float worldY = dot(worldPosition, u_worldUp);
	float fogDistance = sqrt((u_fogYMultiplier * u_fogYMultiplier - 1) * (worldY * worldY) + dot(worldPosition, worldPosition));	// Reduce Y component of worldPosition in distance calculation
	float fogFactor = (fogIntegral(0) - fogIntegral(worldY)) / (0 - worldY);
	return saturate(saturate(u_hazeStartDensity.y * (fogDistance - u_hazeStartDensity.x)) + fogFactor * u_fogBottomTopDensity.z * fogDistance);
}

#ifdef USE_SKINNING
float4x4 getSkinningMatrix(float4 joints, float4 weights)
{
	float4x4 skin = (float4x4)0;
	skin += weights.x * u_jointMatrices[int(joints.x)];
	skin += weights.y * u_jointMatrices[int(joints.y)];
	skin += weights.z * u_jointMatrices[int(joints.z)];
	skin += weights.w * u_jointMatrices[int(joints.w)];
	return skin;
}
#endif

void main(
	in float3 a_position: POSITION,
	in float3 a_normal: NORMAL,
	in float2 a_texcoord: TEXCOORD,
	in float a_instance: INSTANCE,
#ifdef USE_SKINNING
	in float4 a_joints: BLENDINDICES,
	in float4 a_weights: BLENDWEIGHTS,
#endif
	out float4 v_color : COLOR,
	out float2 v_texcoord : TEXCOORD,
	out float v_fog : FOG,
	out float4 sv_position: SV_POSITION
)
{
	// Texture
	v_texcoord = a_texcoord;

	// Instancing
	int instance = int(a_instance);

	// Apply skinning if enabled
	float3 skinnedPosition = a_position;
	float3 skinnedNormal = a_normal;

#ifdef USE_SKINNING
	float4x4 skinMatrix = getSkinningMatrix(a_joints, a_weights);
	skinnedPosition = mul(skinMatrix, float4(a_position, 1.0)).xyz;
	skinnedNormal = normalize(mul((float3x3)skinMatrix, a_normal));
#endif

	// Normal
	float3 worldNormal = normalize(mul(float4(skinnedNormal, 0.0), u_worldMatrix[instance]).xyz);

	// Lighting
	v_color = u_materialColor;
	float3 lightColor = u_ambientLightColor;
	lightColor += u_diffuseLightColor1 * max(dot(u_directionToLight1, worldNormal), 0.0);
	lightColor += u_diffuseLightColor2 * max(dot(u_directionToLight2, worldNormal), 0.0);
	v_color = float4(lightColor, 1) * u_materialColor;

	// Emission color
	v_color += u_emissionColor;

	// Fog
	v_fog = calculateFog(skinnedPosition, instance);

	// Position
	sv_position = mul(float4(skinnedPosition, 1.0), u_worldViewProjectionMatrix[instance]);
}

#endif
#ifdef GLSL

// <Semantic Name='POSITION' Attribute='a_position' />
// <Semantic Name='NORMAL' Attribute='a_normal' />
// <Semantic Name='COLOR' Attribute='a_color' />
// <Semantic Name='TEXCOORD' Attribute='a_texcoord' />
#ifndef USE_SKINNING
// <Semantic Name='INSTANCE' Attribute='a_instance' />
#endif
// <Semantic Name='BLENDINDICES' Attribute='a_joints' />
// <Semantic Name='BLENDWEIGHTS' Attribute='a_weights' />

// Transform
uniform mat4 u_worldMatrix[MAX_INSTANCES_COUNT];
uniform mat4 u_worldViewProjectionMatrix[MAX_INSTANCES_COUNT];

#ifdef USE_SKINNING
uniform mat4 u_jointMatrices[MAX_JOINTS_COUNT];
#endif

// Material
uniform vec4 u_materialColor;

// Emission color
uniform vec4 u_emissionColor;

// Light
uniform vec3 u_ambientLightColor;
uniform vec3 u_diffuseLightColor1;
uniform vec3 u_directionToLight1;
uniform vec3 u_diffuseLightColor2;
uniform vec3 u_directionToLight2;

// Fog
uniform vec3 u_worldUp;
uniform float u_fogYMultiplier;
uniform vec3 u_fogBottomTopDensity;
uniform vec2 u_hazeStartDensity;

// Inputs and outputs
attribute vec3 a_position;
attribute vec3 a_normal;
attribute vec2 a_texcoord;
#ifndef USE_SKINNING
attribute float a_instance;
#endif
#ifdef USE_SKINNING
attribute vec4 a_joints;
attribute vec4 a_weights;
#endif
varying vec4 v_color;
varying vec2 v_texcoord;
varying float v_fog;

float fogIntegral(float y)
{
	return smoothstep(u_fogBottomTopDensity.x, u_fogBottomTopDensity.y, y) * (u_fogBottomTopDensity.y - u_fogBottomTopDensity.x) + u_fogBottomTopDensity.x;
}

float calculateFog(vec3 position, int instance)
{
	vec3 worldPosition = vec3(u_worldMatrix[instance] * vec4(position, 1.0));
	float worldY = dot(worldPosition, u_worldUp);
	float fogDistance = sqrt((u_fogYMultiplier * u_fogYMultiplier - 1.0) * (worldY * worldY) + dot(worldPosition, worldPosition));	// Reduce Y component of worldPosition in distance calculation
	float fogFactor = (fogIntegral(0.0) - fogIntegral(worldY)) / (0.0 - worldY);
	return clamp(clamp(u_hazeStartDensity.y * (fogDistance - u_hazeStartDensity.x), 0.0, 1.0) + fogFactor * u_fogBottomTopDensity.z * fogDistance, 0.0, 1.0);
}

#ifdef USE_SKINNING
mat4 getSkinningMatrix(vec4 joints, vec4 weights)
{
	mat4 skin = mat4(0.0);
	skin += weights.x * u_jointMatrices[int(joints.x)];
	skin += weights.y * u_jointMatrices[int(joints.y)];
	skin += weights.z * u_jointMatrices[int(joints.z)];
	skin += weights.w * u_jointMatrices[int(joints.w)];
	return skin;
}
#endif

void main()
{
	// Texture
	v_texcoord = a_texcoord;

	// Instancing (skinned models always use instance 0)
#ifdef USE_SKINNING
	int instance = 0;
#else
	int instance = int(a_instance);
#endif

	// Apply skinning if enabled
	vec3 skinnedPosition = a_position;
	vec3 skinnedNormal = a_normal;

#ifdef USE_SKINNING
	mat4 skinMatrix = getSkinningMatrix(a_joints, a_weights);
	skinnedPosition = vec3(skinMatrix * vec4(a_position, 1.0));
	skinnedNormal = normalize(mat3(skinMatrix) * a_normal);
#endif

	// Normal
	vec3 worldNormal = normalize(vec3(u_worldMatrix[instance] * vec4(skinnedNormal, 0.0)));

	// Lighting
	v_color = u_materialColor;
	vec3 lightColor = u_ambientLightColor;
	lightColor += u_diffuseLightColor1 * max(dot(u_directionToLight1, worldNormal), 0.0);
	lightColor += u_diffuseLightColor2 * max(dot(u_directionToLight2, worldNormal), 0.0);
	v_color = vec4(lightColor, 1) * u_materialColor;

	// Emission color
	v_color += u_emissionColor;

	// Fog
	v_fog = calculateFog(skinnedPosition, instance);

	// Position
	gl_Position = u_worldViewProjectionMatrix[instance] * vec4(skinnedPosition, 1.0);

	// Fix gl_Position
	OPENGL_POSITION_FIX;
}

#endif

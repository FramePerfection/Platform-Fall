import ChaosGraphics.ParticleSystem;
import *Methods, *Variables, *Defines from ChaosGraphics.TransparencyMask;

void ps_blend(
	vec2 inTex : TEXCOORD0,
	vec4 inColor : COLOR0,
	float additive : ADDITIVE,
	out vec4 col : COLOR0
) {
	col = texture(tex, inTex) * inColor;
	col.rgb *= col.a;
	col.a *= additive;
}

void vs_passAdditive(
	vec4 inData : PARTICLE_TEXOFFSET,
	out float additive : ADDITIVE
) {
	additive = 1.0 - inData.z;
}

void ps_Mask(
	vec4 inColor : COLOR0,
	vec2 maskTexCoord : TEXCOORD0
) {	
	vec4 col = texture(tex, maskTexCoord) * inColor;
	if (col.a < maskTexBias)
		discard;
	ps_createTransparencyMask();
}

Pass Mask {
	Enable(CullFace, false);
	VertexShader = vs_createParticleInstanced;
	PixelShader = ps_Mask;
}

Pass All {
	Enable(Blend, true);
	BlendFuncSeperate(One, OneMinusSrcAlpha, OneMinusDstAlpha, One);
	Enable(CullFace, false);
	Enable(DepthTest, true);
	DepthMask(false);
	VertexShader = vs_createParticleInstanced, vs_passAdditive;
	PixelShader = ps_blend;
}
shader_type canvas_item;

//https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Colorspace-Conversion-Node.html

void Unity_ColorspaceConversion_HSV_RGB_float(vec3 In, out vec3 Out)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 P = abs(fract(In.xxx + K.xyz) * 6.0 - K.www);
    Out = In.z * mix(K.xxx, clamp(P - K.xxx, 0, 1), In.y);
}

//void Unity_ColorspaceConversion_HSV_Linear_float(vec3 In, out vec3 Out)
//{
    //vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    //vec3 P = abs(fract(In.xxx + K.xyz) * 6.0 - K.www);
    //vec3 RGB = In.z * mix(K.xxx, clamp(P - K.xxx, 0, 1), In.y);
    //vec3 linearRGBLo = RGB / 12.92;
    //vec3 linearRGBHi = pow(max(abs((RGB + 0.055) / 1.055), 1.192092896e-07), vec3(2.4, 2.4, 2.4));
    //Out = vec3(RGB.any <= 0.04045) ? linearRGBLo : linearRGBHi;
//}


void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.
	vec3 blub;
	float uvX = UV.x;
	Unity_ColorspaceConversion_HSV_RGB_float(vec3(uvX,1,1), blub);
	COLOR = vec4(blub, 1);
}

void light() {
	// Called for every pixel for every light affecting the CanvasItem.
}

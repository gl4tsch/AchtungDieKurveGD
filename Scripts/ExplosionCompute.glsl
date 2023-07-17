#[compute]
#version 460

layout(local_size_x = 16, local_size_y = 1, local_size_z = 1) in;

// Prepare memory for the image, which will be both read and written to
// `restrict` is used to tell the compiler that the memory will only be accessed
// by this variable.
layout(r8, binding = 0) restrict uniform image2D arena;

struct GLSLExplodyPixelData
{
	float xPos, yPos;
    float xDir, yDir;
    float r, g, b;
};

layout(set = 0, binding = 1, std430) restrict buffer ExplodyPixelBuffer {
    GLSLExplodyPixelData pixels[];
}
explodyPixelBuffer;

layout(set = 0, binding = 2, std430) restrict buffer Params {
	float deltaTime;
}
params;

void main() {

	uint pxIdx = gl_GlobalInvocationID.x;
	if (pxIdx >= explodyPixelBuffer.pixels.length()) {
		return;
	}
	GLSLExplodyPixelData px = explodyPixelBuffer.pixels[pxIdx];
	vec2 pos = vec2(px.xPos, px.yPos);
	ivec2 coords = ivec2(pos.x, pos.y);
	vec4 color = vec4(px.r, px.g, px.b, 0.5);

	// remove old pixel if still there
	if (imageLoad(arena, coords).rgb == color.rgb)
	{
		//imageStore(arena, coords, vec4(0,0,0,0));
	}

	// calculate new position and velocity
	vec2 moveDirection = vec2(px.xDir, px.yDir);
	vec2 newPos = pos + moveDirection; // * params.deltaTime * 100.0;
	explodyPixelBuffer.pixels[pxIdx].xPos = newPos.x;
	explodyPixelBuffer.pixels[pxIdx].yPos = newPos.y;

	// draw new pixel
	ivec2 newCoords = ivec2(newPos.x, newPos.y);
	imageStore(arena, newCoords, color);
}
#[compute]
#version 460

layout(local_size_x = 16, local_size_y = 1, local_size_z = 1) in;

// Prepare memory for the image, which will be both read and written to
// `restrict` is used to tell the compiler that the memory will only be accessed
// by this variable.
layout(r8, binding = 0) restrict uniform image2D arenaIn;
layout(r8, binding = 1) restrict uniform image2D arenaOut;

struct GLSLExplodyPixelData
{
	float xPos, yPos;
    float xDir, yDir;
    float r, g, b;
};

layout(set = 0, binding = 2, std430) restrict buffer ExplodyPixelBuffer
{
    GLSLExplodyPixelData pixels[];
}
explodyPixelBuffer;

layout(set = 0, binding = 3, std430) restrict buffer Params
{
	float deltaTime;
}
params;

float pxMoveSpeed = 200.0;
float pxSpeedDecay = 10; //
float pxAlpha = 0.8;
float pxEqualityThreshold = 0.01;

void main()
{
	uint pxIdx = gl_GlobalInvocationID.x;
	if (pxIdx >= explodyPixelBuffer.pixels.length())
	{
		return;
	}

	GLSLExplodyPixelData px = explodyPixelBuffer.pixels[pxIdx];
	vec2 pos = vec2(px.xPos, px.yPos);
	ivec2 coords = ivec2(pos.x, pos.y);
	vec4 color = vec4(px.r, px.g, px.b, pxAlpha);

	// calculate new position
	vec2 moveDirection = vec2(px.xDir, px.yDir);
	vec2 newPos = pos + moveDirection * params.deltaTime * pxMoveSpeed;
	explodyPixelBuffer.pixels[pxIdx].xPos = newPos.x;
	explodyPixelBuffer.pixels[pxIdx].yPos = newPos.y;
	ivec2 newCoords = ivec2(newPos.x, newPos.y);
	// calculate new velocity
	moveDirection *= clamp(1.0 - pxSpeedDecay * params.deltaTime, 0.0, 1.0);

	explodyPixelBuffer.pixels[pxIdx].xDir = moveDirection.x;
	explodyPixelBuffer.pixels[pxIdx].yDir = moveDirection.y;

	// remove old pixel if still there
	vec4 oldPxColor = imageLoad(arenaIn, coords);
	if (distance(oldPxColor, color) < pxEqualityThreshold)
	{
		imageStore(arenaOut, coords, vec4(0,0,0,0));
	}

	// draw new pixel if there is space
	vec4 newPxColor = imageLoad(arenaIn, newCoords);
	if (newPxColor.a < 0.98)
	{
		imageStore(arenaOut, newCoords, color);
	}
}
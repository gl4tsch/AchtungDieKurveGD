#[compute]
#version 460

// Instruct the GPU to use 8x8x1 = 64 local invocations per workgroup.
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Prepare memory for the image, which will be both read and written to
// `restrict` is used to tell the compiler that the memory will only be accessed
// by this variable.
layout(r8, binding = 0) restrict uniform image2D arena;

struct GLSLSnakeData
{
    int prevPosX, prevPosY, newPosX, newPosY;
    int halfThickness;
    float colorR, colorG, colorB, colorA;
    int collision; // bool
};

layout(set = 0, binding = 1, std430) restrict buffer SnakeBuffer
{
    GLSLSnakeData[] snakes;
} snakeBuffer;

float sdSegment( vec2 p, vec2 a, vec2 b )
{
    vec2 pa = p-a, ba = b-a;
    float h = clamp( dot(pa,ba) / dot(ba,ba), 0.0, 1.0 );
    return length( pa - ba*h );
}

void main()
{
	// Grab the current pixel's position from the ID of this specific invocation ("thread").
	ivec2 coords = ivec2(gl_GlobalInvocationID.xy);
	ivec2 dimensions = imageSize(arena);
	vec4 pixel = imageLoad(arena, coords);

	for (int i = 0; i < snakeBuffer.snakes.length(); i++)
	{
		GLSLSnakeData snake = snakeBuffer.snakes[i];
		vec2 prevPos = vec2(snake.prevPosX, snake.prevPosY);
		vec2 newPos = vec2(snake.newPosX, snake.newPosY);
		vec4 color = vec4(snake.colorR, snake.colorG, snake.colorB, snake.colorA);
		float distToSegment = sdSegment(coords, prevPos, newPos);

		if (distToSegment <= snake.halfThickness)
		{
			imageStore(arena, coords, color);
		}
	}
}

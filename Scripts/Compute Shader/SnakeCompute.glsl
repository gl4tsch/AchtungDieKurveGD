#[compute]
#version 460

// Instruct the GPU to use 8x8x1 = 64 local invocations per workgroup.
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

// Prepare memory for the image, which will be both read and written to
// `restrict` is used to tell the compiler that the memory will only be accessed
// by this variable.
layout(r8, binding = 0) uniform image2D arena;

struct LineData
{
    float prevPosX, prevPosY, newPosX, newPosY;
    float halfThickness;
    float colorR, colorG, colorB, colorA;
	int clipMode;
};

// snake draw data inpt
layout(set = 0, binding = 1, std430) restrict readonly buffer SnakeBuffer
{
	uint snakeCount;
    LineData[] snakes;
} snakeBuffer;

// collision output
layout(set = 0, binding = 2, std430) restrict writeonly buffer CollisionBuffer
{
	int[] collisions;
} collisionBuffer;

// additional line draw data input
layout(set = 0, binding = 3, std430) restrict readonly buffer LineBuffer
{
	uint lineCount;
	LineData[] lines;
} lineBuffer;

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

	// SNAKES
	for (int i = 0; i < snakeBuffer.snakeCount; i++)
	{
		LineData snake = snakeBuffer.snakes[i];
		vec2 prevPos = vec2(snake.prevPosX, snake.prevPosY);
		vec2 newPos = vec2(snake.newPosX, snake.newPosY);
		vec4 color = vec4(snake.colorR, snake.colorG, snake.colorB, snake.colorA);
		float distToSegment = sdSegment(coords, prevPos, newPos);

		if (distToSegment <= snake.halfThickness)
		{
			// if the pixel is not the end of same snake from previous frame && the pixel has a value already
			float distToStart = length(prevPos - coords);
            if (distToStart > snake.halfThickness && pixel.a == 1)
            {
                // COLLISION
                collisionBuffer.collisions[i] = 1;
            }

			// draw pixel
			imageStore(arena, coords, color);
		}
	}

	// LINES
	for (int i = 0; i < lineBuffer.lineCount; i++)
	{
		LineData line = lineBuffer.lines[i];
		vec2 posA = vec2(line.prevPosX, line.prevPosY);
		vec2 posB = vec2(line.newPosX, line.newPosY);
		vec4 color = vec4(line.colorR, line.colorG, line.colorB, line.colorA);
		float distToSegment = sdSegment(coords, posA, posB);

		if (distToSegment <= line.halfThickness)
		{
			// clip ends
			// clip a
			if ((line.clipMode == 1 || line.clipMode == 3) && length(posA - coords) <= line.halfThickness)
			{
				continue;
			}
			if ((line.clipMode == 2 || line.clipMode == 3) && length(posB - coords) <= line.halfThickness)
			{
				continue;
			}

			// fill pixel
			imageStore(arena, coords, color);
		}
	}
}

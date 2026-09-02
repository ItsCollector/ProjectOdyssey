// shader.frag
#version 410 core

in vec2 vTexCoord;
out vec4 FragColor;

uniform sampler2D uTexture;
uniform vec4 uColor;
uniform int uUseTexture;

void main()
{
    if (uUseTexture == 0)
    {
        FragColor = uColor;
    }
    else if (uUseTexture == 1)
    {
        FragColor = texture(uTexture, vTexCoord);
    }
    else
    {
        FragColor = texture(uTexture, vTexCoord) * uColor;
    }
}
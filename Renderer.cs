using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace ProjectOdyssey
{
    public class Renderer
    {
        private int vao;
        private int vbo;
        private int ebo;
        private Shader shader;
        private Matrix4 projection;

        public Renderer()
        {
            SetupMesh();
            shader = new Shader("shader/shader.vert", "shader/shader.frag");
        }

        private void SetupMesh()
        {
            float[] vertices =
            {
                -0.5f, -0.5f,  0.0f, 0.0f,
                 0.5f, -0.5f,  1.0f, 0.0f,
                 0.5f,  0.5f,  1.0f, 1.0f,
                -0.5f,  0.5f,  0.0f, 1.0f
            };

            uint[] indices =
            {
                0, 1, 3,
                1, 2, 3
            };

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            // Vertex Position attribute
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // UV attribute
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
        }

        public void Intitialise()
        {
            shader.Use();
            GL.BindVertexArray(vao);

            shader.SetMatrix4("projection", projection);
            shader.SetInt("uTexture", 0); 
            shader.SetInt("uUseTexture", 0);

            GL.ActiveTexture(TextureUnit.Texture0);
        }

        public void Draw(Texture texture, float xPosition, float yPosition, float width = -1, float height = -1)
        {
            float textureWidth = (width == -1) ? texture.width : width;
            float textureHeight = (height == -1) ? texture.height : height;

            GL.BindTexture(TextureTarget.Texture2D, texture.handle);

            shader.SetVector2("uPosition", xPosition, yPosition);
            shader.SetVector2("uSize", textureWidth, textureHeight);

            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        }

        public void Resize(int width, int height)
        {
            projection = Matrix4.CreateOrthographicOffCenter(0f, width, height, 0f, -1f, 1f);
        }

        public void Dispose()
        {
            shader.Dispose();
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(vao);
        }
    }
}

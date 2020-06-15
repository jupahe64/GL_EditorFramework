using GL_EditorFramework;
using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static GL_EditorFramework.Framework;

namespace GL_EditorFramework
{
    public static class Renderers
    {
        private static void Face(ref float[][] points, ref List<float> data, int p1, int p2, int p4, int p3)
        {
            data.AddRange(new float[] {
                points[p1][0], points[p1][1], points[p1][2],
                points[p2][0], points[p2][1], points[p2][2],
                points[p3][0], points[p3][1], points[p3][2],
                points[p4][0], points[p4][1], points[p4][2]
            });
        }

        private static void LineFace(ref float[][] points, ref List<float> data, int p1, int p2, int p4, int p3)
        {
            data.AddRange(new float[] {
                points[p1][0], points[p1][1], points[p1][2],
                points[p2][0], points[p2][1], points[p2][2],
                points[p2][0], points[p2][1], points[p2][2],
                points[p3][0], points[p3][1], points[p3][2],
                points[p3][0], points[p3][1], points[p3][2],
                points[p4][0], points[p4][1], points[p4][2],
                points[p4][0], points[p4][1], points[p4][2],
                points[p1][0], points[p1][1], points[p1][2]
            });
        }

        private static void Line(ref float[][] points, ref List<float> data, int p1, int p2)
        {
            data.AddRange(new float[] {
                points[p1][0], points[p1][1], points[p1][2],
                points[p2][0], points[p2][1], points[p2][2]
            });
        }

        private static void FaceInv(ref float[][] points, ref List<float> data, int p2, int p1, int p3, int p4)
        {
            data.AddRange(new float[] {
                points[p1][0], points[p1][1], points[p1][2],
                points[p2][0], points[p2][1], points[p2][2],
                points[p3][0], points[p3][1], points[p3][2],
                points[p4][0], points[p4][1], points[p4][2]
            });
        }

        public static class ColorBlockRenderer
        {
            private static bool Initialized = false;

            public static ShaderProgram DefaultShaderProgram { get; private set; }

            private static VertexArrayObject linesVao;
            private static VertexArrayObject blockVao;
            
            private static int lineDrawList;

            public static float[][] points = new float[][]
            {
                new float[]{-1,-1, 1},
                new float[]{ 1,-1, 1},
                new float[]{-1, 1, 1},
                new float[]{ 1, 1, 1},
                new float[]{-1,-1,-1},
                new float[]{ 1,-1,-1},
                new float[]{-1, 1,-1},
                new float[]{ 1, 1,-1}
            };


            public static void Initialize()
            {
                if (!Initialized)
                {
                    var defaultFrag = new FragmentShader(
                        @"#version 330
                        uniform sampler2D tex;
                        in vec4 fragColor;
                        in vec3 fragPosition;
                        in vec2 uv;
                
                        void main(){
                            gl_FragColor = fragColor*((fragPosition.y+2)/3)*texture(tex, uv, 100);
                        }");
                    var defaultVert = new VertexShader(
                        @"#version 330
                        layout(location = 0) in vec4 position;
                        uniform vec4 color;
                        uniform mat4 mtxMdl;
                        uniform mat4 mtxCam;
                        out vec4 fragColor;
                        out vec3 fragPosition;
                        out vec2 uv;

                        vec2 map(vec2 value, vec2 min1, vec2 max1, vec2 min2, vec2 max2) {
                            return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
                        }

                        void main(){
                            fragPosition = position.xyz;
                            uv = map(fragPosition.xz,vec2(-1.0625,-1.0625),vec2(1.0625,1.0625), vec2(0.5,0.5), vec2(0.75,1.0));
                            gl_Position = mtxCam*mtxMdl*position;
                            fragColor = color;
                        }");
                    DefaultShaderProgram = new ShaderProgram(defaultFrag, defaultVert);
                    
                    int buffer;

                    #region block
                    buffer = GL.GenBuffer();
                    
                    blockVao = new VertexArrayObject(buffer);
                    blockVao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);
                    blockVao.Submit();

                    List<float> list = new List<float>();
                    Face(ref points, ref list, 0, 1, 2, 3);
                    FaceInv(ref points, ref list, 4, 5, 6, 7);
                    FaceInv(ref points, ref list, 0, 1, 4, 5);
                    Face(ref points, ref list, 2, 3, 6, 7);
                    Face(ref points, ref list, 0, 2, 4, 6);
                    FaceInv(ref points, ref list, 1, 3, 5, 7);

                    float[] data = list.ToArray();
                    GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);


                    #endregion

                    #region lines
                    buffer = GL.GenBuffer();

                    linesVao = new VertexArrayObject(buffer);
                    linesVao.AddAttribute(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);
                    linesVao.Submit();

                    list = new List<float>();
                    LineFace(ref points, ref list, 0, 1, 2, 3);
                    LineFace(ref points, ref list, 4, 5, 6, 7);
                    Line(ref points, ref list, 0, 4);
                    Line(ref points, ref list, 1, 5);
                    Line(ref points, ref list, 2, 6);
                    Line(ref points, ref list, 3, 7);

                    data = list.ToArray();
                    GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * data.Length, data, BufferUsageHint.StaticDraw);

                    #endregion

                    #region legacy lines
                    lineDrawList = GL.GenLists(1);

                    GL.NewList(lineDrawList, ListMode.Compile);
                    GL.Begin(PrimitiveType.LineStrip);
                    GL.Vertex3(points[6]);
                    GL.Vertex3(points[2]);
                    GL.Vertex3(points[3]);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[6]);

                    GL.Vertex3(points[4]);
                    GL.Vertex3(points[5]);
                    GL.Vertex3(points[1]);
                    GL.Vertex3(points[0]);
                    GL.Vertex3(points[4]);
                    GL.End();

                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex3(points[2]);
                    GL.Vertex3(points[0]);
                    GL.Vertex3(points[3]);
                    GL.Vertex3(points[1]);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[5]);
                    GL.End();
                    GL.EndList();
                    #endregion

                    Initialized = true;
                }
            }

            public static void Draw(GL_ControlModern control, Pass pass, Vector4 boxColor, Vector4 outlineColor, Vector4 pickingColor)
            {
                if (pass == Pass.OPAQUE)
                {
                    control.CurrentShader = DefaultShaderProgram;

                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, Framework.TextureSheet);

                    DefaultShaderProgram.SetVector4("color", boxColor);

                    blockVao.Use(control);
                    GL.DrawArrays(PrimitiveType.Quads, 0, 24);

                    #region outlines
                    GL.LineWidth(1);
                    control.CurrentShader = SolidColorShaderProgram;
                    SolidColorShaderProgram.SetVector4("color", outlineColor);

                    linesVao.Use(control);
                    GL.DrawArrays(PrimitiveType.Lines, 0, 24);
                    GL.LineWidth(2);
                    #endregion
                }
                else
                {
                    control.CurrentShader = SolidColorShaderProgram;
                    SolidColorShaderProgram.SetVector4("color", pickingColor);

                    blockVao.Use(control);
                    GL.DrawArrays(PrimitiveType.Quads, 0, 24);
                }
            }

            public static void Draw(GL_ControlLegacy control, Pass pass, Vector4 boxColor, Vector4 lineColor, Vector4 pickingColor)
            {
                if (pass == Pass.OPAQUE)
                {
                    GL.Enable(EnableCap.Texture2D);

                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, Framework.TextureSheet);

                    #region draw textured faces
                    Vector4 darkerColor = boxColor * 0.71875f;
                    GL.Begin(PrimitiveType.Quads);
                    GL.Color4(boxColor);
                    GL.TexCoord2(0.71875f, 0.515625f);
                    GL.Vertex3(points[7]);
                    GL.TexCoord2(0.53125f, 0.515625f);
                    GL.Vertex3(points[6]);
                    GL.TexCoord2(0.53125f, 0.984375f);
                    GL.Vertex3(points[2]);
                    GL.TexCoord2(0.71875f, 0.984375f);
                    GL.Vertex3(points[3]);

                    GL.Color4(darkerColor);
                    GL.TexCoord2(0.53125f, 0.515625f);
                    GL.Vertex3(points[4]);
                    GL.TexCoord2(0.71875f, 0.515625f);
                    GL.Vertex3(points[5]);
                    GL.TexCoord2(0.71875f, 0.984375f);
                    GL.Vertex3(points[1]);
                    GL.TexCoord2(0.53125f, 0.984375f);
                    GL.Vertex3(points[0]);
                    GL.End();

                    GL.Begin(PrimitiveType.QuadStrip);
                    GL.TexCoord2(0.71875f, 0.515625f);
                    GL.Color4(boxColor);
                    GL.Vertex3(points[7]);
                    GL.Color4(darkerColor);
                    GL.Vertex3(points[5]);
                    GL.Color4(boxColor);
                    GL.Vertex3(points[6]);
                    GL.Color4(darkerColor);
                    GL.Vertex3(points[4]);
                    GL.Color4(boxColor);
                    GL.Vertex3(points[2]);
                    GL.Color4(darkerColor);
                    GL.Vertex3(points[0]);
                    GL.Color4(boxColor);
                    GL.Vertex3(points[3]);
                    GL.Color4(darkerColor);
                    GL.Vertex3(points[1]);
                    GL.Color4(boxColor);
                    GL.Vertex3(points[7]);
                    GL.Color4(darkerColor);
                    GL.Vertex3(points[5]);
                    GL.End();
                    #endregion

                    GL.Disable(EnableCap.Texture2D);

                    GL.LineWidth(1);
                    GL.Color4(lineColor);
                    GL.CallList(lineDrawList);
                    GL.LineWidth(2);
                }
                else if(pass == Pass.PICKING)
                {
                    #region draw colored faces
                    GL.Color4(pickingColor);
                    GL.Begin(PrimitiveType.Quads);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[6]);
                    GL.Vertex3(points[2]);
                    GL.Vertex3(points[3]);

                    GL.Vertex3(points[4]);
                    GL.Vertex3(points[5]);
                    GL.Vertex3(points[1]);
                    GL.Vertex3(points[0]);
                    GL.End();

                    GL.Begin(PrimitiveType.QuadStrip);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[5]);
                    GL.Vertex3(points[6]);
                    GL.Vertex3(points[4]);
                    GL.Vertex3(points[2]);
                    GL.Vertex3(points[0]);
                    GL.Vertex3(points[3]);
                    GL.Vertex3(points[1]);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[5]);
                    GL.End();
                    #endregion
                }
            }

            public static void DrawWithoutTextures(GL_ControlModern control, Pass pass, Vector4 boxColor, Vector4 lineColor, Vector4 pickingColor)
            {
                control.CurrentShader = SolidColorShaderProgram;

                if (pass == Pass.OPAQUE)
                {
                    SolidColorShaderProgram.SetVector4("color", boxColor);

                    blockVao.Use(control);
                    GL.DrawArrays(PrimitiveType.Quads, 0, 24);

                    #region outlines
                    GL.LineWidth(1);

                    SolidColorShaderProgram.SetVector4("color", lineColor);

                    linesVao.Use(control);
                    GL.DrawArrays(PrimitiveType.Lines, 0, 24);
                    GL.LineWidth(2);
                    #endregion
                }
                else
                {
                    SolidColorShaderProgram.SetVector4("color", pickingColor);

                    blockVao.Use(control);
                    GL.DrawArrays(PrimitiveType.Quads, 0, 24);
                }
            }

            public static void DrawWithoutTextures(GL_ControlLegacy control, Pass pass, Vector4 boxColor, Vector4 lineColor, Vector4 pickingColor)
            {
                if (pass == Pass.OPAQUE)
                {
                    GL.Enable(EnableCap.Texture2D);

                    #region draw colored faces
                    GL.Color4(boxColor);
                    GL.Begin(PrimitiveType.Quads);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[6]);
                    GL.Vertex3(points[2]);
                    GL.Vertex3(points[3]);

                    GL.Vertex3(points[4]);
                    GL.Vertex3(points[5]);
                    GL.Vertex3(points[1]);
                    GL.Vertex3(points[0]);
                    GL.End();

                    GL.Begin(PrimitiveType.QuadStrip);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[5]);
                    GL.Vertex3(points[6]);
                    GL.Vertex3(points[4]);
                    GL.Vertex3(points[2]);
                    GL.Vertex3(points[0]);
                    GL.Vertex3(points[3]);
                    GL.Vertex3(points[1]);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[5]);
                    GL.End();
                    #endregion

                    GL.LineWidth(1);
                    GL.Color4(lineColor);
                    GL.CallList(lineDrawList);
                    GL.LineWidth(2);
                }
                else if (pass == Pass.PICKING)
                {
                    #region draw colored faces
                    GL.Color4(pickingColor);
                    GL.Begin(PrimitiveType.Quads);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[6]);
                    GL.Vertex3(points[2]);
                    GL.Vertex3(points[3]);

                    GL.Vertex3(points[4]);
                    GL.Vertex3(points[5]);
                    GL.Vertex3(points[1]);
                    GL.Vertex3(points[0]);
                    GL.End();

                    GL.Begin(PrimitiveType.QuadStrip);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[5]);
                    GL.Vertex3(points[6]);
                    GL.Vertex3(points[4]);
                    GL.Vertex3(points[2]);
                    GL.Vertex3(points[0]);
                    GL.Vertex3(points[3]);
                    GL.Vertex3(points[1]);
                    GL.Vertex3(points[7]);
                    GL.Vertex3(points[5]);
                    GL.End();
                    #endregion

                    GL.LineWidth(1);
                    GL.CallList(lineDrawList);
                    GL.LineWidth(2);
                }
            }

            public static void DrawLineBox(GL_ControlModern control, Pass pass, Vector4 color, Vector4 pickingColor)
            {
                control.CurrentShader = SolidColorShaderProgram;

                if (pass == Pass.OPAQUE)
                {
                    SolidColorShaderProgram.SetVector4("color", color);
                }
                else
                {
                    GL.LineWidth(4);
                    SolidColorShaderProgram.SetVector4("color", pickingColor);
                }

                
                linesVao.Use(control);
                GL.DrawArrays(PrimitiveType.Lines, 0, 24);
                GL.LineWidth(2);
            }

            public static void DrawLineBox(GL_ControlLegacy control, Pass pass, Vector4 color, Vector4 pickingColor)
            {
                GL.Disable(EnableCap.Texture2D);

                if (pass == Pass.OPAQUE)
                {
                    GL.Color4(color);
                }
                else
                {
                    GL.LineWidth(6);
                    GL.Color4(pickingColor);
                }

                GL.CallList(lineDrawList);
                GL.LineWidth(2);
                GL.Enable(EnableCap.Texture2D);
            }
        }
    }
}

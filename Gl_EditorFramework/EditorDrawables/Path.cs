using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GL_EditorFramework.GL_Core;
using GL_EditorFramework.Interfaces;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using WinInput = System.Windows.Input;
using static GL_EditorFramework.EditorDrawables.EditorSceneBase;
using static GL_EditorFramework.Renderers;

namespace GL_EditorFramework.EditorDrawables
{
    public class Path : EditableObject
    {
        private static bool Initialized = false;
        private static bool InitializedLegacy = false;
        private static ShaderProgram defaultShaderProgram;
        private static ShaderProgram bezierCurveShaderProgram;
        private static int lineColorLoc, gapIndexLoc, isPickingModeLoc;

        private static int drawLists;

        private int pathPointVao;
        private int pathPointBuffer;
        public List<PathPoint> PathPoints;

        public bool Closed = false;

        public new static Vector4 hoverColor = new Vector4(1, 1, 0.925f, 1);
        public new static Vector4 selectColor = new Vector4(1, 1f, 0.5f, 1);

        public Path(List<PathPoint> pathPoints)
        {
            this.PathPoints = pathPoints;
        }

        public override string ToString() => "path";

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if(pass == Pass.PICKING)
                GL.LineWidth(4f);
            else
                GL.LineWidth(1f);

            GL.BindVertexArray(pathPointVao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, pathPointBuffer);

            Quaternion rotation = editorScene.CurrentAction.NewRot(Quaternion.Identity);

            Vector3 scale = editorScene.CurrentAction.NewScale(Vector3.One);

            float[] data = new float[PathPoints.Count * 12]; //px, py, pz, pCol, cp1x, cp1y, cp1z, cp1Col,  cp2x, cp2y, cp2z, cp2Col

            int i = 0;
            int index = 0;
            control.CurrentShader = bezierCurveShaderProgram;
            
            Vector4 col;
            Vector3 pos;
            if (pass == Pass.OPAQUE)
            {
                int part = 1;

                int randomColor = control.RNG.Next();
                Vector4 color = new Vector4(
                    ((randomColor >> 16) & 0xFF) / 255f,
                    ((randomColor >> 8) & 0xFF) / 255f,
                    (randomColor & 0xFF) / 255f,
                    1f
                    );

                GL.Uniform4(lineColorLoc, color);
                GL.Uniform1(gapIndexLoc, Closed?-1:PathPoints.Count-1);
                GL.Uniform1(isPickingModeLoc, 0);

                foreach (PathPoint point in PathPoints)
                {
                    if (point.Selected)
                        pos = editorScene.CurrentAction.NewPos(point.position);
                    else
                        pos = point.position;

                    data[i] =     pos.X;
                    data[i + 1] = pos.Y;
                    data[i + 2] = pos.Z;

                    if (point.Selected)
                        col = selectColor;
                    else
                        col = color;

                    data[i + 3] =  BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 255),
                    (byte)(col.Y * 255),
                    (byte)(col.Z * 255),
                    (byte)(col.W * 255)}, 0);

                    part++;


                    if (point.controlPoint1 != Vector3.Zero && editorScene.ExclusiveAction!=NoAction && 
                        editorScene.Hovered == this && editorScene.HoveredPart==part)

                        pos = editorScene.ExclusiveAction.NewPos(point.position + point.controlPoint1);

                    else if (point.Selected)
                        pos = editorScene.CurrentAction.NewPos(point.position) + rotation * (point.controlPoint1 * scale);
                    else
                        pos = point.position + point.controlPoint1;

                    data[i + 4] =  pos.X;
                    data[i + 5] =  pos.Y;
                    data[i + 6] =  pos.Z;
                    data[i + 7] =  BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 255),
                    (byte)(col.Y * 255),
                    (byte)(col.Z * 255),
                    (byte)(col.W * 255)}, 0);

                    if(point.controlPoint1!=Vector3.Zero)
                        part++;


                    if (point.controlPoint2 != Vector3.Zero && editorScene.ExclusiveAction != NoAction &&
                        editorScene.Hovered == this && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(point.position + point.controlPoint2);

                    else if (point.Selected)
                        pos = editorScene.CurrentAction.NewPos(point.position) + rotation * (point.controlPoint2 * scale);
                    else
                        pos = point.position + point.controlPoint2;

                    data[i + 8] =  pos.X;
                    data[i + 9] =  pos.Y;
                    data[i + 10] = pos.Z;
                    data[i + 11] = BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 255),
                    (byte)(col.Y * 255),
                    (byte)(col.Z * 255),
                    (byte)(col.W * 255)}, 0);

                    if (point.controlPoint2 != Vector3.Zero)
                        part++;


                    i += 12;
                    index++;
                }
            }
            else
            {
                GL.Uniform4(lineColorLoc, control.NextPickingColor());
                GL.Uniform1(gapIndexLoc, Closed ? -1 : PathPoints.Count - 1);
                GL.Uniform1(isPickingModeLoc, 1);

                foreach (PathPoint point in PathPoints)
                {
                    pos = point.position;

                    data[i] = pos.X;
                    data[i + 1] = pos.Y;
                    data[i + 2] = pos.Z;

                    col = control.NextPickingColor();

                    data[i + 3] = BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 256),
                    (byte)(col.Y * 256),
                    (byte)(col.Z * 256),
                    (byte)(col.W * 256)}, 0);

                    if(point.controlPoint1!=Vector3.Zero)
                        col = control.NextPickingColor();

                    pos = point.position + point.controlPoint1;

                    data[i + 4] = pos.X;
                    data[i + 5] = pos.Y;
                    data[i + 6] = pos.Z;
                    data[i + 7] = BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 256),
                    (byte)(col.Y * 256),
                    (byte)(col.Z * 256),
                    (byte)(col.W * 256)}, 0);

                    if (point.controlPoint2 != Vector3.Zero)
                        col = control.NextPickingColor();

                    pos = point.position + point.controlPoint2;

                    data[i + 8] = pos.X;
                    data[i + 9] = pos.Y;
                    data[i + 10] = pos.Z;
                    data[i + 11] = BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 256),
                    (byte)(col.Y * 256),
                    (byte)(col.Z * 256),
                    (byte)(col.W * 256)}, 0);

                    i += 12;
                    index++;
                }
            }
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length*4, data, BufferUsageHint.DynamicDraw);

            

            control.ResetModelMatrix();
            
            GL.BindVertexArray(pathPointVao);

            GL.DrawArrays(PrimitiveType.LineLoop, 0, PathPoints.Count);

            control.CurrentShader = defaultShaderProgram;
            GL.DrawArrays(PrimitiveType.Points, 0, PathPoints.Count);
        }

        public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            GL.Disable(EnableCap.Texture2D);

            Quaternion rotation = editorScene.CurrentAction.NewRot(Quaternion.Identity);

            Vector3 scale = editorScene.CurrentAction.NewScale(Vector3.One);

            Vector4 color = new Vector4();
            if (pass == Pass.OPAQUE)
            {
                int randomColor = control.RNG.Next();
                color = new Vector4(
                        ((randomColor >> 16) & 0xFF) / 256f,
                        ((randomColor >> 8) & 0xFF) / 256f,
                        (randomColor & 0xFF) / 256f,
                        1f
                        );
                GL.Color4(color);
                GL.LineWidth(1.0f);
            }
            else
            {
                color = control.NextPickingColor();
                GL.LineWidth(2.0f);
            }

            int part = 1;

            Vector3[] positions = new Vector3[PathPoints.Count*3];

            int posIndex = 0;
            if (pass == Pass.OPAQUE)
            {
                foreach (PathPoint point in PathPoints)
                {
                    Vector3 pos;
                    if (point.Selected)
                        positions[posIndex] = pos = editorScene.CurrentAction.NewPos(point.position);
                    else
                        positions[posIndex] = pos = point.position;

                    control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) * Matrix4.CreateTranslation(pos));

                    GL.CallList(drawLists + 1); //white lines

                    if (point.Selected)
                        GL.Color4(selectColor);
                    else
                        GL.Color4(color);
                    GL.CallList(drawLists);
                    part++;



                    if (point.controlPoint1 != Vector3.Zero)
                    {
                        if (editorScene.ExclusiveAction != NoAction &&
                            editorScene.Hovered == this && editorScene.HoveredPart == part)

                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex + 1] = editorScene.ExclusiveAction.NewPos(point.position + point.controlPoint1)));
                        else if (point.Selected)
                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex + 1] = pos + rotation * (scale * point.controlPoint1)));
                        else
                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex + 1] = pos + point.controlPoint1));

                        GL.CallList(drawLists + 1); //white lines

                        if (point.Selected)
                            GL.Color4(selectColor);
                        else
                            GL.Color4(color);

                        GL.CallList(drawLists);
                        part++;
                    }
                    else
                        positions[posIndex + 1] = pos;

                    if (point.controlPoint2 != Vector3.Zero)
                    {
                        if (editorScene.ExclusiveAction != NoAction &&
                            editorScene.Hovered == this && editorScene.HoveredPart == part)

                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex+2] = editorScene.ExclusiveAction.NewPos(point.position + point.controlPoint2)));
                        else if (point.Selected)
                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex + 2] = pos + rotation * (scale * point.controlPoint2)));
                        else
                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex+2] = pos + point.controlPoint2));

                        GL.CallList(drawLists + 1); //white lines

                        if (point.Selected)
                            GL.Color4(selectColor);
                        else
                            GL.Color4(color);

                        GL.CallList(drawLists);
                        part++;
                    }
                    else
                        positions[posIndex + 2] = pos;
                    
                    posIndex += 3;
                }
            }
            else
            {
                foreach (PathPoint point in PathPoints)
                {
                    control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) * Matrix4.CreateTranslation(point.position));

                    GL.Color4(control.NextPickingColor());
                    GL.CallList(drawLists);

                    if (point.controlPoint1 != Vector3.Zero)
                    {
                        control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(point.position + point.controlPoint1));

                        GL.Color4(control.NextPickingColor());
                        GL.CallList(drawLists);
                    }

                    if (point.controlPoint2 != Vector3.Zero)
                    {
                        control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(point.position + point.controlPoint2));

                        GL.Color4(control.NextPickingColor());
                        GL.CallList(drawLists);
                    }
                }
            }

            control.ResetModelMatrix();

            GL.Color4(color);

            posIndex = 0;
            for(int i = 1; i<PathPoints.Count; i++)
            {
                GL.Begin(PrimitiveType.LineStrip);
                if (PathPoints[i-1].controlPoint2 != Vector3.Zero || PathPoints[i].controlPoint1 != Vector3.Zero)//bezierCurve
                {
                    Vector3 p0 = positions[posIndex];
                    Vector3 p1 = positions[posIndex+2];
                    Vector3 p2 = positions[posIndex+4];
                    Vector3 p3 = positions[posIndex+3];

                    if (PathPoints[i-1].controlPoint2 != Vector3.Zero)
                        GL.Vertex3(p1);

                    for (float t = 0f; t<=1.0; t += 0.125f)
                    {
                        float u = 1f - t;
                        float tt = t * t;
                        float uu = u * u;
                        float uuu = uu * u;
                        float ttt = tt * t;

                        GL.Vertex3(uuu * p0 +
                                        3 * uu * t * p1 +
                                        3 * u * tt * p2 +
                                            ttt * p3);
                    }

                    if (PathPoints[i].controlPoint1 != Vector3.Zero)
                        GL.Vertex3(p2);
                }
                else
                {
                    GL.Vertex3(positions[posIndex]);

                    GL.Vertex3(positions[posIndex+3]);
                }
                GL.End();
                posIndex += 3;
            }
            if (Closed)
            {
                GL.Begin(PrimitiveType.LineStrip);
                if (PathPoints[PathPoints.Count - 1].controlPoint2 != Vector3.Zero || PathPoints[0].controlPoint1 != Vector3.Zero)//bezierCurve
                {
                    Vector3 p0 = positions[posIndex];
                    Vector3 p1 = positions[posIndex + 2];
                    Vector3 p2 = positions[1];
                    Vector3 p3 = positions[0];

                    if (PathPoints[PathPoints.Count - 1].controlPoint2 != Vector3.Zero)
                        GL.Vertex3(p1);

                    for (float t = 0f; t <= 1.0; t += 0.25f)
                    {
                        float u = 1f - t;
                        float tt = t * t;
                        float uu = u * u;
                        float uuu = uu * u;
                        float ttt = tt * t;

                        GL.Vertex3(uuu * p0 +
                                        3 * uu * t * p1 +
                                        3 * u * tt * p2 +
                                            ttt * p3);
                    }

                    if (PathPoints[0].controlPoint1 != Vector3.Zero)
                        GL.Vertex3(p2);
                }
                else
                {
                    GL.Vertex3(positions[posIndex]);

                    GL.Vertex3(positions[0]);
                }
                GL.End();
            }
            
            
        }

        public override void Prepare(GL_ControlModern control)
        {
            pathPointVao = GL.GenVertexArray();
            GL.BindVertexArray(pathPointVao);

            pathPointBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, pathPointBuffer);

            float[] data = new float[PathPoints.Count];

            GL.BufferData(BufferTarget.ArrayBuffer, data.Length, data, BufferUsageHint.DynamicDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 12, 0); //pos

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.UnsignedByte, true, sizeof(float) * 12, sizeof(float) * 3); //col

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, sizeof(float) * 12, sizeof(float) * 4); //controlPoint1

            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.UnsignedByte, true, sizeof(float) * 12, sizeof(float) * 7); //cp1Col

            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, sizeof(float) * 12, sizeof(float) * 8); //controlPoint2

            GL.EnableVertexAttribArray(5);
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.UnsignedByte, true, sizeof(float) * 12, sizeof(float) * 11); //cp2Col

            if (!Initialized)
            {
                var defaultFrag = new FragmentShader(
              @"#version 330
                in vec4 fragColor;
                void main(){
                    gl_FragColor = fragColor;
                }");

                var defaultVert = new VertexShader(
              @"#version 330
                layout(location = 0) in vec3 position;
                layout(location = 1) in vec4 _color;
                layout(location = 2) in vec3 _cp1;
                layout(location = 3) in vec4 _color_cp1;
                layout(location = 4) in vec3 _cp2;
                layout(location = 5) in vec4 _color_cp2;

                out vec4 color;
                out vec4 color_cp1;
                out vec4 color_cp2;
                out vec3 cp1;
                out vec3 cp2;

                void main(){
                    gl_Position = vec4(position,1);
                    cp1 = _cp1;
                    cp2 = _cp2;
                    color = _color;
                    color_cp1 = _color_cp1;
                    color_cp2 = _color_cp2;
                }");

                #region block shader
                defaultShaderProgram = new ShaderProgram(defaultFrag, defaultVert, new GeomertyShader(
              @"#version 330
                layout(points) in;
                layout(triangle_strip, max_vertices = 72) out;
                
                in vec4 color[];
                in vec4 color_cp1[];
                in vec4 color_cp2[];
                in vec3 cp1[];
                in vec3 cp2[];
                out vec4 fragColor;

                uniform mat4 mtxMdl;
                uniform mat4 mtxCam;
                
                float cubeScale = 0.5;
                vec4 pos;

                mat4 mtx = mtxCam*mtxMdl;
                
                vec4 points[8] = vec4[](
                    vec4(-1.0,-1.0,-1.0, 0.0),
                    vec4( 1.0,-1.0,-1.0, 0.0),
                    vec4(-1.0, 1.0,-1.0, 0.0),
                    vec4( 1.0, 1.0,-1.0, 0.0),
                    vec4(-1.0,-1.0, 1.0, 0.0),
                    vec4( 1.0,-1.0, 1.0, 0.0),
                    vec4(-1.0, 1.0, 1.0, 0.0),
                    vec4( 1.0, 1.0, 1.0, 0.0)
                );

                void face(int p1, int p2, int p3, int p4){
                    gl_Position = mtx * (pos + points[p1]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p2]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p3]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p4]*cubeScale); EmitVertex();
                    EndPrimitive();
                }

                void faceInv(int p3, int p4, int p1, int p2){
                    gl_Position = mtx * (pos + points[p1]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p2]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p3]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p4]*cubeScale); EmitVertex();
                    EndPrimitive();
                }
                
                void main(){
                    
                    pos = gl_in[0].gl_Position;
                    fragColor = color[0];
                    faceInv(0,1,2,3);
                    face(4,5,6,7);
                    face(0,1,4,5);
                    faceInv(2,3,6,7);
                    faceInv(0,2,4,6);
                    face(1,3,5,7);
                    cubeScale = 0.25;

                    if(cp1[0]-gl_in[0].gl_Position.xyz!=vec3(0,0,0)){
                        fragColor = color_cp1[0];
                        pos = vec4(cp1[0],1);
                        faceInv(0,1,2,3);
                        face(4,5,6,7);
                        face(0,1,4,5);
                        faceInv(2,3,6,7);
                        faceInv(0,2,4,6);
                        face(1,3,5,7);
                    }

                    if(cp2[0]-gl_in[0].gl_Position.xyz!=vec3(0,0,0)){
                        fragColor = color_cp2[0];
                        pos = vec4(cp2[0],1);
                        faceInv(0,1,2,3);
                        face(4,5,6,7);
                        face(0,1,4,5);
                        faceInv(2,3,6,7);
                        faceInv(0,2,4,6);
                        face(1,3,5,7);
                    }
                }
                "), control);
                #endregion
                

                #region connections shader
                bezierCurveShaderProgram = new ShaderProgram(defaultFrag, defaultVert, new GeomertyShader(
              @"#version 330
                layout(lines) in;
                layout(line_strip, max_vertices = 119) out;
                
                in vec4 color[];
                in vec4 color_cp1[];
                in vec4 color_cp2[];
                in vec3 cp1[];
                in vec3 cp2[];
                in int gl_PrimitiveIDIn[];
                out vec4 fragColor;

                uniform mat4 mtxMdl;
                uniform mat4 mtxCam;
                uniform vec4 lineColor;
                uniform int gapIndex;
                uniform bool isPickingMode;
                
                float cubeScale;
                vec4 pos;

                mat4 mtx = mtxCam*mtxMdl;
                
                vec3 p0 = gl_in[0].gl_Position.xyz;
                vec3 p1 = cp2[0];
                vec3 p2 = cp1[1];
                vec3 p3 = gl_in[1].gl_Position.xyz;
                
                vec4 points[8] = vec4[](
                    vec4(-1.0,-1.0,-1.0, 0.0),
                    vec4( 1.0,-1.0,-1.0, 0.0),
                    vec4(-1.0, 1.0,-1.0, 0.0),
                    vec4( 1.0, 1.0,-1.0, 0.0),
                    vec4(-1.0,-1.0, 1.0, 0.0),
                    vec4( 1.0,-1.0, 1.0, 0.0),
                    vec4(-1.0, 1.0, 1.0, 0.0),
                    vec4( 1.0, 1.0, 1.0, 0.0)
                );

                void face(int p1, int p2, int p4, int p3){
                    gl_Position = mtx * (pos + points[p1]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p2]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p3]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p4]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p1]*cubeScale); EmitVertex();
                    EndPrimitive();
                }

                void line(int p1, int p2){
                    gl_Position = mtx * (pos + points[p1]*cubeScale); EmitVertex();
                    gl_Position = mtx * (pos + points[p2]*cubeScale); EmitVertex();
                    EndPrimitive();
                }

                void getPointAtTime(float t){
                    float u = 1.0 - t;
                    float tt = t * t;
                    float uu = u * u;
                    float uuu = uu * u;
                    float ttt = tt * t;

                    gl_Position = mtx * vec4(uuu    * p0 +
                                    3 * uu * t * p1 +
                                    3 * u  *tt * p2 +
                                        ttt    * p3, 1);
                    EmitVertex();
                }


                void main(){                  
                    if(isPickingMode)
                        fragColor = color[0];
                    else
                        fragColor = vec4(1,1,1,1);
                    
                    cubeScale = 0.5f;
                    pos = vec4(p0,1);
                    
                    face(0,1,2,3);
                    face(4,5,6,7);
                    line(0,4);
                    line(1,5);
                    line(2,6);
                    line(3,7);
                    
                    if(isPickingMode)
                        fragColor = color[1];
                    else
                        fragColor = vec4(1,1,1,1);
                    
                    pos = vec4(p3,1);
                    face(0,1,2,3);
                    face(4,5,6,7);
                    line(0,4);
                    line(1,5);
                    line(2,6);
                    line(3,7);
                    
                    cubeScale = 0.25f;
                    
                    if(p1!=p0){
                        fragColor = color[0];
                        gl_Position = mtx * vec4(p0,1); EmitVertex();
                        gl_Position = mtx * vec4(p1,1); EmitVertex();
                        EndPrimitive();
                        
                        if(isPickingMode)
                            fragColor = color_cp2[0];
                        else
                            fragColor = vec4(1,1,1,1);
                        
                        pos = vec4(p1,1);
                        face(0,1,2,3);
                        face(4,5,6,7);
                        line(0,4);
                        line(1,5);
                        line(2,6);
                        line(3,7);
                    }
                    
                    if(p2!=p3){
                        fragColor = color[1];
                        gl_Position = mtx * vec4(p2,1); EmitVertex();
                        gl_Position = mtx * vec4(p3,1); EmitVertex();
                        EndPrimitive();
                        
                        if(isPickingMode)
                            fragColor = color_cp1[1];
                        else
                            fragColor = vec4(1,1,1,1);
                        
                        pos = vec4(p2,1);
                        face(0,1,2,3);
                        face(4,5,6,7);
                        line(0,4);
                        line(1,5);
                        line(2,6);
                        line(3,7);
                    }
                    if(gl_PrimitiveIDIn[0]!=gapIndex){
                        fragColor = lineColor;
                        if(p1!=p0||p2!=p3){
                            for(float t = 0; t<=1.0; t+=0.0625){
                                getPointAtTime(t);
                            }
                        }else{
                            gl_Position = mtx * vec4(p0, 1);
                            EmitVertex();
                            gl_Position = mtx * vec4(p3, 1);
                            EmitVertex(); 
                        }
                        EndPrimitive();
                    }
                }
                "), control);
                #endregion
                
                lineColorLoc = bezierCurveShaderProgram["lineColor"];
                gapIndexLoc = bezierCurveShaderProgram["gapIndex"];
                isPickingModeLoc = bezierCurveShaderProgram["isPickingMode"];

                Initialized = true;
            }
            else
            {
                defaultShaderProgram.Link(control);
                bezierCurveShaderProgram.Link(control);
            }
        }

        public override void Prepare(GL_ControlLegacy control)
        {
            if (!InitializedLegacy)
            {
                drawLists = GL.GenLists(2);

                GL.NewList(drawLists, ListMode.Compile);
                GL.Begin(PrimitiveType.Quads);
                GL.Vertex3(ColorBlockRenderer.points[7]);
                GL.Vertex3(ColorBlockRenderer.points[6]);
                GL.Vertex3(ColorBlockRenderer.points[2]);
                GL.Vertex3(ColorBlockRenderer.points[3]);

                GL.Vertex3(ColorBlockRenderer.points[4]);
                GL.Vertex3(ColorBlockRenderer.points[5]);
                GL.Vertex3(ColorBlockRenderer.points[1]);
                GL.Vertex3(ColorBlockRenderer.points[0]);
                GL.End();

                GL.Begin(PrimitiveType.QuadStrip);
                GL.Vertex3(ColorBlockRenderer.points[7]);
                GL.Vertex3(ColorBlockRenderer.points[5]);
                GL.Vertex3(ColorBlockRenderer.points[6]);
                GL.Vertex3(ColorBlockRenderer.points[4]);
                GL.Vertex3(ColorBlockRenderer.points[2]);
                GL.Vertex3(ColorBlockRenderer.points[0]);
                GL.Vertex3(ColorBlockRenderer.points[3]);
                GL.Vertex3(ColorBlockRenderer.points[1]);
                GL.Vertex3(ColorBlockRenderer.points[7]);
                GL.Vertex3(ColorBlockRenderer.points[5]);
                GL.End();
                GL.EndList();


                GL.NewList(drawLists + 1, ListMode.Compile);
                GL.Color4(Color.White);
                GL.Begin(PrimitiveType.LineStrip);
                GL.Vertex3(ColorBlockRenderer.points[6]);
                GL.Vertex3(ColorBlockRenderer.points[2]);
                GL.Vertex3(ColorBlockRenderer.points[3]);
                GL.Vertex3(ColorBlockRenderer.points[7]);
                GL.Vertex3(ColorBlockRenderer.points[6]);

                GL.Vertex3(ColorBlockRenderer.points[4]);
                GL.Vertex3(ColorBlockRenderer.points[5]);
                GL.Vertex3(ColorBlockRenderer.points[1]);
                GL.Vertex3(ColorBlockRenderer.points[0]);
                GL.Vertex3(ColorBlockRenderer.points[4]);
                GL.End();

                GL.Begin(PrimitiveType.Lines);
                GL.Vertex3(ColorBlockRenderer.points[2]);
                GL.Vertex3(ColorBlockRenderer.points[0]);
                GL.Vertex3(ColorBlockRenderer.points[3]);
                GL.Vertex3(ColorBlockRenderer.points[1]);
                GL.Vertex3(ColorBlockRenderer.points[7]);
                GL.Vertex3(ColorBlockRenderer.points[5]);
                GL.End();
                GL.EndList();
                InitializedLegacy = true;
            }
        }

        public override int GetPickableSpan()
        {
            int i = 1;
            foreach (PathPoint point in PathPoints)
                i += point.GetPickableSpan();

            return i;
        }

        public override bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively)
        {
            if(hoveredPart == 0)
            {
                BoundingBox box = BoundingBox.Default;
                bool allPointsSelected = true;
                foreach (PathPoint point in PathPoints)
                {
                    if (point.Selected)
                        box.Include(new BoundingBox(
                            point.position.X - 0.5f,
                            point.position.X + 0.5f,
                            point.position.Y - 0.5f,
                            point.position.Y + 0.5f,
                            point.position.Z - 0.5f,
                            point.position.Z + 0.5f
                        ));
                    allPointsSelected &= point.Selected;
                }

                localOrientation = new LocalOrientation(box.GetCenter());
                dragExclusively = false;

                return allPointsSelected;
            }
            else
            {
                hoveredPart--;
                foreach (PathPoint point in PathPoints)
                {
                    int span = point.GetPickableSpan();
                    if (hoveredPart >= 0 && hoveredPart < span)
                    {
                        return point.TryStartDragging(actionType, hoveredPart, out localOrientation, out dragExclusively);
                    }
                    hoveredPart -= span;
                }
            }
            throw new Exception("Invalid partIndex");
        }

        public override bool IsSelected()
        {
            return false;
        }

        public override uint SelectAll(GL_ControlBase control, ISet<object> selectedObjects)
        {
            selectedObjects?.Add(this);
            foreach (PathPoint point in PathPoints)
                point.SelectAll(control, selectedObjects);

            return REDRAW;
        }

        public override uint SelectDefault(GL_ControlBase control, ISet<object> selectedObjects)
        {
            selectedObjects?.Add(this);
            foreach (PathPoint point in PathPoints)
                point.SelectDefault(control, selectedObjects);

            return REDRAW;
        }

        public override bool IsSelected(int partIndex)
        {
            if (partIndex == 0)
            {
                bool allPointsSelected = true;
                foreach (PathPoint point in PathPoints)
                {
                   allPointsSelected &= point.Selected;
                }
                return allPointsSelected;
            }
            else
            {
                partIndex--;
                foreach (PathPoint point in PathPoints)
                {
                    int span = point.GetPickableSpan();
                    if (partIndex >= 0 && partIndex < span)
                    {
                        if (point.IsSelected(partIndex))
                            return true;
                    }
                    partIndex -= span;
                }
                return false;
            }
        }

        public override uint Select(int partIndex, GL_ControlBase control, ISet<object> selectedObjects)
        {
            selectedObjects?.Add(this);
            if (partIndex == 0)
            {
                foreach (PathPoint point in PathPoints)
                    point.SelectDefault(control, selectedObjects);
            }
            else
            {
                partIndex--;
                foreach (PathPoint point in PathPoints)
                {
                    int span = point.GetPickableSpan();
                    if (partIndex >= 0 && partIndex < span)
                    {
                        point.Select(partIndex, control, selectedObjects);
                    }
                    partIndex -= span;
                }
            }

            return REDRAW;
        }

        public override uint Deselect(int partIndex, GL_ControlBase control, ISet<object> selectedObjects)
        {
            selectedObjects?.Remove(this);
            if (partIndex == 0)
            {
                foreach (PathPoint point in PathPoints)
                    point.DeselectAll(control, selectedObjects);
            }
            else
            {
                bool noPointsSelected = true;
                partIndex--;
                foreach (PathPoint point in PathPoints)
                {
                    int span = point.GetPickableSpan();
                    if (partIndex >= 0 && partIndex < span)
                    {
                        point.Deselect(partIndex, control, selectedObjects);
                    }
                    partIndex -= span;
                    noPointsSelected &= !point.Selected;
                }
            }

            return REDRAW;
        }

        public override void SetTransform(Vector3? pos, Quaternion? rot, Vector3? scale, int _part, out Vector3? prevPos, out Quaternion? prevRot, out Vector3? prevScale)
        {
            _part--;
            foreach (PathPoint point in PathPoints)
            {
                int span = point.GetPickableSpan();
                if (_part >= 0 && _part < span)
                {
                    point.SetTransform(pos, rot, scale, _part, out prevPos, out prevRot, out prevScale);
                    return;
                }
                _part -= span;
            }
            throw new Exception("Invalid partIndex");
        }

        public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos transformChangeInfos)
        {
            
        }

        public override void ApplyTransformActionToPart(AbstractTransformAction transformAction, int _part, ref TransformChangeInfos transformChangeInfos)
        {
            _part--;
            foreach (PathPoint point in PathPoints)
            {
                int span = point.GetPickableSpan();
                if (_part >= 0 && _part < span)
                {
                    point.ApplyTransformActionToPart(transformAction, _part, ref transformChangeInfos);
                    return;
                }
                _part -= span;
            }
            throw new Exception("Invalid partIndex");
        }

        public override uint DeselectAll(GL_ControlBase control, ISet<object> selectedObjects)
        {
            selectedObjects?.Remove(this);
            foreach (PathPoint point in PathPoints)
                point.DeselectAll(control, selectedObjects);

            return REDRAW;
        }

        public override void Draw(GL_ControlModern control, Pass pass)
        {
            throw new NotImplementedException();
        }

        public override void Draw(GL_ControlLegacy control, Pass pass)
        {
            throw new NotImplementedException();
        }

        public override void GetSelectionBox(ref BoundingBox boundingBox)
        {
            foreach (PathPoint point in PathPoints)
                point.GetSelectionBox(ref boundingBox);
        }

        public override LocalOrientation GetLocalOrientation(int partIndex)
        {
            if (partIndex == 0)
            {
                BoundingBox box = BoundingBox.Default;
                foreach (PathPoint point in PathPoints)
                {
                    box.Include(point.position);
                }
                return new LocalOrientation(box.GetCenter());
            }
            else
            {
                partIndex--;
                foreach (PathPoint point in PathPoints)
                {
                    int span = point.GetPickableSpan();
                    if (partIndex >= 0 && partIndex < span)
                        return point.GetLocalOrientation(partIndex);
                    partIndex -= span;
                }
                throw new Exception("Invalid partIndex");
            }
        }

        public override bool IsInRange(float range, float rangeSquared, Vector3 pos)
        {
            BoundingBox box;
            for (int i = 1; i<PathPoints.Count; i++)
            {
                box = BoundingBox.Default;
                box.Include(PathPoints[i - 1].position);
                box.Include(PathPoints[i - 1].position + PathPoints[i - 1].controlPoint2);
                box.Include(PathPoints[i].position + PathPoints[i].controlPoint1);
                box.Include(PathPoints[i].position);

                if (pos.X < box.maxX + range && pos.X > box.minX - range &&
                    pos.Y < box.maxY + range && pos.Y > box.minY - range &&
                    pos.Z < box.maxZ + range && pos.Z > box.minZ - range)
                    return true;
            }
            return false;
        }

        public override void DeleteSelected(DeletionManager manager, IList list, IList currentList)
        {
            bool allPointsSelected = true;
            foreach (PathPoint point in PathPoints)
                allPointsSelected &= point.Selected;

            if (allPointsSelected)
            {
                if (currentList == PathPoints)
                {
                    for(int i = 1; i<PathPoints.Count; i++)
                        PathPoints[i].DeleteSelected(manager, PathPoints, currentList);
                }
                else
                    manager.Add(list, this);
            }
            else
            {
                foreach (PathPoint point in PathPoints)
                    point.DeleteSelected(manager, PathPoints, currentList);
            }
        }

        public override bool ProvidesProperty(EditorSceneBase scene)
        {
            if (scene.SelectedObjects.Count > PathPoints.Count+1)
                return false;
            foreach(object obj in scene.SelectedObjects)
            {
                if (!PathPoints.Contains(obj) && obj != this)
                    return false;
            }
            return true;

        }

        public override IPropertyProvider GetPropertyProvider(EditorSceneBase scene)
        {
            return new PropertyProvider(this, scene);
        }

        public class PropertyProvider : IPropertyProvider
        {
            bool prevClosed;
            Vector3 prevPathPointPos;
            Vector3 prevPathPointCP1;
            Vector3 prevPathPointCP2;
            
            Path path;
            PathPoint point;
            readonly EditorSceneBase scene;
            public PropertyProvider(Path path, EditorSceneBase scene)
            {
                this.path = path;

                List<PathPoint> points = new List<PathPoint>();
                
                foreach(IEditableObject obj in scene.SelectedObjects)
                {
                    if ((point = obj as PathPoint) != null)
                        points.Add(point);
                }

                if (points.Count == 1)
                    point = point ?? points.First(); //if the last selected object isn't a PathPoint, use the first selected point
                else
                    point = null;

                this.scene = scene;
            }

            public void DoUI(IObjectPropertyControl control)
            {
                path.Closed = control.CheckBox("Closed", path.Closed);

                if (scene.CurrentList!= path.PathPoints && control.Button("Edit Pathpoints"))
                    scene.SetCurrentList(path.PathPoints);

                if (point != null)
                {
                    if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                        point.position = control.Vector3Input(point.position, "Position", 1, 16);
                    else
                        point.position = control.Vector3Input(point.position, "Position", 0.125f, 2);

                    if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                        point.controlPoint1 = control.Vector3Input(point.controlPoint1, "Control Point 1", 1, 16);
                    else
                        point.controlPoint1 = control.Vector3Input(point.controlPoint1, "Control Point 1", 0.125f, 2);

                    if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                        point.controlPoint2 = control.Vector3Input(point.controlPoint2, "Control Point 2", 1, 16);
                    else
                        point.controlPoint2 = control.Vector3Input(point.controlPoint2, "Control Point 2", 0.125f, 2);
                }
            }

            public void OnValueChangeStart()
            {
                prevClosed = path.Closed;
                prevPathPointPos = point.position;
                prevPathPointCP1 = point.controlPoint1;
                prevPathPointCP2 = point.controlPoint2;
            }

            public void OnValueChanged()
            {
                scene.Refresh();
            }

            public void OnValueSet()
            {
                if (prevClosed != path.Closed)
                    Console.WriteLine("Closed Changed");
                if (prevPathPointPos != point.position)
                    Console.WriteLine("Point Position Changed");
                if (prevPathPointCP1 != point.controlPoint1)
                    Console.WriteLine("Control Point 1 Changed");
                if (prevPathPointCP2 != point.controlPoint2)
                    Console.WriteLine("Control Point 2 Changed");

                scene.Refresh();
            }

            public void UpdateProperties()
            {

            }
        }

        public class PathPoint : EditableObject
        {
            public bool Selected = false;

            public override string ToString() => "PathPoint";

            public PathPoint(Vector3 position, Vector3 controlPoint1, Vector3 controlPoint2)
            {
                this.position = position;
                this.controlPoint1 = controlPoint1;
                this.controlPoint2 = controlPoint2;
            }
            public Vector3 position;
            public Vector3 controlPoint1;
            public Vector3 controlPoint2;

            public override bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively)
            {

                if (hoveredPart == 0)
                {
                    localOrientation = new LocalOrientation(position);
                    dragExclusively = false;
                    return Selected;
                }

                int part = 1;

                if (controlPoint1 != Vector3.Zero)
                {
                    if (hoveredPart == part)
                    {
                        if (actionType == DragActionType.TRANSLATE)
                        {
                            localOrientation = new LocalOrientation(position + controlPoint1);
                            dragExclusively = true; //controlPoints can be moved exclusively
                            return true;
                        }
                        else
                        {
                            localOrientation = new LocalOrientation(position);
                            dragExclusively = false;
                            return Selected;
                        }
                    }
                    part++;
                }

                if (controlPoint2 != Vector3.Zero)
                {
                    if (hoveredPart == part)
                    {
                        if (actionType == DragActionType.TRANSLATE)
                        {
                            localOrientation = new LocalOrientation(position + controlPoint2);
                            dragExclusively = true; //controlPoints can be moved exclusively
                            return true;
                        }
                        else
                        {
                            localOrientation = new LocalOrientation(position);
                            dragExclusively = false;
                            return Selected;
                        }
                    }
                }
                throw new Exception("Invalid partIndex");
            }

            public override bool IsSelected() => Selected;

            public override bool IsSelected(int partIndex) => Selected;

            public override void GetSelectionBox(ref BoundingBox boundingBox)
            {
                if (!Selected)
                    return;

                boundingBox.Include(new BoundingBox(
                    position.X - 0.5f,
                    position.X + 0.5f,
                    position.Y - 0.5f,
                    position.Y + 0.5f,
                    position.Z - 0.5f,
                    position.Z + 0.5f
                ));
            }

            public override LocalOrientation GetLocalOrientation(int partIndex)
            {
                return new LocalOrientation(position);
            }

            public override bool IsInRange(float range, float rangeSquared, Vector3 pos) => true; //probably never gets called

            public override uint SelectAll(GL_ControlBase control, ISet<object> selectedObjects)
            {
                Selected = true;
                selectedObjects?.Add(this);
                return REDRAW;
            }

            public override uint SelectDefault(GL_ControlBase control, ISet<object> selectedObjects)
            {
                Selected = true;
                selectedObjects?.Add(this);
                return REDRAW;
            }

            public override uint Select(int partIndex, GL_ControlBase control, ISet<object> selectedObjects)
            {
                if (partIndex == 0)
                {
                    Selected = true;
                    selectedObjects?.Add(this);
                }
                return REDRAW;
            }

            public override uint Deselect(int partIndex, GL_ControlBase control, ISet<object> selectedObjects)
            {
                if (partIndex == 0)
                {
                    Selected = false;
                    selectedObjects?.Remove(this);
                }
                return REDRAW;
            }

            public override uint DeselectAll(GL_ControlBase control, ISet<object> selectedObjects)
            {
                Selected = false;
                selectedObjects?.Remove(this);
                return REDRAW;
            }

            public override void SetTransform(Vector3? pos, Quaternion? rot, Vector3? scale, int _part, out Vector3? prevPos, out Quaternion? prevRot, out Vector3? prevScale)
            {
                prevPos = null;
                prevRot = null;
                prevScale = null;

                if (!pos.HasValue)
                    return;

                if (_part == 0)
                {
                    prevPos = position;
                    position = pos.Value;
                    return;
                }

                int part = 1;

                if (controlPoint1 != Vector3.Zero)
                {
                    if (_part == part)
                    {
                        prevPos = controlPoint1;
                        controlPoint1 = pos.Value;
                        return;
                    }
                    part++;
                }

                if (controlPoint2 != Vector3.Zero)
                {
                    if (_part == part)
                    {
                        prevPos = controlPoint2;
                        controlPoint2 = pos.Value;
                        return;
                    }
                    part++;
                }
            }
            
            public override void Prepare(GL_ControlModern control)
            {
                //probably never gets called
            }

            public override void Prepare(GL_ControlLegacy control)
            {
                //probably never gets called
            }

            public override void Draw(GL_ControlModern control, Pass pass)
            {
                //probably never gets called
            }

            public override void Draw(GL_ControlLegacy control, Pass pass)
            {
                //probably never gets called
            }

            public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction, ref TransformChangeInfos transformChangeInfos)
            {
                if (Selected)
                {
                    int part = 1;

                    Vector3? prevPos;

                    if (controlPoint1 != Vector3.Zero)
                    {
                        controlPoint1 = transformAction.NewIndividualPos(controlPoint1, out prevPos);
                        if (prevPos.HasValue)
                            transformChangeInfos.Add(this, part, prevPos, null, null);
                        part++;
                    }

                    if (controlPoint2 != Vector3.Zero)
                    {
                        controlPoint2 = transformAction.NewIndividualPos(controlPoint2, out prevPos);
                        if (prevPos.HasValue)
                            transformChangeInfos.Add(this, part, prevPos, null, null);
                    }

                    position = transformAction.NewPos(position, out prevPos);
                    if (prevPos.HasValue)
                        transformChangeInfos.Add(this, 0, prevPos, null, null);
                }
            }

            public override void ApplyTransformActionToPart(AbstractTransformAction transformAction, int _part, ref TransformChangeInfos transformChangeInfos)
            {
                int part = 1;
                if (controlPoint1 != Vector3.Zero)
                {
                    if (part == _part)
                    {
                        controlPoint1 = transformAction.NewPos(position + controlPoint1, out Vector3? prevPos) - position;
                        if (prevPos.HasValue)
                            transformChangeInfos.Add(this, part, prevPos - position, null, null);

                        return;
                    }
                    part++;
                }

                if (controlPoint2 != Vector3.Zero)
                {
                    if (part == _part)
                    {
                        controlPoint2 = transformAction.NewPos(position + controlPoint2, out Vector3? prevPos) - position;
                        if (prevPos.HasValue)
                            transformChangeInfos.Add(this, part, prevPos - position, null, null);
                        return;
                    }
                }
            }

            public override int GetPickableSpan()
            {
                int i = 1;
                if (controlPoint1 != Vector3.Zero)
                    i++;
                if (controlPoint2 != Vector3.Zero)
                    i++;

                return i;
            }

            public override void DeleteSelected(DeletionManager manager, IList list, IList currentList)
            {
                if (Selected)
                    manager.Add(list, this);
            }
        }
    }
}

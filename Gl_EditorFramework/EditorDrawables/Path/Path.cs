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
using System.IO;

namespace GL_EditorFramework.EditorDrawables
{
    public partial class Path : EditableObject
    {
        public static float CubeScale => 0.5f;
        public static float ControlCubeScale => 0.25f;

        private static bool Initialized = false;
        private static bool InitializedLegacy = false;
        private static ShaderProgram triangleShaderProgram;
        private static ShaderProgram lineShaderProgram;
        private static int colorLoc_Line, gapIndexLoc_Line, isPickingModeLoc_Line, 
            isPickingModeLoc_Tri, colorLoc_Tri;

        private static int drawLists;

        private int pathPointVao;
        private int pathPointBuffer;
        readonly protected List<PathPoint> pathPoints;

        public IReadOnlyList<PathPoint> PathPoints => pathPoints;

        [PropertyCapture.Undoable]
        public bool Closed { get; set; } = false;

        public Path(List<PathPoint> pathPoints)
        {
            this.pathPoints = pathPoints;
            foreach (PathPoint point in pathPoints)
                point.Path = this;
        }

        public override void ListChanged(IList list)
        {
            if (list == pathPoints)
            {
                foreach (PathPoint point in pathPoints)
                    point.Path = this;
            }
        }

        public override string ToString() => "path";

        public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!ObjectRenderState.ShouldBeDrawn(this))
                return;
                

            GL.BindVertexArray(pathPointVao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, pathPointBuffer);

            Vector4 color;

            float[] data = new float[pathPoints.Count * 12]; //px, py, pz, pCol, cp1x, cp1y, cp1z, cp1Col,  cp2x, cp2y, cp2z, cp2Col

            int bufferIndex = 0;

            bool hovered = editorScene.Hovered == this;

            Vector4 col;
            Vector3 pos;
            if (pass == Pass.OPAQUE)
            {
                GL.LineWidth(1f);

                int randomColor = control.RNG.Next();
                color = new Vector4(
                    (((randomColor >> 16) & 0xFF) / 255f) * 0.5f + 0.25f,
                    (((randomColor >> 8)  & 0xFF) / 255f) * 0.5f + 0.25f,
                    ((randomColor         & 0xFF) / 255f) * 0.5f + 0.25f,
                    1f
                    );

                #region generate buffer
                int part = 1;
                for (int i = 0; i < pathPoints.Count; i++)
                {
                    PathPoint point = pathPoints[i];
                    #region Point
                    //determine position
                    if (editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(point.GlobalPos);

                    else if (point.Selected)
                        pos = editorScene.CurrentAction.NewPos(point.GlobalPos);
                    else
                        pos = point.GlobalPos;

                    Vector3 pointPos = pos;

                    //determine color
                    bool _hovered = hovered && (editorScene.HoveredPart == part || editorScene.HoveredPart == 0);

                    if (point.Selected && _hovered)
                        col = hoverSelectColor;
                    else if (point.Selected)
                        col = selectColor;
                    else if (_hovered)
                        col = hoverColor;
                    else
                        col = color;

                    //write data
                    data[bufferIndex]     = pos.X;
                    data[bufferIndex + 1] = pos.Y;
                    data[bufferIndex + 2] = pos.Z;
                    data[bufferIndex + 3] =  BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 255),
                    (byte)(col.Y * 255),
                    (byte)(col.Z * 255),
                    (byte)(col.W * 255)}, 0);

                    part++;
                    #endregion

                    #region ControlPoint1
                    //determine position
                    if (point.GlobalCP1 != Vector3.Zero && editorScene.ExclusiveAction!=NoAction && 
                        hovered && editorScene.HoveredPart==part)

                        pos = editorScene.ExclusiveAction.NewPos(pointPos + point.GlobalCP1);

                    else if (point.Selected)
                        pos = pointPos + editorScene.CurrentAction.NewIndividualPos(point.GlobalCP1);
                    else
                        pos = pointPos + point.GlobalCP1;

                    //determine color
                    _hovered = hovered && (editorScene.HoveredPart == part || editorScene.HoveredPart == 0);

                    if (point.Selected && _hovered)
                        col = hoverSelectColor;
                    else if (point.Selected)
                        col = selectColor;
                    else if (_hovered)
                        col = hoverColor;
                    else
                        col = color;

                    //write data
                    data[bufferIndex + 4] =  pos.X;
                    data[bufferIndex + 5] =  pos.Y;
                    data[bufferIndex + 6] =  pos.Z;
                    data[bufferIndex + 7] =  BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 255),
                    (byte)(col.Y * 255),
                    (byte)(col.Z * 255),
                    (byte)(col.W * 255)}, 0);

                    part++;
                    #endregion

                    #region ControlPoint2
                    //determine position
                    if (point.GlobalCP2 != Vector3.Zero && editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(pointPos + point.GlobalCP2);

                    else if (point.Selected)
                        pos = pointPos + editorScene.CurrentAction.NewIndividualPos(point.GlobalCP2);
                    else
                        pos = pointPos + point.GlobalCP2;

                    //determine color
                    _hovered = hovered && (editorScene.HoveredPart == part || editorScene.HoveredPart == 0);

                    if (point.Selected && _hovered)
                        col = hoverSelectColor;
                    else if (point.Selected)
                        col = selectColor;
                    else if (_hovered)
                        col = hoverColor;
                    else
                        col = color;

                    //write data
                    data[bufferIndex + 8] =  pos.X;
                    data[bufferIndex + 9] =  pos.Y;
                    data[bufferIndex + 10] = pos.Z;
                    data[bufferIndex + 11] = BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 255),
                    (byte)(col.Y * 255),
                    (byte)(col.Z * 255),
                    (byte)(col.W * 255)}, 0);

                    part++;
                    #endregion;

                    bufferIndex += 12;
                }
                GL.BufferData(BufferTarget.ArrayBuffer, data.Length * 4, data, BufferUsageHint.DynamicDraw);
                #endregion
            }
            else
            {
                GL.LineWidth(8f);

                color = control.NextPickingColor();

                #region generate buffer
                int part = 1;
                for (int i = 0; i < pathPoints.Count; i++)
                {
                    PathPoint point = pathPoints[i];
                    #region Point
                    //determine position
                    if (editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(point.GlobalPos);

                    else if (point.Selected)
                        pos = editorScene.CurrentAction.NewPos(point.GlobalPos);
                    else
                        pos = point.GlobalPos;

                    Vector3 pointPos = pos;

                    //determine color
                    col = control.NextPickingColor();

                    //write data
                    data[bufferIndex]     = pos.X;
                    data[bufferIndex + 1] = pos.Y;
                    data[bufferIndex + 2] = pos.Z;
                    data[bufferIndex + 3] = BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 255),
                    (byte)(col.Y * 255),
                    (byte)(col.Z * 255),
                    (byte)(col.W * 255)}, 0);

                    part++;
                    #endregion

                    #region ControlPoint1
                    //determine position
                    if (point.GlobalCP1 != Vector3.Zero && editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(pointPos + point.GlobalCP1);

                    else if (point.Selected)
                        pos = pointPos + editorScene.CurrentAction.NewIndividualPos(point.GlobalCP1);
                    else
                        pos = pointPos + point.GlobalCP1;

                    //determine color
                    col = control.NextPickingColor();

                    //write data
                    data[bufferIndex + 4] = pos.X;
                    data[bufferIndex + 5] = pos.Y;
                    data[bufferIndex + 6] = pos.Z;
                    data[bufferIndex + 7] = BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 255),
                    (byte)(col.Y * 255),
                    (byte)(col.Z * 255),
                    (byte)(col.W * 255)}, 0);

                    part++;
                    #endregion

                    #region ControlPoint2
                    //determine position
                    if (point.GlobalCP2 != Vector3.Zero && editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(pointPos + point.GlobalCP2);

                    else if (point.Selected)
                        pos = pointPos + editorScene.CurrentAction.NewIndividualPos(point.GlobalCP2);
                    else
                        pos = pointPos + point.GlobalCP2;

                    //determine color
                    col = control.NextPickingColor();

                    //write data
                    data[bufferIndex + 8]  = pos.X;
                    data[bufferIndex + 9]  = pos.Y;
                    data[bufferIndex + 10] = pos.Z;
                    data[bufferIndex + 11] = BitConverter.ToSingle(new byte[]{
                    (byte)(col.X * 255),
                    (byte)(col.Y * 255),
                    (byte)(col.Z * 255),
                    (byte)(col.W * 255)}, 0);

                    part++;
                    #endregion

                    bufferIndex += 12;
                }
                GL.BufferData(BufferTarget.ArrayBuffer, data.Length * 4, data, BufferUsageHint.DynamicDraw);
                #endregion
            }

            GL.BindVertexArray(pathPointVao);

            //draw triangles
            control.CurrentShader = triangleShaderProgram;
            control.ResetModelMatrix();
            GL.Uniform4(colorLoc_Tri, color);
            GL.Uniform1(isPickingModeLoc_Tri, (pass == Pass.PICKING) ? 1 : 0);
            triangleShaderProgram.SetFloat("cubeScale", CubeScale);
            triangleShaderProgram.SetFloat("controlCubeScale", ControlCubeScale);
            
            GL.DrawArrays(PrimitiveType.Points, 0, pathPoints.Count);

            //draw lines
            control.CurrentShader = lineShaderProgram;
            GL.Uniform4(colorLoc_Line, color);
            GL.Uniform1(gapIndexLoc_Line, Closed ? -1 : pathPoints.Count - 1);
            GL.Uniform1(isPickingModeLoc_Line, (pass == Pass.PICKING) ? 1 : 0);
            lineShaderProgram.SetFloat("cubeScale", CubeScale);
            lineShaderProgram.SetFloat("controlCubeScale", ControlCubeScale);

            GL.DrawArrays(PrimitiveType.LineLoop, 0, pathPoints.Count);

            GL.LineWidth(2f);
        }

        public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
        {
            if (pass == Pass.TRANSPARENT)
                return;

            if (!ObjectRenderState.ShouldBeDrawn(this))
                return;

            GL.Disable(EnableCap.Texture2D);

            Vector3[] connectLinePositions = new Vector3[pathPoints.Count*3];

            Vector4[] connectLineColors = new Vector4[pathPoints.Count];

            Vector4 color;

            int posIndex = 0;

            bool hovered = editorScene.Hovered == this;

            Vector4 col;
            Vector3 pos;
            if (pass == Pass.OPAQUE)
            {
                GL.LineWidth(1f);

                int randomColor = control.RNG.Next();
                color = new Vector4(
                    ((randomColor >> 16) & 0xFF) / 255f * 0.5f + 0.25f,
                    ((randomColor >> 8) & 0xFF)  / 255f * 0.5f + 0.25f,
                    (randomColor        & 0xFF)  / 255f * 0.5f + 0.25f,
                    1f
                    );

                #region generate buffer
                int part = 1;
                for (int i = 0; i < pathPoints.Count; i++)
                {
                    PathPoint point = pathPoints[i];
                    #region Point
                    //determine position
                    if (editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(point.GlobalPos);

                    else if (point.Selected)
                        pos = editorScene.CurrentAction.NewPos(point.GlobalPos);
                    else
                        pos = point.GlobalPos;

                    Vector3 pointPos = pos;

                    //determine color
                    bool _hovered = hovered && (editorScene.HoveredPart == part || editorScene.HoveredPart == 0);

                    if (point.Selected && _hovered)
                        col = hoverSelectColor;
                    else if (point.Selected)
                        col = selectColor;
                    else if (_hovered)
                        col = hoverColor;
                    else
                        col = color;

                    //write data
                    connectLinePositions[posIndex] = pos;
                    connectLineColors[i] = col;

                    //draw point
                    control.UpdateModelMatrix(Matrix4.CreateScale(CubeScale) * Matrix4.CreateTranslation(pos));
                    GL.Color4(color * .125f + col * .125f);
                    GL.CallList(drawLists);
                    GL.Color4(col);
                    GL.CallList(drawLists+1);

                    part++;
                    #endregion

                    #region ControlPoint1
                    //determine position
                    if (point.GlobalCP1 != Vector3.Zero && editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(pointPos + point.GlobalCP1);

                    else if (point.Selected)
                        pos = pointPos + editorScene.CurrentAction.NewIndividualPos(point.GlobalCP1);
                    else
                        pos = pointPos + point.GlobalCP1;

                    //determine color
                    _hovered = hovered && (editorScene.HoveredPart == part || editorScene.HoveredPart == 0);

                    if (point.Selected && _hovered)
                        col = hoverSelectColor;
                    else if (point.Selected)
                        col = selectColor;
                    else if (_hovered)
                        col = hoverColor;
                    else
                        col = color;

                    //write data
                    connectLinePositions[posIndex+1] = pos;

                    //draw point
                    control.UpdateModelMatrix(Matrix4.CreateScale(ControlCubeScale) * Matrix4.CreateTranslation(pos));
                    GL.Color4(color * .125f + col * .125f);
                    GL.CallList(drawLists);
                    GL.Color4(col);
                    GL.CallList(drawLists + 1);

                    part++;
                    #endregion

                    #region ControlPoint2
                    //determine position
                    if (point.GlobalCP2 != Vector3.Zero && editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(pointPos + point.GlobalCP2);

                    else if (point.Selected)
                        pos = pointPos + editorScene.CurrentAction.NewIndividualPos(point.GlobalCP2);
                    else
                        pos = pointPos + point.GlobalCP2;

                    //determine color
                    _hovered = hovered && (editorScene.HoveredPart == part || editorScene.HoveredPart == 0);

                    if (point.Selected && _hovered)
                        col = hoverSelectColor;
                    else if (point.Selected)
                        col = selectColor;
                    else if (_hovered)
                        col = hoverColor;
                    else
                        col = color;

                    //write data
                    connectLinePositions[posIndex+2] = pos;

                    //draw point
                    control.UpdateModelMatrix(Matrix4.CreateScale(ControlCubeScale) * Matrix4.CreateTranslation(pos));
                    GL.Color4(color * .125f + col * .125f);
                    GL.CallList(drawLists);
                    GL.Color4(col);
                    GL.CallList(drawLists + 1);

                    part++;
                    #endregion;

                    posIndex += 3;
                }
                #endregion
            }
            else
            {
                GL.LineWidth(16f);

                color = control.NextPickingColor();

                #region generate buffer
                int part = 1;
                for (int i = 0; i < pathPoints.Count; i++)
                {
                    PathPoint point = pathPoints[i];
                    #region Point
                    //determine position
                    if (editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(point.GlobalPos);

                    else if (point.Selected)
                        pos = editorScene.CurrentAction.NewPos(point.GlobalPos);
                    else
                        pos = point.GlobalPos;

                    Vector3 pointPos = pos;

                    //determine color
                    col = control.NextPickingColor();

                    //write data
                    connectLinePositions[posIndex] = pos;
                    connectLineColors[i] = color; //colors need to be the same for proper picking

                    //draw point
                    control.UpdateModelMatrix(Matrix4.CreateScale(CubeScale) * Matrix4.CreateTranslation(pos));
                    GL.Color4(col);
                    GL.CallList(drawLists);

                    part++;
                    #endregion

                    #region ControlPoint1
                    //determine position
                    if (point.GlobalCP1 != Vector3.Zero && editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(pointPos + point.GlobalCP1);

                    else if (point.Selected)
                        pos = pointPos + editorScene.CurrentAction.NewIndividualPos(point.GlobalCP1);
                    else
                        pos = pointPos + point.GlobalCP1;

                    //determine color
                    col = control.NextPickingColor();

                    //write data
                    connectLinePositions[posIndex+1] = pos;

                    //draw point
                    control.UpdateModelMatrix(Matrix4.CreateScale(ControlCubeScale) * Matrix4.CreateTranslation(pos));
                    GL.Color4(col);
                    GL.CallList(drawLists);

                    part++;
                    #endregion

                    #region ControlPoint2
                    //determine position
                    if (point.GlobalCP2 != Vector3.Zero && editorScene.ExclusiveAction != NoAction &&
                        hovered && editorScene.HoveredPart == part)

                        pos = editorScene.ExclusiveAction.NewPos(pointPos + point.GlobalCP2);

                    else if (point.Selected)
                        pos = pointPos + editorScene.CurrentAction.NewIndividualPos(point.GlobalCP2);
                    else
                        pos = pointPos + point.GlobalCP2;

                    //determine color
                    col = control.NextPickingColor();

                    //draw point
                    control.UpdateModelMatrix(Matrix4.CreateScale(ControlCubeScale) * Matrix4.CreateTranslation(pos));
                    GL.Color4(col);
                    GL.CallList(drawLists);

                    //write data
                    connectLinePositions[posIndex+2] = pos;

                    part++;
                    #endregion

                    posIndex += 3;
                }
                #endregion
            }

            #region draw connection line(s)
            control.ResetModelMatrix();

            if (pass != Pass.PICKING)
            {
                //draw control handles for first point
                GL.Color4(color * 0.5f);

                pos = connectLinePositions[0];

                if (pathPoints[0].ControlPoint1 != Vector3.Zero)
                {
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex3(pos);
                    GL.Vertex3(connectLinePositions[1]);
                    GL.End();
                }

                if (pathPoints[0].ControlPoint2 != Vector3.Zero)
                {
                    GL.Begin(PrimitiveType.Lines);
                    GL.Vertex3(pos);
                    GL.Vertex3(connectLinePositions[2]);
                    GL.End();
                }
            }

            posIndex = 0;
            for(int i = 1; i<pathPoints.Count; i++)
            {
                if (pass != Pass.PICKING)
                {
                    //draw control handles for point
                    GL.Color4(color * 0.5f);

                    pos = connectLinePositions[posIndex + 3];

                    if (pathPoints[i].ControlPoint1 != Vector3.Zero)
                    {
                        GL.Begin(PrimitiveType.Lines);
                        GL.Vertex3(pos);
                        GL.Vertex3(connectLinePositions[posIndex + 4]);
                        GL.End();
                    }

                    if (pathPoints[i].ControlPoint2 != Vector3.Zero)
                    {
                        GL.Begin(PrimitiveType.Lines);
                        GL.Vertex3(pos);
                        GL.Vertex3(connectLinePositions[posIndex + 5]);
                        GL.End();
                    }
                }

                GL.Begin(PrimitiveType.LineStrip);
                if (pathPoints[i-1].ControlPoint2 != Vector3.Zero || pathPoints[i].ControlPoint1 != Vector3.Zero) //bezierCurve
                {
                    Vector3 p0 = connectLinePositions[posIndex];
                    Vector3 p1 = connectLinePositions[posIndex+2];
                    Vector3 p2 = connectLinePositions[posIndex+4];
                    Vector3 p3 = connectLinePositions[posIndex+3];

                    for (float t = 0f; t<=1.0; t += 0.0625f)
                    {
                        float u = 1f - t;
                        float tt = t * t;
                        float uu = u * u;
                        float uuu = uu * u;
                        float ttt = tt * t;

                        GL.Color4(Vector4.Lerp(connectLineColors[i-1], connectLineColors[i], t));
                        GL.Vertex3(uuu * p0 +
                                        3 * uu * t * p1 +
                                        3 * u * tt * p2 +
                                            ttt * p3);
                    }
                }
                else
                {
                    GL.Color4(connectLineColors[i - 1]);
                    GL.Vertex3(connectLinePositions[posIndex]);
                    GL.Color4(connectLineColors[i]);
                    GL.Vertex3(connectLinePositions[posIndex+3]);
                }
                GL.End();
                posIndex += 3;
            }

            if (Closed)
            {
                GL.Begin(PrimitiveType.LineStrip);
                if (pathPoints[pathPoints.Count - 1].ControlPoint2 != Vector3.Zero || pathPoints[0].ControlPoint1 != Vector3.Zero) //bezierCurve
                {
                    Vector3 p0 = connectLinePositions[posIndex];
                    Vector3 p1 = connectLinePositions[posIndex + 2];
                    Vector3 p2 = connectLinePositions[1];
                    Vector3 p3 = connectLinePositions[0];

                    for (float t = 0f; t <= 1.0; t += 0.0625f)
                    {
                        float u = 1f - t;
                        float tt = t * t;
                        float uu = u * u;
                        float uuu = uu * u;
                        float ttt = tt * t;

                        GL.Color4(Vector4.Lerp(connectLineColors[pathPoints.Count - 1], connectLineColors[0], t));
                        GL.Vertex3(uuu * p0 +
                                        3 * uu * t * p1 +
                                        3 * u * tt * p2 +
                                            ttt * p3);
                    }
                }
                else
                {
                    GL.Vertex3(connectLinePositions[posIndex]);

                    GL.Vertex3(connectLinePositions[0]);
                }
                GL.End();
            }
            #endregion

            GL.LineWidth(2f);
        }

        public override void Prepare(GL_ControlModern control)
        {
            pathPointVao = GL.GenVertexArray();
            GL.BindVertexArray(pathPointVao);

            pathPointBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, pathPointBuffer);

            float[] data = new float[pathPoints.Count];

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
                    if(gl_FragColor==vec4(0,0,0,0))
                        discard;
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

                triangleShaderProgram = new ShaderProgram(defaultFrag, defaultVert, 
                    new GeomertyShader(File.ReadAllText(
                        System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\Shaders\\TriangleShader.geom"
                        )), control);
                

                lineShaderProgram = new ShaderProgram(defaultFrag, defaultVert,
                    new GeomertyShader(File.ReadAllText(System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\Shaders\\LineShader.geom")), control);
                
                colorLoc_Line = lineShaderProgram["pathColor"];
                gapIndexLoc_Line = lineShaderProgram["gapIndex"];
                isPickingModeLoc_Line = lineShaderProgram["isPickingMode"];

                colorLoc_Tri = triangleShaderProgram["pathColor"];
                isPickingModeLoc_Tri = triangleShaderProgram["isPickingMode"];

                Initialized = true;
            }
            else
            {
                triangleShaderProgram.Link(control);
                lineShaderProgram.Link(control);
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
            if (!ObjectRenderState.ShouldBeDrawn(this))
                return 0;
            int i = 1;
            foreach (PathPoint point in pathPoints)
                i += point.GetPickableSpan();

            return i;
        }

        public override void StartDragging(DragActionType actionType, int hoveredPart, EditorSceneBase scene)
        {
            if(hoveredPart == 0)
            {
                if (actionType == DragActionType.SCALE_INDIVIDUAL)
                    return;
                else if(actionType == DragActionType.TRANSLATE && WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl))
                {
                    Vector3 placePos = -scene.GL_Control.GetPointUnderMouse();

                    float closest_dist = float.MaxValue;

                    Vector3 closest_pos = new Vector3();

                    int closest_i = -1;

                    for (int i = 1; i < pathPoints.Count; i++)
                    {

                        Vector3 p0 = pathPoints[i-1].GlobalPos;
                        Vector3 p1 = pathPoints[i-1].GlobalPos + pathPoints[i - 1].GlobalCP2;
                        Vector3 p2 = pathPoints[i].GlobalPos + pathPoints[i].GlobalCP1;
                        Vector3 p3 = pathPoints[i].GlobalPos;

                        for (float t = 0f; t <= 1.0; t += 0.0625f)
                        {
                            float u = 1f - t;
                            float tt = t * t;
                            float uu = u * u;
                            float uuu = uu * u;
                            float ttt = tt * t;

                            Vector3 pos = uuu * p0 +
                                           3 * uu * t * p1 +
                                           3 * u * tt * p2 +
                                               ttt * p3;

                            float dist = (pos - placePos).LengthSquared;

                            if (dist < closest_dist)
                            {
                                closest_dist = dist;
                                closest_pos = pos;
                                closest_i = i;
                            }
                        }
                    }

                    if(Closed)
                    {
                        Vector3 p0 = pathPoints[pathPoints.Count-1].GlobalPos;
                        Vector3 p1 = pathPoints[pathPoints.Count - 1].GlobalPos + pathPoints[pathPoints.Count - 1].GlobalCP2;
                        Vector3 p2 = pathPoints[0].GlobalPos + pathPoints[0].GlobalCP1;
                        Vector3 p3 = pathPoints[0].GlobalPos;

                        for (float t = 0f; t <= 1.0; t += 0.0625f)
                        {
                            float u = 1f - t;
                            float tt = t * t;
                            float uu = u * u;
                            float uuu = uu * u;
                            float ttt = tt * t;

                            Vector3 pos = uuu * p0 +
                                           3 * uu * t * p1 +
                                           3 * u * tt * p2 +
                                               ttt * p3;

                            float dist = (pos - placePos).LengthSquared;

                            if (dist < closest_dist)
                            {
                                closest_dist = dist;
                                closest_pos = pos;
                                closest_i = pathPoints.Count;
                            }
                        }
                    }

                    PathPoint pathPoint = new PathPoint(closest_pos, Vector3.Zero, Vector3.Zero);

                    pathPoints.Insert(closest_i, pathPoint);

                    scene.StartTransformAction(new TranslateAction(
                        scene.GL_Control,
                        scene.GL_Control.GetMousePos(),
                        closest_pos,
                        scene.GL_Control.PickingDepth
                        ),
                        1 + 3 * closest_i, new RevertableSingleAddition(pathPoint, pathPoints));



                    return;
                }

                BoundingBox box = BoundingBox.Default;
                bool allPointsSelected = true;
                foreach (PathPoint point in pathPoints)
                {
                    if (point.Selected)
                        box.Include(new BoundingBox(
                            point.GlobalPos.X - CubeScale,
                            point.GlobalPos.X + CubeScale,
                            point.GlobalPos.Y - CubeScale,
                            point.GlobalPos.Y + CubeScale,
                            point.GlobalPos.Z - CubeScale,
                            point.GlobalPos.Z + CubeScale
                        ));
                    allPointsSelected &= point.Selected;
                }

                if(allPointsSelected)
                    scene.StartTransformAction(new LocalOrientation(), actionType);
            }
            else
            {
                hoveredPart--;

                if (!Closed && actionType == DragActionType.TRANSLATE && WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftCtrl))
                {
                    int i;
                    Vector3 pos;
                    if (hoveredPart / 3 == 0)
                    {
                        i = 0;
                        pos = pathPoints[0].GlobalPos;
                    }
                    else if (hoveredPart / 3 == pathPoints.Count - 1)
                    {
                        i = pathPoints.Count;
                        pos = pathPoints[pathPoints.Count - 1].GlobalPos;
                    }
                    else
                        return;

                    PathPoint pathPoint = new PathPoint(pos, Vector3.Zero, Vector3.Zero);

                    pathPoints.Insert(i, pathPoint);

                    scene.StartTransformAction(new TranslateAction(
                        scene.GL_Control,
                        scene.GL_Control.GetMousePos(),
                        pos,
                        scene.GL_Control.PickingDepth
                        ),
                        1 + 3 * i, new RevertableSingleAddition(pathPoint, pathPoints));

                    return;
                }

                foreach (PathPoint point in pathPoints)
                {
                    int span = point.GetPickableSpan();
                    if (hoveredPart >= 0 && hoveredPart < span)
                    {
                        point.StartDragging(actionType, hoveredPart, scene);
                    }
                    hoveredPart -= span;
                }
            }
        }

        public override uint SelectAll(GL_ControlBase control)
        {
            
            foreach (PathPoint point in pathPoints)
                point.SelectAll(control);

            return REDRAW;
        }

        public override uint SelectDefault(GL_ControlBase control)
        {
            
            foreach (PathPoint point in pathPoints)
                point.SelectDefault(control);

            return REDRAW;
        }

        public override bool IsSelected(int partIndex)
        {
            if (partIndex == 0)
            {
                bool allPointsSelected = true;
                foreach (PathPoint point in pathPoints)
                {
                   allPointsSelected &= point.Selected;
                }
                return allPointsSelected;
            }
            else
            {
                partIndex--;
                foreach (PathPoint point in pathPoints)
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

        public override uint Select(int partIndex, GL_ControlBase control)
        {
            
            if (partIndex == 0)
            {
                foreach (PathPoint point in pathPoints)
                    point.SelectDefault(control);
            }
            else
            {
                partIndex--;
                foreach (PathPoint point in pathPoints)
                {
                    int span = point.GetPickableSpan();
                    if (partIndex >= 0 && partIndex < span)
                    {
                        point.Select(partIndex, control);
                    }
                    partIndex -= span;
                }
            }

            return REDRAW;
        }

        public override uint Deselect(int partIndex, GL_ControlBase control)
        {
            
            if (partIndex == 0)
            {
                foreach (PathPoint point in pathPoints)
                    point.DeselectAll(control);
            }
            else
            {
                bool noPointsSelected = true;
                partIndex--;
                foreach (PathPoint point in pathPoints)
                {
                    int span = point.GetPickableSpan();
                    if (partIndex >= 0 && partIndex < span)
                    {
                        point.Deselect(partIndex, control);
                    }
                    partIndex -= span;
                    noPointsSelected &= !point.Selected;
                }
            }

            return REDRAW;
        }

        public override void SetTransform(Vector3? pos, Vector3? rot, Vector3? scale, int _part, out Vector3? prevPos, out Vector3? prevRot, out Vector3? prevScale)
        {
            _part--;
            foreach (PathPoint point in pathPoints)
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
            foreach (PathPoint point in pathPoints)
                point.ApplyTransformActionToSelection(transformAction, ref transformChangeInfos);
        }

        public override void ApplyTransformActionToPart(AbstractTransformAction transformAction, int _part, ref TransformChangeInfos transformChangeInfos)
        {
            _part--;
            foreach (PathPoint point in pathPoints)
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

        public override uint DeselectAll(GL_ControlBase control)
        {
            
            foreach (PathPoint point in pathPoints)
                point.DeselectAll(control);

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
            foreach (PathPoint point in pathPoints)
                point.GetSelectionBox(ref boundingBox);
        }

        public override bool IsInRange(float range, float rangeSquared, Vector3 pos)
        {
            if (pathPoints.Count == 1)
                return (pathPoints[0].Position - pos).LengthSquared < rangeSquared;

            BoundingBox box;
            for (int i = 1; i<pathPoints.Count; i++)
            {
                box = BoundingBox.Default;
                box.Include(pathPoints[i - 1].GlobalPos);
                box.Include(pathPoints[i - 1].GlobalPos + pathPoints[i - 1].GlobalCP2);
                box.Include(pathPoints[i].GlobalPos + pathPoints[i].GlobalCP1);
                box.Include(pathPoints[i].GlobalPos);

                if (pos.X < box.maxX + range && pos.X > box.minX - range &&
                    pos.Y < box.maxY + range && pos.Y > box.minY - range &&
                    pos.Z < box.maxZ + range && pos.Z > box.minZ - range)
                    return true;
            }
            return false;
        }

        public override void DeleteSelected(EditorSceneBase scene, DeletionManager manager, IList list)
        {
            bool allPointsSelected = true;
            foreach (PathPoint point in pathPoints)
                allPointsSelected &= point.Selected;

            if (allPointsSelected)
            {
                scene.InvalidateList(pathPoints);
                manager.Add(list, this);
            }
            else
            {
                foreach (PathPoint point in pathPoints)
                    point.DeleteSelected(scene, manager, pathPoints);
            }
        }

        public override bool TrySetupObjectUIControl(EditorSceneBase scene, ObjectUIControl objectUIControl)
        {
            bool any = false;

            foreach (PathPoint point in pathPoints)
                any |= point.Selected;


            if (!any)
                return false;

            objectUIControl.AddObjectUIContainer(new PathUIContainer(this, scene), "Path");
            
            List<PathPoint> points = new List<PathPoint>();

            foreach (PathPoint point in pathPoints)
            {
                if (point.Selected)
                    points.Add(point);
            }

            if (points.Count == 1)
                objectUIControl.AddObjectUIContainer(new SinglePathPointUIContainer(points[0], scene), "Path Point");

            return true;
        }

        public override bool IsSelectedAll()
        {
            bool all = false;

            foreach(PathPoint point in pathPoints)
                all &= point.Selected;

            return all;
        }

        public override bool IsSelected()
        {
            bool any = false;

            foreach (PathPoint point in pathPoints)
                any |= point.Selected;

            return any;
        }

        public override Vector3 GetFocusPoint()
        {
            return pathPoints[0].GlobalPos;
        }

        public class PathUIContainer : IObjectUIContainer
        {
            PropertyCapture? pathCapture = null;

            Path path;
            readonly EditorSceneBase scene;
            public PathUIContainer(Path path, EditorSceneBase scene)
            {
                this.path = path;

                List<PathPoint> points = new List<PathPoint>();
                
                this.scene = scene;
            }

            public void DoUI(IObjectUIControl control)
            {
                path.Closed = control.CheckBox("Closed", path.Closed);

                if (scene.CurrentList!= path.pathPoints && control.Button("Edit Pathpoints"))
                    scene.EnterList(path.pathPoints);
            }

            public void OnValueChangeStart()
            {
                pathCapture = new PropertyCapture(path);
            }

            public void OnValueChanged()
            {
                scene.Refresh();
            }

            public void OnValueSet()
            {
                pathCapture?.HandleUndo(scene);

                pathCapture = null;

                scene.Refresh();
            }

            public void UpdateProperties()
            {

            }
        }

        public class SinglePathPointUIContainer : IObjectUIContainer
        {
            PropertyCapture? pointCapture = null;

            //Path path;
            PathPoint point;
            readonly EditorSceneBase scene;
            public SinglePathPointUIContainer(PathPoint point, EditorSceneBase scene)
            {
                this.point = point;
                this.scene = scene;
            }

            public void DoUI(IObjectUIControl control)
            {
                if (point != null)
                {
                    if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                        point.Position = control.Vector3Input(point.Position, "Position", 1, 16);
                    else
                        point.Position = control.Vector3Input(point.Position, "Position", 0.125f, 2);

                    if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                        point.ControlPoint1 = control.Vector3Input(point.ControlPoint1, "Control Point 1", 1, 16);
                    else
                        point.ControlPoint1 = control.Vector3Input(point.ControlPoint1, "Control Point 1", 0.125f, 2);

                    if (WinInput.Keyboard.IsKeyDown(WinInput.Key.LeftShift))
                        point.ControlPoint2 = control.Vector3Input(point.ControlPoint2, "Control Point 2", 1, 16);
                    else
                        point.ControlPoint2 = control.Vector3Input(point.ControlPoint2, "Control Point 2", 0.125f, 2);
                }
            }

            public void OnValueChangeStart()
            {
                pointCapture = new PropertyCapture(point);
            }

            public void OnValueChanged()
            {
                scene.Refresh();
            }

            public void OnValueSet()
            {
                pointCapture?.HandleUndo(scene);
                
                pointCapture = null;

                scene.Refresh();
            }

            public void UpdateProperties()
            {

            }
        }
    }
}

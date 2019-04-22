using System;
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
		private List<PathPoint> pathPoints;

		public bool Closed = false;

		private HashSet<int> selectedIndices = new HashSet<int>();

		public new static Vector4 hoverColor = new Vector4(1, 1, 0.925f, 1);
		public new static Vector4 selectColor = new Vector4(1, 1f, 0.5f, 1);

		public Path(List<PathPoint> pathPoints)
		{
            this.pathPoints = pathPoints;
		}

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

            float[] data = new float[pathPoints.Count * 12]; //px, py, pz, pCol, cp1x, cp1y, cp1z, cp1Col,  cp2x, cp2y, cp2z, cp2Col

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
                    ((randomColor >> 16) & 0xFF) / 256f,
                    ((randomColor >> 8) & 0xFF) / 256f,
                    (randomColor & 0xFF) / 256f,
                    1f
                    );

                GL.Uniform4(lineColorLoc, color);
                GL.Uniform1(gapIndexLoc, Closed?-1:pathPoints.Count-1);
                GL.Uniform1(isPickingModeLoc, 0);

                foreach (PathPoint point in pathPoints)
                {
                    if (selectedIndices.Contains(index))
                        pos = editorScene.CurrentAction.NewPos(point.position);
                    else
                        pos = point.position;

                    data[i] =     pos.X;
                    data[i + 1] = pos.Y;
                    data[i + 2] = pos.Z;

                    if (selectedIndices.Contains(index))
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

                    else if (selectedIndices.Contains(index))
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

                    else if (selectedIndices.Contains(index))
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
                GL.Uniform1(gapIndexLoc, Closed ? -1 : pathPoints.Count - 1);
                GL.Uniform1(isPickingModeLoc, 1);

                foreach (PathPoint point in pathPoints)
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

            GL.DrawArrays(PrimitiveType.LineLoop, 0, pathPoints.Count);

            control.CurrentShader = defaultShaderProgram;
            GL.DrawArrays(PrimitiveType.Points, 0, pathPoints.Count);
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

            Vector3[] positions = new Vector3[pathPoints.Count*3];

            int posIndex = 0;
            if (pass == Pass.OPAQUE)
            {
                int index = 0;
                foreach (PathPoint point in pathPoints)
                {
                    Vector3 pos;
                    if (selectedIndices.Contains(index))
                        positions[posIndex] = pos = editorScene.CurrentAction.NewPos(point.position);
                    else
                        positions[posIndex] = pos = point.position;

                    control.UpdateModelMatrix(Matrix4.CreateScale(0.5f) * Matrix4.CreateTranslation(pos));

                    GL.CallList(drawLists + 1); //white lines

                    if (selectedIndices.Contains(index))
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
                        else if (selectedIndices.Contains(index))
                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex + 1] = pos + rotation * (scale * point.controlPoint1)));
                        else
                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex + 1] = pos + point.controlPoint1));

                        GL.CallList(drawLists + 1); //white lines

                        if (selectedIndices.Contains(index))
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
                        else if (selectedIndices.Contains(index))
                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex + 2] = pos + rotation * (scale * point.controlPoint2)));
                        else
                            control.UpdateModelMatrix(Matrix4.CreateScale(0.25f) *
                            Matrix4.CreateTranslation(
                                positions[posIndex+2] = pos + point.controlPoint2));

                        GL.CallList(drawLists + 1); //white lines

                        if (selectedIndices.Contains(index))
                            GL.Color4(selectColor);
                        else
                            GL.Color4(color);

                        GL.CallList(drawLists);
                        part++;
                    }
                    else
                        positions[posIndex + 2] = pos;

                    index++;
                    posIndex += 3;
                }
            }
            else
            {
                foreach (PathPoint point in pathPoints)
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
            for(int i = 1; i<pathPoints.Count; i++)
            {
                GL.Begin(PrimitiveType.LineStrip);
                if (pathPoints[i-1].controlPoint2 != Vector3.Zero || pathPoints[i].controlPoint1 != Vector3.Zero)//bezierCurve
                {
                    Vector3 p0 = positions[posIndex];
                    Vector3 p1 = positions[posIndex+2];
                    Vector3 p2 = positions[posIndex+4];
                    Vector3 p3 = positions[posIndex+3];

                    if (pathPoints[i-1].controlPoint2 != Vector3.Zero)
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

                    if (pathPoints[i].controlPoint1 != Vector3.Zero)
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
                if (pathPoints[pathPoints.Count - 1].controlPoint2 != Vector3.Zero || pathPoints[0].controlPoint1 != Vector3.Zero)//bezierCurve
                {
                    Vector3 p0 = positions[posIndex];
                    Vector3 p1 = positions[posIndex + 2];
                    Vector3 p2 = positions[1];
                    Vector3 p3 = positions[0];

                    if (pathPoints[pathPoints.Count - 1].controlPoint2 != Vector3.Zero)
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

                    if (pathPoints[0].controlPoint1 != Vector3.Zero)
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
				"));
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
				"));
				#endregion
                
                lineColorLoc = bezierCurveShaderProgram["lineColor"];
                gapIndexLoc = bezierCurveShaderProgram["gapIndex"];
                isPickingModeLoc = bezierCurveShaderProgram["isPickingMode"];

                Initialized = true;
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
			foreach (PathPoint point in pathPoints)
			{
				i++;
				if (point.controlPoint1 != Vector3.Zero)
					i++;
				if (point.controlPoint2 != Vector3.Zero)
					i++;
			}

			return i;
		}

        public override bool TryStartDragging(DragActionType actionType, int hoveredPart, out LocalOrientation localOrientation, out bool dragExclusively)
        {
            int part = 1;
            int index = 0;
            foreach(PathPoint point in pathPoints)
            {
                if (part == hoveredPart)
                {
                    localOrientation = new LocalOrientation(point.position);
                    dragExclusively = false;
                    return selectedIndices.Contains(index);
                }
                part++;

                if (point.controlPoint1 != Vector3.Zero)
                {
                    if (part == hoveredPart)
                    {
                        if (actionType == DragActionType.TRANSLATE)
                        {
                            localOrientation = new LocalOrientation(point.position + point.controlPoint1);
                            dragExclusively = true; //controlPoints can be moved exclusively
                            return true;
                        }
                        else
                        {
                            localOrientation = new LocalOrientation(point.position);
                            dragExclusively = false;
                            return selectedIndices.Contains(index);
                        }
                    }
                    part++;
                }

                if (point.controlPoint2 != Vector3.Zero)
                {
                    if (part == hoveredPart)
                    {
                        if (actionType == DragActionType.TRANSLATE)
                        {
                            localOrientation = new LocalOrientation(point.position + point.controlPoint2);
                            dragExclusively = true; //controlPoints can be moved exclusively
                            return true;
                        }
                        else
                        {
                            localOrientation = new LocalOrientation(point.position);
                            dragExclusively = false;
                            return selectedIndices.Contains(index);
                        }
                    }
                    part++;
                }
                index++;
            }

            BoundingBox box = BoundingBox.Default;
            int segmentIndex = 0;
            foreach (PathPoint point in pathPoints)
            {
                if (selectedIndices.Contains(segmentIndex))
                    box.Include(new BoundingBox(
                        point.position.X - 0.5f,
                        point.position.X + 0.5f,
                        point.position.Y - 0.5f,
                        point.position.Y + 0.5f,
                        point.position.Z - 0.5f,
                        point.position.Z + 0.5f
                    ));
                segmentIndex++;
            }

            localOrientation = new LocalOrientation(box.GetCenter());
            dragExclusively = false;

            //all points have to be selected
            
            for (int i = 0; i<pathPoints.Count; i++)
            {
                if (!selectedIndices.Contains(i))
                    return false;
            }
            return true;
        }

        public override bool IsSelected()
		{
			return selectedIndices.Count!=0;
		}

		public override uint SelectAll(GL_ControlBase control)
		{
			int i = 0;
			foreach (PathPoint point in pathPoints)
				selectedIndices.Add(i++);

			return REDRAW;
		}

		public override uint SelectDefault(GL_ControlBase control)
		{
			int i = 0;
			foreach (PathPoint point in pathPoints)
				selectedIndices.Add(i++);

			return REDRAW;
		}

		public override bool IsSelected(int partIndex)
		{
			if (partIndex == 0)
			{
				int i = 0;
				foreach (PathPoint point in pathPoints)
				{
					if (!selectedIndices.Contains(i++))
						return false;
				}
				return true;
			}
			else
			{
				int part = 1;
				int i = 0;
				foreach (PathPoint point in pathPoints)
				{
					if (partIndex == part) //this point was selected
					{
						if (selectedIndices.Contains(i))
							return true;
					}
					part++;
					if (point.controlPoint1 != Vector3.Zero)
						part++;

					if (point.controlPoint2 != Vector3.Zero)
						part++;

					i++;
				}
				return false;
			}
		}

		public override uint Select(int partIndex, GL_ControlBase control)
		{
			if (partIndex == 0)
			{
				int i = 0;
				foreach (PathPoint point in pathPoints)
				{
					selectedIndices.Add(i++);
				}
			}
			else
			{
				int part = 1;
				int i = 0;
				foreach (PathPoint point in pathPoints)
				{
					if(partIndex == part) //this point was selected
					{
						selectedIndices.Add(i); //select segment 
						part++;
						break;
					}
					part++;
					if (point.controlPoint1 != Vector3.Zero)
						part++;

					if (point.controlPoint2 != Vector3.Zero)
						part++;

					i++;
				}
			}
			return REDRAW;
		}

		public override uint Deselect(int partIndex, GL_ControlBase control)
		{
			if (partIndex == 0)
				selectedIndices.Clear();
			else
			{
				int part = 1;
				int i = 0;
				foreach (PathPoint point in pathPoints)
				{
					if (partIndex == part) //this point was selected
					{
						if(selectedIndices.Contains(i))
							selectedIndices.Remove(i);
						break;
					}
					part++;
					if (point.controlPoint1 != Vector3.Zero)
						part++;

					if (point.controlPoint2 != Vector3.Zero)
						part++;

					i++;
				}
			}
			return REDRAW;
		}

		public override void ApplyTransformActionToSelection(AbstractTransformAction transformAction)
		{
			Quaternion rotation = transformAction.NewRot(Quaternion.Identity);

			Vector3 scale = transformAction.NewScale(Vector3.One);

			PathPoint[] points = pathPoints.ToArray();

			for(int i = 0; i < pathPoints.Count; i++)
			{
				if (selectedIndices.Contains(i))
				{
					points[i].position = transformAction.NewPos(pathPoints[i].position);

					points[i].controlPoint1 = Vector3.Transform(points[i].controlPoint1, rotation) * scale;

					points[i].controlPoint2 = Vector3.Transform(points[i].controlPoint2, rotation) * scale;
				}
			}
			pathPoints = points.ToList();
		}

        public override void ApplyTransformActionToPart(AbstractTransformAction transformAction, int _part)
        {
            int part = 1;

            PathPoint[] points = pathPoints.ToArray();

            for (int i = 0; i < pathPoints.Count; i++)
            {
                part++;

                if (points[i].controlPoint1 != Vector3.Zero)
                {
                    if (part == _part)
                    {
                        points[i].controlPoint1 = transformAction.NewPos(points[i].position + points[i].controlPoint1) - points[i].position;
                        break;
                    }
                    part++;
                }

                if (points[i].controlPoint2 != Vector3.Zero)
                {
                    if (part == _part)
                    {
                        points[i].controlPoint2 = transformAction.NewPos(points[i].position + points[i].controlPoint2) - points[i].position;
                        break;
                    }
                    part++;
                }
            }
            pathPoints = points.ToList();
        }

        public override uint DeselectAll(GL_ControlBase control)
		{
			selectedIndices.Clear();
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

		public override BoundingBox GetSelectionBox()
		{
			BoundingBox box = BoundingBox.Default;
			int segmentIndex = 0;
			foreach (PathPoint point in pathPoints)
			{
				if(selectedIndices.Contains(segmentIndex))
					box.Include(new BoundingBox(
						point.position.X - 0.5f,
						point.position.X + 0.5f,
						point.position.Y - 0.5f,
						point.position.Y + 0.5f,
						point.position.Z - 0.5f,
						point.position.Z + 0.5f
					));
				segmentIndex++;
			}
			return box;
		}

		public override uint KeyDown(KeyEventArgs e, GL_ControlBase control)
		{
			Vector3 newCP1, newCP2;
			if (e.KeyCode == Keys.R && e.Shift && e.Control)
			{
				newCP1 = new Vector3(-1, 0, 0);
				newCP2 = new Vector3(1, 0, 0);
			}
			else if (e.KeyCode == Keys.R && e.Shift)
			{
				newCP1 = new Vector3();
				newCP2 = new Vector3();
			}
			else
				return base.KeyDown(e, control);
            

			PathPoint[] points = pathPoints.ToArray();

			for (int i = 0; i < pathPoints.Count; i++)
			{
				if (selectedIndices.Contains(i))
				{
					points[i].controlPoint1 = newCP1;
					points[i].controlPoint2 = newCP2;
				}
			}
			pathPoints = points.ToList();

			return REDRAW;
		}

		public override LocalOrientation GetLocalOrientation(int partIndex)
		{
			if (partIndex == 0)
			{
				BoundingBox box = BoundingBox.Default;
				foreach (PathPoint point in pathPoints)
				{
					box.Include(point.position);
				}
				return new LocalOrientation(box.GetCenter());
			}
			else
			{
				int part = 1;
				int i = 0;
				foreach (PathPoint point in pathPoints)
				{
					if (partIndex == part) //this point was selected
						return new LocalOrientation(point.position);

					part++;
					if (point.controlPoint1 != Vector3.Zero)
					{
						if (partIndex == part) //this point was selected
							return new LocalOrientation(point.position);
						part++;
					}

					if (point.controlPoint2 != Vector3.Zero)
					{
						if (partIndex == part) //this point was selected
							return new LocalOrientation(point.position);
						part++;
					}

					i++;
				}
				throw new Exception("Invalid partIndex");
			}
		}

        public override bool IsInRange(float range, float rangeSquared, Vector3 pos)
        {
            BoundingBox box;
            for (int i = 1; i<pathPoints.Count; i++)
            {
                box = BoundingBox.Default;
                box.Include(pathPoints[i - 1].position);
                box.Include(pathPoints[i - 1].position + pathPoints[i - 1].controlPoint2);
                box.Include(pathPoints[i].position + pathPoints[i].controlPoint1);
                box.Include(pathPoints[i].position);

                if (pos.X < box.maxX + range && pos.X > box.minX - range &&
                    pos.Y < box.maxY + range && pos.Y > box.minY - range &&
                    pos.Z < box.maxZ + range && pos.Z > box.minZ - range)
                    return true;
            }
            return false;
        }

        public struct PathPoint
		{
			public PathPoint(Vector3 position, Vector3 controlPoint1, Vector3 controlPoint2)
			{
				this.position = position;
				this.controlPoint1 = controlPoint1;
				this.controlPoint2 = controlPoint2;
			}
			public Vector3 position;
			public Vector3 controlPoint1;
			public Vector3 controlPoint2;
		}
	}
}

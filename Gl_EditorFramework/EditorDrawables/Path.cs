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

namespace GL_EditorFramework.EditorDrawables
{
	public class Path : EditableObject
	{
		private static bool Initialized = false;
		private static ShaderProgram defaultShaderProgram;
		private static ShaderProgram bezierCurveShaderProgram;

		private List<PathPoint> pathPoints;

		private HashSet<int> selectedIndices = new HashSet<int>();

		protected        Vector4 Color     = new Vector4(1f, 0.5f, 0f, 1f);

		public new static Vector4 hoverColor = new Vector4(1, 1, 0.925f, 1);
		public new static Vector4 selectColor = new Vector4(1, 1f, 0.5f, 1);

		public Path()
		{
			pathPoints = new List<PathPoint>();
			pathPoints.Add(new PathPoint(
				new Vector3(0, 0, 0),
				new Vector3(0, 0, 0),
				new Vector3(3, 0, 0)
				));
			pathPoints.Add(new PathPoint(
				new Vector3(8, 4, 2),
				new Vector3(-4, 0, 4),
				new Vector3(4, 0, -4)
				));
			pathPoints.Add(new PathPoint(
				new Vector3(4, 2, -6),
				new Vector3(0, 0, 0),
				new Vector3(0, 0, 0)
				));
		}

		public override void Draw(GL_ControlModern control, Pass pass, EditorSceneBase editorScene)
		{
			if (pass == Pass.TRANSPARENT)
				return;

			if(pass == Pass.PICKING)
				GL.LineWidth(4f);
			else
				GL.LineWidth(1f);

			control.CurrentShader = bezierCurveShaderProgram;

			Matrix4 mtx = Matrix4.CreateTranslation(Position);
			control.UpdateModelMatrix(mtx);

			Quaternion rotation = editorScene.currentAction.newRot(Quaternion.Identity);

			Vector3 scale = editorScene.currentAction.newScale(Vector3.One);

			bool picked = editorScene.hovered==this;

			if(pass == Pass.OPAQUE)
				GL.Uniform4(bezierCurveShaderProgram["color"], (picked && editorScene.hoveredPart == 0) ? hoverColor : Color);
			else
				GL.Uniform4(bezierCurveShaderProgram["color"], control.nextPickingColor());

			GL.Begin(PrimitiveType.Lines);
			int segmentIndex = 0;
			for (int i = 1; i < pathPoints.Count; i++)
			{
				PathPoint p1 = pathPoints[i - 1];
				PathPoint p2 = pathPoints[i];

				Vector3 pos1 = selectedIndices.Contains(segmentIndex) ?
					editorScene.currentAction.newPos(p1.position) : p1.position;

				Vector3 pos2 = selectedIndices.Contains(segmentIndex + 1) ?
					editorScene.currentAction.newPos(p2.position) : p2.position;


				GL.VertexAttrib3(1, pos1 + (selectedIndices.Contains(segmentIndex) ?
					Vector3.Transform(p1.controlPoint2, rotation) * scale : p1.controlPoint2));

				GL.Vertex3(pos1);

				GL.VertexAttrib3(1, pos2 + (selectedIndices.Contains(segmentIndex+1) ?
					Vector3.Transform(p2.controlPoint1, rotation) * scale : p2.controlPoint1));

				GL.Vertex3(pos2);
				segmentIndex++;
			}
			GL.End();


			control.CurrentShader = defaultShaderProgram;

			GL.Begin(PrimitiveType.Points);
			{
				segmentIndex = 0;
				int i = 1;
				foreach (PathPoint point in pathPoints)
				{
					#region assign colors
					//segment
					if (pass == Pass.OPAQUE)
					{
						Vector4 col;
						bool hovered = picked && editorScene.hoveredPart == i;
						bool selected = selectedIndices.Contains(segmentIndex);
						if (hovered && selected)
							col = hoverColor;
						else if (hovered || selected)
							col = selectColor;
						else
							col = Color;
						GL.VertexAttrib4(1, col );
					}
					else //pass == Pass.PICKING
						GL.VertexAttrib4(1, control.nextPickingColor());

					i++;

					if (point.controlPoint1 != Vector3.Zero)
					{
						//controlPoint in
						if (pass == Pass.OPAQUE)
						{
							GL.VertexAttrib4(2, picked && editorScene.hoveredPart == i ? selectColor : Color);
							i++;
						}
						else //pass == Pass.PICKING
							GL.VertexAttrib4(2, control.nextPickingColor());
					}
					if (point.controlPoint2 != Vector3.Zero)
					{
						//controlPoint out
						if (pass == Pass.OPAQUE)
						{
							GL.VertexAttrib4(3, picked && editorScene.hoveredPart == i ? selectColor : Color);
							i++;
						}
						else //pass == Pass.PICKING
							GL.VertexAttrib4(3, control.nextPickingColor());
					}
					#endregion

					#region calculate positions
					Vector3 pos = selectedIndices.Contains(segmentIndex) ?
						editorScene.currentAction.newPos(point.position) : point.position;


					GL.VertexAttrib3(4, pos + (selectedIndices.Contains(segmentIndex) ?
						Vector3.Transform(point.controlPoint1, rotation) * scale : point.controlPoint1));

					GL.VertexAttrib3(5, pos + (selectedIndices.Contains(segmentIndex) ?
						Vector3.Transform(point.controlPoint2, rotation) * scale : point.controlPoint2));

					GL.Vertex3(pos);
					#endregion

					segmentIndex++;
				}
			}
			GL.End();
		}

		public override void Draw(GL_ControlLegacy control, Pass pass, EditorSceneBase editorScene)
		{
			throw new NotImplementedException();
		}

		public override void Prepare(GL_ControlModern control)
		{
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
				in vec4 position;
				
				layout(location = 1) in vec4 _color;
				layout(location = 2) in vec4 _color_cp1;
				layout(location = 3) in vec4 _color_cp2;
				layout(location = 4) in vec3 _cp1;
				layout(location = 5) in vec3 _cp2;

				out vec4 color;
				out vec4 color_cp1;
				out vec4 color_cp2;
				out vec3 cp1;
				out vec3 cp2;

				void main(){
					cp1 = _cp1;
					cp2 = _cp2;
					color = _color;
					color_cp1 = _color_cp1;
					color_cp2 = _color_cp2;
					gl_Position = position;
				}");

				#region block shader
				defaultShaderProgram = new ShaderProgram(defaultFrag, defaultVert);

				defaultShaderProgram.AttachShader(new Shader(
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
				
				float cubeScale;
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
					cubeScale = 0.5;
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
				", ShaderType.GeometryShader));

				defaultShaderProgram.LinkShaders();
				#endregion
				

				var connectLinesFrag = new FragmentShader(
				  @"#version 330
				in vec4 fragColor;
				void main(){
					gl_FragColor = fragColor;
				}");
				var connectLinesVert = new VertexShader(
				  @"#version 330
				in vec4 position;
				
				layout(location = 1) in vec3 _controlPoint;

				out vec3 controlPoint;

				void main(){
					controlPoint = _controlPoint;
					gl_Position = position;
				}");

				#region connections shader
				bezierCurveShaderProgram = new ShaderProgram(connectLinesFrag, connectLinesVert);

				bezierCurveShaderProgram.AttachShader(new Shader(
				  @"#version 330
                layout(lines) in;
				layout(line_strip, max_vertices = 119) out;
				
				in vec3 controlPoint[];
				out vec4 fragColor;

				uniform vec4 color;
				uniform mat4 mtxMdl;
				uniform mat4 mtxCam;
				
				float cubeScale;
				vec4 pos;

				mat4 mtx = mtxCam*mtxMdl;
				
				vec3 p0 = gl_in[0].gl_Position.xyz;
				vec3 p1 = controlPoint[0];
				vec3 p2 = controlPoint[1];
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
					float u = 1f - t;
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
					fragColor = vec4(1,1,1,1);
					cubeScale = 0.5f;
					pos = vec4(p0,1);
					
					face(0,1,2,3);
					face(4,5,6,7);
					line(0,4);
					line(1,5);
					line(2,6);
					line(3,7);
					
					pos = vec4(p3,1);
					face(0,1,2,3);
					face(4,5,6,7);
					line(0,4);
					line(1,5);
					line(2,6);
					line(3,7);
					
					cubeScale = 0.25f;
					
					if(p1!=p0){
						fragColor = color;
						gl_Position = mtx * vec4(p0,1); EmitVertex();
						gl_Position = mtx * vec4(p1,1); EmitVertex();
						EndPrimitive();
						
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
						fragColor = color;
						gl_Position = mtx * vec4(p2,1); EmitVertex();
						gl_Position = mtx * vec4(p3,1); EmitVertex();
						EndPrimitive();
						
						fragColor = vec4(1,1,1,1);
						pos = vec4(p2,1);
						face(0,1,2,3);
						face(4,5,6,7);
						line(0,4);
						line(1,5);
						line(2,6);
						line(3,7);
					}
					
					fragColor = color;

					for(float t = 0; t<=1.0; t+=0.0625){
						getPointAtTime(t);
					}
					EndPrimitive();
				}
				", ShaderType.GeometryShader));
				#endregion

				bezierCurveShaderProgram.LinkShaders();

				Initialized = true;
			}
		}

		public override void Prepare(GL_ControlLegacy control)
		{
			throw new NotImplementedException();
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

		public override bool CanStartDragging() => true;

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

		public override void ApplyTransformActionToSelection(EditorSceneBase.AbstractTransformAction transformAction)
		{
			Quaternion rotation = transformAction.newRot(Quaternion.Identity);

			Vector3 scale = transformAction.newScale(Vector3.One);

			PathPoint[] points = pathPoints.ToArray();

			for(int i = 0; i < pathPoints.Count; i++)
			{
				if (selectedIndices.Contains(i))
				{
					points[i].position = transformAction.newPos(pathPoints[i].position);

					points[i].controlPoint1 = Vector3.Transform(points[i].controlPoint1, rotation) * scale;

					points[i].controlPoint2 = Vector3.Transform(points[i].controlPoint2, rotation) * scale;
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

		struct PathPoint
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

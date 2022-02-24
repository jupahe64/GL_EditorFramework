using GL_EditorFramework.EditorDrawables;
using OpenTK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GL_EditorFramework
{
    public delegate TValue ValueGetter<TObject, TValue>(TObject obj);
    public delegate void ValueSetter<TObject, TValue>(TObject obj, TValue value);

    public class MultipleValueCapture<TObject, TValue>
    {
        private ValueGetter<TObject, TValue> getter;
        private ValueSetter<TObject, TValue> setter;
        
        private Multiple<TValue> prevValue;

        public Multiple<TValue> Value { get; set; }


        public MultipleValueCapture(ValueGetter<TObject, TValue> getter, ValueSetter<TObject, TValue> setter, IList objects)
        {
            this.getter = getter;
            this.setter = setter;

            Update(objects);
        }

        public void Update(IList objects)
        {
            Multiple<TValue> res = getter((TObject)objects[0]);

            for (int i = 0; i < objects.Count; i++)
            {
                if (res != getter((TObject)objects[i]))
                {
                    Value = new Multiple<TValue>();
                    return;
                }
            }

            prevValue = res;
            Value = res;
        }

        public void ApplyIfChanged(IList objects, EditorSceneBase scene = null)
        {
            var infos = new List<EditorSceneBase.RevertableMassPropertyChange<TObject, TValue>.Info>();

            if (Value != prevValue)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    var value = getter((TObject)objects[i]);

                    if(!value.Equals(Value.SharedValue))
                    {
                        infos.Add(new EditorSceneBase.RevertableMassPropertyChange<TObject, TValue>.Info((TObject)objects[i], value));

                        setter((TObject)objects[i], Value.SharedValue);
                    }
                }

                prevValue = Value;

                if (infos.Count > 0)
                    scene?.AddToUndo(new EditorSceneBase.RevertableMassPropertyChange<TObject, TValue>(getter, setter, infos.ToArray()));
            }
        }
    }

    public class MultipleVector3Capture<TObject>
    {
        private ValueGetter<TObject, Vector3> getter;
        private ValueSetter<TObject, Vector3> setter;

        private MultipleVector3 prevValue;

        public MultipleVector3 Value { get; set; }


        public MultipleVector3Capture(ValueGetter<TObject, Vector3> getter, ValueSetter<TObject, Vector3> setter, IList objects)
        {
            this.getter = getter;
            this.setter = setter;

            Update(objects);
        }

        public void Update(IList objects)
        {
            MultipleVector3 res = getter((TObject)objects[0]);
            for (int i = 1; i < objects.Count; i++)
            {
                if (getter((TObject)objects[i]).X != res.X)
                {
                    res.X = new Multiple<float>();
                }

                if (getter((TObject)objects[i]).Y != res.Y)
                {
                    res.Y = new Multiple<float>();
                }

                if (getter((TObject)objects[i]).Z != res.Z)
                {
                    res.Z = new Multiple<float>();
                }

                if (res == new MultipleVector3())
                    break;
            }

            prevValue = res;
            Value = res;
        }

        public void ApplyIfChanged(IList objects, EditorSceneBase scene = null)
        {
            if (Value != prevValue)
            {
                var infos = new List<EditorSceneBase.RevertableMassPropertyChange<TObject, Vector3>.Info>();

                for (int i = 0; i < objects.Count; i++)
                {
                    var value = getter((TObject)objects[i]);
                    var _prevValue = value;

                    if (Value.X != value.X)
                        value.X = Value.X.SharedValue;

                    if (Value.Y != value.Y)
                        value.Y = Value.Y.SharedValue;

                    if (Value.Z != value.Z)
                        value.Z = Value.Z.SharedValue;

                    if(_prevValue != value)
                    {
                        infos.Add(new EditorSceneBase.RevertableMassPropertyChange<TObject, Vector3>.Info((TObject)objects[i], _prevValue));

                        setter((TObject)objects[i], value);
                    }
                }

                prevValue = Value;

                if(infos.Count>0)
                    scene?.AddToUndo(new EditorSceneBase.RevertableMassPropertyChange<TObject, Vector3>(getter, setter, infos.ToArray()));
            }
        }
    }

    public struct Multiple<T>
    {
        public Multiple(T value)
        {
            SharedValue = value;
            HasSharedValue = true;
        }


        public T SharedValue { get; set; }

        public bool HasSharedValue { get; set; }

        public static implicit operator Multiple<T>(T value) => new Multiple<T>() { HasSharedValue = true, SharedValue = value};

        public static bool operator ==(Multiple<T> left, Multiple<T> right)
        {
            if ((left.HasSharedValue == false) && (right.HasSharedValue = false))
                return true;
            else
                return left.HasSharedValue == right.HasSharedValue && left.SharedValue.Equals(right.SharedValue);
        }

        public static bool operator !=(Multiple<T> left, Multiple<T> right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Multiple<T> other)
                return this == other;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    public struct MultipleVector3
    {
        public MultipleVector3(Multiple<float> x, Multiple<float> y, Multiple<float> z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool IsAllShared => X.HasSharedValue && Y.HasSharedValue && Z.HasSharedValue;

        public static implicit operator MultipleVector3(Vector3 vec) => new MultipleVector3(vec.X, vec.Y, vec.Z);

        public Vector3 SharedVector => new Vector3(X.SharedValue, Y.SharedValue, Z.SharedValue);

        public Multiple<float> X { get; set; }
        public Multiple<float> Y { get; set; }
        public Multiple<float> Z { get; set; }

        public static bool operator ==(MultipleVector3 left, MultipleVector3 right)
        {
            return left.X == right.X &&
                   left.Y == right.Y &&
                   left.Z == right.Z;
        }

        public static bool operator !=(MultipleVector3 left, MultipleVector3 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is MultipleVector3 other)
                return this == other;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    public class ObjectUITooltip : Form
    {
        string text = string.Empty;

        public new string Text
        {
            get => text;
            set
            {
                text = value;
                Refresh();
            }
        }

        public ObjectUITooltip()
        {
            SetStyle(
        ControlStyles.AllPaintingInWmPaint |
        ControlStyles.UserPaint |
        ControlStyles.OptimizedDoubleBuffer,
        true);

            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            BackColor = SystemColors.ControlLightLight;

            ShowInTaskbar = false;
        }

        #region make absolutely sure that this form can't be focused on
        private const int SW_SHOWNOACTIVATE = 4;
        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        static extern bool SetWindowPos(
             int hWnd,             // Window handle
             int hWndInsertAfter,  // Placement-order handle
             int X,                // Horizontal position
             int Y,                // Vertical position
             int cx,               // Width
             int cy,               // Height
             uint uFlags);         // Window positioning flags

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static void ShowInactiveTopmost(Form frm)
        {
            ShowWindow(frm.Handle, SW_SHOWNOACTIVATE);
            SetWindowPos(frm.Handle.ToInt32(), HWND_TOPMOST,
            frm.Left, frm.Top, frm.Width, frm.Height,
            SWP_NOACTIVATE);
        }
        #endregion

        public void Show(Point location, int width, string text)
        {
            Bounds = new Rectangle(location, new Size(width, 
                (int)Math.Ceiling(Font.GetHeight(DeviceDpi)) + 5));

            this.text = text;

            ShowInactiveTopmost(this);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.DrawRectangle(SystemPens.Highlight, new Rectangle(0, 0, Width - 1, Height - 1));

            e.Graphics.DrawString(text, Font, SystemBrushes.ControlText, new RectangleF(2, 2, Width - 4, Height - 4));

            SetBounds(-1, -1, -1, (int)e.Graphics.MeasureString(text, Font, Width).Height+4, BoundsSpecified.Height);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams baseParams = base.CreateParams;

                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                baseParams.ExStyle |= (int)(WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

                if (Environment.OSVersion.Version.Major >= 6)
                    baseParams.ExStyle |= 0x02000000; //WS_EX_COMPOSITED

                return baseParams;
            }
        }

        private const int WM_MOUSEACTIVATE = 0x0021, MA_NOACTIVATE = 0x0003;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }
    }

    public class ObjectUIControl : FlexibleUIControl, IObjectUIControl, IObjectUIControlWithMultipleSupport
    {
        ObjectUITooltip tooltip = new ObjectUITooltip();

        int fieldWidth;
        const int fieldSpace = 2;
        const int beforeTwoLineSpacing = 5;
        const int fullWidthSpace = 5;
        const int margin = 10;

        int rowHeight;

        private int currentY;

        protected override bool HasNoUIContent() => containerInfos.Count == 0;

        class ContainerInfo
        {
            public bool isExpanded;
            public string name;
            public IObjectUIContainer objectUIContainer;

            public ContainerInfo(IObjectUIContainer objectUIContainer, string name)
            {
                this.isExpanded = true;
                this.name = name;
                this.objectUIContainer = objectUIContainer;
            }
        }

        List<ContainerInfo> containerInfos = new List<ContainerInfo>();

        public IEnumerable<IObjectUIContainer> ObjectUIContainers => containerInfos.Select(x => x.objectUIContainer);

        Timer tooltipFadeTimer = new Timer() { Interval = 20 };

        public ObjectUIControl()
        {
            tooltipFadeTimer.Tick += TooltipFadeTimer_Tick;
            tooltipFadeTimer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            OnFontChanged(e);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            rowHeight = (int)Math.Ceiling(Font.GetHeight(DeviceDpi)) + 5;
        }

        [System.Diagnostics.DebuggerStepThrough]
        private void TooltipFadeTimer_Tick(object sender, EventArgs e)
        {
            if(tooltip.Opacity<0.01)
                tooltip.Opacity += 0.0005;
            else
                tooltip.Opacity += 0.3;
        }


        public void AddObjectUIContainer(IObjectUIContainer objectUIContainer, string name)
        {
            containerInfos.Add(new ContainerInfo(objectUIContainer, name));
        }

        public void RemoveObjectUIContainer(IObjectUIContainer objectUIContainer)
        {
            UnFocusInput();

            foreach (ContainerInfo containerInfo in containerInfos)
            {
                if (containerInfo.objectUIContainer == objectUIContainer)
                {
                    containerInfos.Remove(containerInfo);
                    break;
                }
            }
        }

        public void ClearObjectUIContainers()
        {
            UnFocusInput();

            containerInfos.Clear();
        }
        int horizontalSperatorStartY;

        bool acceptSeperatorCalls = false;

        private void BeginHorizontalSeperator()
        {
            if (!acceptSeperatorCalls)
                return;

            horizontalSperatorStartY = currentY;
        }

        bool draggingHSeperator = false;

        private void EndHorizontalSeperator()
        {
            if (!acceptSeperatorCalls)
                return;

            int t = horizontalSperatorStartY + 1;

            int b = currentY - 6;

            int x = usableWidth - fieldWidth * 4 - fieldSpace * 3 - margin - 5;

            if (t >= b || (Math.Abs(x- mousePos.X - 15)>20 && !draggingHSeperator)) //seperator has length of 0, don't draw it
                return;


            g.DrawLine(
                draggingHSeperator ? Pens.Black : SystemPens.ControlDark, 
                x, t, x, b);

            if (eventType == EventType.DRAG_START && new Rectangle(x - 4, t, x + 4, b - t).Contains(mousePos))
            {
                draggingHSeperator = true;

                seperatorPositionBeforeDrag = seperatorPosition;
            }
        }


        double seperatorPosition = 0.6;

        double seperatorPositionBeforeDrag;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);


        }

        Rectangle nameClipping;

        Rectangle contentClipping;

        float[] clipboardNumbers;

        int clipboardVectorSize;

        static int copyBtnImageWidth = Properties.Resources.CopyIcon.Width;

        string tooltipText;
        int tooltipY;

        string currentTooltipAreaText = null;
        int currentTooltipAreaTop = 0;

        /// <summary>
        /// <para>Sets the tooltip text for all following controls in the ObjectUIContainer</para>
        /// <para>Setting text to null will deactivate tooltips for the following controls</para>
        /// </summary>
        /// <param name="text">The text that will be displayed in the tooltip box</param>
        public void SetTooltip(string text)
        {
            if(tooltipText == null && //no tooltip area was hovered yet

                currentTooltipAreaText != null && //the current tooltip area has a tooltip defined

                mousePos.Y >= currentTooltipAreaTop && mousePos.Y < currentY) //the current tooltip is hovered
            {
                tooltipText = currentTooltipAreaText;
                tooltipY = currentY;
            }
            else
            {
                currentTooltipAreaText = text;
                currentTooltipAreaTop = currentY;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!TryInitDrawing(e))
            {
                AutoScrollMinSize = new Size();
                return;
            }

            if (eventType == EventType.DRAG_END || eventType == EventType.CLICK)
                draggingHSeperator = false;

            if (eventType == EventType.DRAG_ABORT && draggingHSeperator)
            {
                draggingHSeperator = false;
                seperatorPosition = seperatorPositionBeforeDrag;
            }

            if (draggingHSeperator)
            {
                seperatorPosition = Math.Min(Math.Max(0.25, (mousePos.X - margin) / (double)(usableWidth - 2 * margin)), 0.75);
            }

            fieldWidth = (int)(((usableWidth - 2 * margin) * (1-seperatorPosition) - fieldSpace * 3) / 4.0);

            nameClipping = new Rectangle(0, 0, usableWidth - margin - fieldWidth * 4 - fieldSpace * 3 - 10, Height);

            contentClipping = new Rectangle(0, 0, usableWidth - margin, Height);

            currentY = margin + AutoScrollPosition.Y;

            tooltipText = null;

            currentTooltipAreaText = null;

            #region Check Clipboard
            try
            {
                var text = Clipboard.GetText();

                if (!string.IsNullOrEmpty(text) && text.Length < 100) //Larger strings will be ignored for performance sake
                {
                    var values = text.Split(',');

                    clipboardNumbers = new float[values.Length];

                    clipboardVectorSize = values.Length;

                    for (int i = 0; i < values.Length; i++)
                    {
                        if (float.TryParse(values[i].Trim(), out float value))
                            clipboardNumbers[i] = value;
                        else
                            clipboardVectorSize = 0;
                    }
                }
                #endregion
            }
            catch (Exception) { }

            foreach (ContainerInfo containerInfo in containerInfos)
            {
                int lastY = currentY - margin / 2;
                bool hovered = new Rectangle(Width - margin - 20 - SystemInformation.VerticalScrollBarWidth, currentY, 20 + SystemInformation.VerticalScrollBarWidth, 20).Contains(mousePos);

                if (hovered && eventType == EventType.CLICK)
                {
                    containerInfo.isExpanded = !containerInfo.isExpanded;

                    eventType = EventType.DRAW; //Click Handled
                }

                g.TranslateTransform(usableWidth - margin - 20, currentY);
                g.FillPolygon(hovered ? SystemBrushes.ControlDark : Framework.backBrush, containerInfo.isExpanded ? arrowDown : arrowLeft);
                g.ResetTransform();
                Heading(containerInfo.name);
                Spacing(margin / 2);

                if (containerInfo.isExpanded)
                {
                    acceptSeperatorCalls = true;

                    BeginHorizontalSeperator();
                    containerInfo.objectUIContainer.DoUI(this);

                    EndHorizontalSeperator();

                    acceptSeperatorCalls = false;
                }

                g.DrawRectangle(SystemPens.ControlDark, margin / 2, lastY, usableWidth - margin, currentY - lastY);

                Spacing(margin);

                SetTooltip(null);

                if (valueChangeEvents != 0)
                {
                    if ((valueChangeEvents & VALUE_CHANGE_START) > 0)
                        containerInfo.objectUIContainer.OnValueChangeStart();
                    if ((valueChangeEvents & VALUE_CHANGED) > 0)
                        containerInfo.objectUIContainer.OnValueChanged();
                    if ((valueChangeEvents & VALUE_SET) > 0)
                        containerInfo.objectUIContainer.OnValueSet();
                }
            }

            AutoScrollMinSize = new Size(0, currentY - AutoScrollPosition.Y + margin);

            if (tooltipText == null)
                tooltip.Hide();
            else
            {
                if (!tooltip.Visible)
                {
                    tooltip.Opacity = 0.0;
                    tooltip.Show(PointToScreen(new Point(margin, tooltipY)), usableWidth, tooltipText);
                }
                else
                {
                    Point screenPoint = PointToScreen(new Point(margin, tooltipY));
                    if (tooltip.Location.Y != screenPoint.Y)
                    {
                        tooltip.Location = screenPoint;
                        tooltip.Opacity = 0.0;
                    }

                    tooltip.Text = tooltipText;
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Refresh();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            tooltip.Hide();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            tooltip.Hide();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            tooltip.Hide();
        }

        #region autogenerated code (I wish lol)

        public float NumberInput(float number, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
            =>
        NumberInput((Multiple<float>)number, name,
            increment, incrementDragDivider, min, max, wrapAround).SharedValue;


        public Vector3 Vector3Input(Vector3 vec, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
            => 
        Vector3Input((MultipleVector3)vec, name,
            increment, incrementDragDivider, min, max, wrapAround).SharedVector;


        public Vector3 FullWidthVector3Input(Vector3 vec, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
            =>
        FullWidthVector3Input((MultipleVector3)vec, name,
            increment, incrementDragDivider, min, max, wrapAround).SharedVector;


        public bool CheckBox(string name, bool isChecked) 
            =>
        CheckBox(name, (Multiple<bool>)isChecked).SharedValue;


        public string TextInput(string text, string name)
            =>
        TextInput((Multiple<string>)text, name).SharedValue;


        public string FullWidthTextInput(string text, string name)
            =>
        FullWidthTextInput((Multiple<string>)text, name).SharedValue;


        public object ChoicePicker(string name, object value, IList values)
            =>
        ChoicePicker(name, new Multiple<object>(value), values).SharedValue;


        public string DropDownTextInput(string name, string value, string[] dropDownItems, bool filterSuggestions = true)
            =>
        DropDownTextInput(name, (Multiple<string>)value, dropDownItems, filterSuggestions).SharedValue;
        #endregion


        #region IObjectControl

        public Multiple<float> NumberInput(Multiple<float> number, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false)
        {
            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();

            number = MultipleNumericInputField(usableWidth - margin - fieldWidth * 4 - fieldSpace * 3, currentY, fieldWidth * 4 + fieldSpace * 3, number,
                new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround), true);
            
            currentY += rowHeight;

            return number;
        }

        protected bool UndefinedFieldButton(int x, int y, int width, string text, bool isCentered = true)
        {
            bool clicked = false;

            if (new Rectangle(x, y, width, textBoxHeight + 2).Contains(mousePos))
            {
                DrawField(x, y, width, text, SystemBrushes.ActiveCaption, SystemBrushes.ControlLightLight, isCentered, Brushes.Gray);

                clicked = eventType == EventType.CLICK;

                if (clicked)
                    eventType = EventType.DRAW; //Click Handled
            }
            else
            {
                DrawField(x, y, width, text, SystemBrushes.InactiveCaption, SystemBrushes.ControlLightLight, isCentered, Brushes.Gray);
            }

            index++;

            return clicked;
        }

        protected Multiple<float> MultipleNumericInputField(int x, int y, int width, Multiple<float> number, NumberInputInfo info, bool isCentered, float multiResolveValue = 0)
        {
            if (number.HasSharedValue)
                return NumericInputField(x, y, width, number.SharedValue, info, isCentered);
            else
            {
                if (UndefinedFieldButton(x, y, width, "?"))
                {
                    valueChangeEvents |= VALUE_SET;
                    return multiResolveValue;
                }
                else
                    return number;
            }
        }

        public MultipleVector3 Vector3Input(MultipleVector3 vec, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false, Vector3 multiResolveValue = new Vector3(), bool allowMixed = true)
        {
            void ResolveVec()
            {
                if (allowMixed)
                {
                    if (!vec.X.HasSharedValue)
                        vec.X = multiResolveValue.X;

                    if (!vec.Y.HasSharedValue)
                        vec.Y = multiResolveValue.Y;

                    if (!vec.Z.HasSharedValue)
                        vec.Z = multiResolveValue.Z;
                }
                else
                    vec = multiResolveValue;
            }


            NumberInputInfo input = new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround);

            if (eventType == EventType.DRAG_START && new Rectangle(usableWidth - margin - fieldWidth * 4 - fieldSpace * 3, currentY, usableWidth, 15).Contains(mousePos))
                valueChangeEvents |= VALUE_CHANGE_START;

            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();

            if (!allowMixed && !vec.IsAllShared)
            {
                bool clicked = false;
                clicked |= UndefinedFieldButton(usableWidth - margin - fieldWidth * 4 - fieldSpace * 3, currentY, fieldWidth, "?", true);
                clicked |= UndefinedFieldButton(usableWidth - margin - fieldWidth * 3 - fieldSpace * 2, currentY, fieldWidth, "?", true);
                clicked |= UndefinedFieldButton(usableWidth - margin - fieldWidth * 2 - fieldSpace * 1, currentY, fieldWidth, "?", true);

                if (clicked)
                {
                    ResolveVec();
                    valueChangeEvents |= VALUE_SET;
                }
            }
            else
            {
                vec.X = MultipleNumericInputField(usableWidth - margin - fieldWidth * 4 - fieldSpace * 3, currentY, fieldWidth, vec.X, input, true, multiResolveValue.X);
                vec.Y = MultipleNumericInputField(usableWidth - margin - fieldWidth * 3 - fieldSpace * 2, currentY, fieldWidth, vec.Y, input, true, multiResolveValue.Y);
                vec.Z = MultipleNumericInputField(usableWidth - margin - fieldWidth * 2 - fieldSpace * 1, currentY, fieldWidth, vec.Z, input, true, multiResolveValue.Z);
            }

            int copyBtnX = usableWidth - margin - fieldWidth;

            g.SetClip(contentClipping);

            if (vec.IsAllShared)
            {
                //Copy Button
                if (ImageButton(copyBtnX, currentY, Properties.Resources.CopyIcon, Properties.Resources.CopyIconClick, Properties.Resources.CopyIconHover))
                    Clipboard.SetText($"{vec.X.SharedValue},{vec.Y.SharedValue},{vec.Z.SharedValue}");
            }
            else
            {
                //Multi Resolve Button
                if (ImageButton(copyBtnX, currentY, Properties.Resources.ResolveMultiVector3Icon, Properties.Resources.ResolveMultiVector3IconClick, Properties.Resources.ResolveMultiVector3IconHover))
                {
                    valueChangeEvents |= VALUE_SET;
                    ResolveVec();
                }
            }

            if (clipboardVectorSize == 3)
            {
                //Paste Button
                if (ImageButton(copyBtnX + copyBtnImageWidth, currentY, Properties.Resources.PasteIcon, Properties.Resources.PasteIconClick, Properties.Resources.PasteIconHover))
                {
                    vec.X = clipboardNumbers[0];
                    vec.Y = clipboardNumbers[1];
                    vec.Z = clipboardNumbers[2];

                    valueChangeEvents |= VALUE_SET;
                }
            }

            g.ResetClip();

            currentY += rowHeight;
            return vec;
        }

        public MultipleVector3 FullWidthVector3Input(MultipleVector3 vec, string name,
            float increment = 1, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false, Vector3 multiResolveValue = new Vector3())
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            currentY += beforeTwoLineSpacing;

            NumberInputInfo input = new NumberInputInfo(increment, incrementDragDivider, min, max, wrapAround);

            const int nameWidth = 13;
            int width = (usableWidth - margin * 2 - fullWidthSpace * 2) / 3;

            g.DrawString(name, HeadingFont, SystemBrushes.ControlText, margin, currentY);
            currentY += rowHeight;

            DrawText(margin, currentY, "X");
            vec.X = MultipleNumericInputField(margin + nameWidth, currentY, width - nameWidth, vec.X, input, true, multiResolveValue.X);

            DrawText(10 + width + fullWidthSpace, currentY, "Y");
            vec.Y = MultipleNumericInputField(margin + nameWidth + width + fullWidthSpace, currentY, width - nameWidth, vec.Y, input, true, multiResolveValue.Y);

            DrawText(10 + width * 2 + fullWidthSpace * 2, currentY, "Z");
            vec.Z = MultipleNumericInputField(margin + nameWidth + width * 2 + fullWidthSpace * 2, currentY, width - nameWidth, vec.Z, input, true, multiResolveValue.Z);


            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return vec;
        }

        public bool Button(string name)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            bool clicked = Button(margin, currentY, usableWidth - margin * 2, name);
            currentY += rowHeight + 4;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return clicked;
        }

        public int DoubleButton(string name, string name2)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            int width = (usableWidth - margin * 2 - fullWidthSpace) / 2;

            int clickedIndex = 0;

            if (Button(margin, currentY, width, name))
                clickedIndex = 1;
            if (Button(margin + width + fullWidthSpace, currentY, width, name2))
                clickedIndex = 2;

            currentY += rowHeight + 4;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return clickedIndex;
        }

        public int TripleButton(string name, string name2, string name3)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            int width = (usableWidth - margin * 2 - fullWidthSpace * 2) / 3;

            int clickedIndex = 0;

            if (Button(margin, currentY, width, name))
                clickedIndex = 1;
            if (Button(margin + width + fullWidthSpace, currentY, width, name2))
                clickedIndex = 2;
            if (Button(margin + width * 2 + fullWidthSpace * 2, currentY, usableWidth - margin * 2 - width * 2 - fullWidthSpace * 2, name3))
                clickedIndex = 3;

            currentY += rowHeight + 4;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return clickedIndex;
        }

        public int QuadripleButton(string name, string name2, string name3, string name4)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            int width = (usableWidth - margin * 2 - fullWidthSpace * 3) / 4;

            int clickedIndex = 0;

            if (Button(margin, currentY, width, name))
                clickedIndex = 1;
            if (Button(margin + width + fullWidthSpace, currentY, width, name2))
                clickedIndex = 2;
            if (Button(margin + width * 2 + fullWidthSpace * 2, currentY, width, name3))
                clickedIndex = 3;
            if (Button(margin + width * 3 + fullWidthSpace * 3, currentY, width, name4))
                clickedIndex = 4;

            currentY += rowHeight + 4;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return clickedIndex;
        }

        public bool Link(string name)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            bool clicked = false;

            if (new Rectangle(margin, currentY, (int)g.MeasureString(name, Font).Width, textBoxHeight).Contains(mousePos))
            {
                if (mouseDown)
                    g.DrawString(name, LinkFont, Brushes.Red, margin, currentY);
                else
                    g.DrawString(name, LinkFont, Brushes.Blue, margin, currentY);

                clicked = eventType == EventType.CLICK;

                if(clicked)
                    eventType = EventType.DRAW; //Click Handled
            }
            else
            {
                g.DrawString(name, LinkFont, Brushes.Blue, margin, currentY);
            }

            currentY += rowHeight;
            index++;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return clicked;
        }

        public void PlainText(string text)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            DrawText(margin, currentY, text);
            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it
        }

        public void Heading(string text)
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            g.DrawString(text, HeadingFont, SystemBrushes.ControlText, margin, currentY);
            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it
        }

        public Multiple<bool> CheckBox(string name, Multiple<bool> isChecked, bool multiResolveValue = false)
        {
            string markString = "?";

            EndHorizontalSeperator(); //this control doesn't get aligned to it

            DrawText(margin, currentY, name);

            if (new Rectangle(usableWidth - margin - (textBoxHeight + 2), currentY, textBoxHeight + 2, textBoxHeight + 2).Contains(mousePos))
            {
                if (eventType == EventType.DRAG_START)
                    valueChangeEvents |= VALUE_CHANGE_START;

                if (eventType == EventType.CLICK)
                {
                    if (isChecked.HasSharedValue)
                        isChecked = !isChecked.SharedValue;
                    else
                        isChecked = multiResolveValue;

                    valueChangeEvents |= VALUE_SET;

                    eventType = EventType.DRAW; //Click Handled
                }

                if (isChecked.HasSharedValue)
                    markString = isChecked.SharedValue ? "x" : "";

                DrawField(usableWidth - margin - (textBoxHeight + 2), currentY, textBoxHeight + 2, markString,
                    SystemBrushes.Highlight, SystemBrushes.ControlLightLight);
            }
            else
            {
                if (isChecked.HasSharedValue)
                    markString = isChecked.SharedValue ? "x" : "";

                DrawField(usableWidth - margin - (textBoxHeight + 2), currentY, textBoxHeight + 2, markString,
                    SystemBrushes.ActiveBorder, SystemBrushes.ControlLightLight);
            }

            currentY += rowHeight;
            index++;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return isChecked;
        }

        protected Multiple<string> MultipleTextInputField(int x, int y, int width, Multiple<string> text, bool isCentered, string multiResolveValue = "")
        {
            if (text.HasSharedValue)
                return TextInputField(x, y, width, text.SharedValue, isCentered);
            else
            {
                if (UndefinedFieldButton(x, y, width, "???"))
                {
                    valueChangeEvents |= VALUE_SET;
                    return multiResolveValue;
                }
                else
                    return text;
            }
        }

        public Multiple<string> TextInput(Multiple<string> text, string name, string multiResolveValue = "")
        {
            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();

            text = MultipleTextInputField(usableWidth - margin - fieldWidth * 4 - fieldSpace * 3, currentY, fieldWidth * 4 + fieldSpace * 3, text, false, multiResolveValue);

            currentY += rowHeight;
            return text;
        }

        public Multiple<string> FullWidthTextInput(Multiple<string> text, string name, string multiResolveValue = "")
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            DrawText(margin, currentY, name);
            currentY += rowHeight;
            text = MultipleTextInputField(margin, currentY, usableWidth - margin * 2, text, false, multiResolveValue);
            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return text;
        }

        public Multiple<object> ChoicePicker(string name, Multiple<object> value, IList values, object multiResolveValue = null)
        {
            int width = fieldWidth * 4 + fieldSpace * 3;

            int x = usableWidth - width - margin;

            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();



            if (value.HasSharedValue)
                value = ChoicePickerField(x, currentY, width, value.SharedValue, values);
            else
            {
                if (UndefinedFieldButton(x, currentY, width, "?", true))
                {
                    valueChangeEvents |= VALUE_SET;
                    value = multiResolveValue ?? values[0];
                }
            }

            currentY += rowHeight;
            return value;
        }

        public Multiple<string> DropDownTextInput(string name, Multiple<string> value, string[] dropDownItems, bool filterSuggestions = true, string multiResolveValue = "")
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it
            
            int width = usableWidth -margin * 2;

            g.SetClip(nameClipping);

            DrawText(margin, currentY, name);

            g.ResetClip();

            currentY += rowHeight;


            if (value.HasSharedValue)
                value = DropDownTextInputField(margin, currentY, width, value.SharedValue, dropDownItems, filterSuggestions);
            else
            {
                if (UndefinedFieldButton(margin, currentY, width, "???"))
                {
                    valueChangeEvents |= VALUE_SET;
                    value = multiResolveValue;
                }
            }

            currentY += rowHeight;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it

            return value;
        }

        public void Spacing(int amount)
        {
            currentY += amount;
        }

        public void VerticalSeperator()
        {
            EndHorizontalSeperator(); //this control doesn't get aligned to it

            g.FillRectangle(SystemBrushes.ControlLightLight, margin, currentY - 2, usableWidth - margin * 2, 2);
            g.FillRectangle(SystemBrushes.ControlDark, margin, currentY - 2, usableWidth - margin * 2 - 1, 1);
            currentY += 2;

            BeginHorizontalSeperator(); //this control doesn't get aligned to it
        }
        #endregion
    }

    /// <summary>
    /// A control for displaying object specific UI that an <see cref="IObjectUIContainer"/> provides
    /// </summary>
    public interface IObjectUIControlWithMultipleSupport : IObjectUIControlBase
    {
        Multiple<float> NumberInput(Multiple<float> number, string name,
    float increment = 1f, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false);

        MultipleVector3 Vector3Input(MultipleVector3 vec, string name,
            float increment = 1f, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false, Vector3 multiResolveValue = new Vector3(), bool allowMixed = true);

        MultipleVector3 FullWidthVector3Input(MultipleVector3 vec, string name,
            float increment = 1f, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false, Vector3 multiResolveValue = new Vector3());

        Multiple<bool> CheckBox(string name, Multiple<bool> isChecked, bool multiResolveValue = false);
        Multiple<string> TextInput(Multiple<string> text, string name, string multiResolveValue = "");
        Multiple<string> FullWidthTextInput(Multiple<string> text, string name, string multiResolveValue = "");
        Multiple<object> ChoicePicker(string name, Multiple<object> value, IList values, object multiResolveValue = null);
        Multiple<string> DropDownTextInput(string name, Multiple<string> value, string[] dropDownItems, bool filterSuggestions = true, string multiResolveValue = "");
    }

    public interface IObjectUIControlBase
    {
        bool Button(string name);
        int DoubleButton(string name, string name2);
        int TripleButton(string name, string name2, string name3);
        int QuadripleButton(string name, string name2, string name3, string name4);
        bool Link(string name);
        void PlainText(string text);
        void Heading(string text);
        void Spacing(int amount);
        void VerticalSeperator();
        void SetTooltip(string text);
    }

    public interface IObjectUIControl : IObjectUIControlBase
    {
        float NumberInput(float number, string name,
    float increment = 1f, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false);

        Vector3 Vector3Input(Vector3 vec, string name,
            float increment = 1f, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false);

        Vector3 FullWidthVector3Input(Vector3 vec, string name,
            float increment = 1f, int incrementDragDivider = 8, float min = float.MinValue, float max = float.MaxValue, bool wrapAround = false);

        bool CheckBox(string name, bool isChecked);
        string TextInput(string text, string name);
        string FullWidthTextInput(string text, string name);
        object ChoicePicker(string name, object value, IList values);
        string DropDownTextInput(string name, string text, string[] dropDownItems, bool filterSuggestions = true);
    }


    /// <summary>
    /// A provider for object specific UI for example properties
    /// </summary>
    public interface IObjectUIContainer
    {
        void DoUI(IObjectUIControl control);
        void UpdateProperties();

        void OnValueChangeStart();
        void OnValueChanged();
        void OnValueSet();
    }
}
using System;
using System.Diagnostics;
using System.Windows.Forms;
using GUI.Types.Renderer;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace GUI.Types.ParticleRenderer
{
    internal class GLRenderControl
    {
        public class RenderEventArgs
        {
            public float FrameTime { get; set; }
            public Camera Camera { get; set; }
        }

        public Camera Camera { get; set; }

        public Control Control { get; }

        private Label fpsLabel;
        private GLControl glControl;

        public event EventHandler<RenderEventArgs> Paint;
        public event EventHandler Load;

        private readonly Stopwatch stopwatch;

        public GLRenderControl()
        {
            Camera = new Camera();
            Control = InitializeControl();

            stopwatch = new Stopwatch();
        }

        protected virtual Control InitializeControl()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
            };

            fpsLabel = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                AutoSize = true,
                Dock = DockStyle.Top,
            };
            panel.Controls.Add(fpsLabel);

#if DEBUG
            glControl = new GLControl(new GraphicsMode(32, 24, 0, 8), 3, 3, GraphicsContextFlags.Debug);
#else
            glControl = new GLControl(new GraphicsMode(32, 24, 0, 8), 3, 3, GraphicsContextFlags.Default);
#endif
            glControl.Dock = DockStyle.Fill;
            glControl.AutoSize = true;
            glControl.Load += OnLoad;
            glControl.Paint += OnPaint;
            glControl.Resize += OnResize;
            glControl.MouseEnter += (_, __) => Camera.MouseOverRenderArea = true;
            glControl.MouseLeave += (_, __) => Camera.MouseOverRenderArea = false;
            glControl.GotFocus += OnGotFocus;

            panel.Controls.Add(glControl);
            return panel;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            glControl.MakeCurrent();

            GL.Enable(EnableCap.Blend);

            stopwatch.Start();

            Load?.Invoke(this, e);

            HandleResize();
            Draw();
        }

        private void OnPaint(object sender, EventArgs e)
        {
            Draw();
        }

        private void Draw()
        {
            var frameTime = stopwatch.ElapsedMilliseconds / 1000f;
            stopwatch.Restart();

            Camera.Tick(frameTime);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Paint?.Invoke(this, new RenderEventArgs { FrameTime = frameTime, Camera = Camera });

            glControl.SwapBuffers();
            glControl.Invalidate();
        }

        private void OnResize(object sender, EventArgs e)
        {
            HandleResize();
            Draw();
        }

        private void HandleResize()
        {
            Camera.SetViewportSize(Control.Width, Control.Height);
        }

        private void OnGotFocus(object sender, EventArgs e)
        {
            glControl.MakeCurrent();
            Draw();
        }
    }
}

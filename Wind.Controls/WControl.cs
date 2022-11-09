/**** BEGIN LICENSE BLOCK ****

BSD 3-Clause License

Copyright (c) 2022, the wind.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

**** END LICENCE BLOCK ****/

using System.Drawing;
using System.Windows.Forms;
#if WIND_GL
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Wind.Log;
#endif

namespace Wind.Controls
{
    /// <summary>My custom controls bridge; renders everything and nothing: ControlStyles.AllPaintingInWmPaint + !WM_ERASEBKGND.</summary>
    public class WControl : Control //TODO there is no System.Windows.Forms, there's Open GL
    {
        //> grep -r "\.NET bug #1"
        public static int SanitizeSize(int size) { return size < 0 ? 0 : size; }

        public WControl()
        {
            this.SetStyle (ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.DoubleBuffer, true);
            this.ResizeRedraw = true;
            this.DoubleBuffered = true;
        }

        protected override void WndProc(ref Message wm)
        {
            try
            {
                const int WM_NULL = 0x0000; // "WinUser.h"
                const int WM_ERASEBKGND = 0x0014; // "WinUser.h"
                if (WM_ERASEBKGND == wm.Msg) // No erase backgnd.
                    wm.Msg = WM_NULL;
            }
            finally
            {
                base.WndProc (ref wm); // Never stand on the way of a WndProc and its prey ;).
            }
        }

        const int DEFAULT_SIZE = 8;

        /// <summary>When a control can't render itself for one reason or another, it shall return "true" here.</summary>
        protected virtual bool NoRender { get { return RenderArea.Width <= 0 || RenderArea.Height <= 0; } }

        /// <summary>WControl.RenderArea - the area your control renders to.</summary>
        protected Rectangle RenderArea { get; set; }

        /// <summary>Default rendering: this gets rendered when the control is not ready to render.</summary>
        protected virtual void DefaultRender(Graphics gc)
        {
            gc.FillRectangle (Brushes.Navy, RenderArea);
            int x = RenderArea.Width - 1, y = RenderArea.Height - 1;
            gc.DrawBezier (Pens.LimeGreen, 0, 0, 0, y / 2, x, y / 2, x, y);
            for (int i = 0; i < 7; i++)
            {
                gc.TranslateTransform (RenderArea.Width / 2, RenderArea.Height / 2);
                gc.RotateTransform (23);
                gc.TranslateTransform (-RenderArea.Width / 2, -RenderArea.Height / 2);
                gc.DrawBezier (Pens.LimeGreen, 0, 0, 0, y / 2, x, y / 2, x, y);
            }
        }

        // You render here.
        protected virtual void Render(Graphics gc) { }

        protected override Size DefaultSize { get { return new Size (DEFAULT_SIZE, DEFAULT_SIZE); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (NoRender) DefaultRender (e.Graphics); else Render (e.Graphics);
        }
    }// public class WControl

    public static partial class WExtensions
    {
        public static bool LessThanOrEqual(this Rectangle a, Rectangle b)
        {
            return a.Width <= b.Width && a.Height <= b.Height;
        }
    }

#if WIND_GL
    #region Open GL
    // Open the graphics library; indirection layers be gone.
    // using HWND = System.IntPtr;
    // using HDC = System.IntPtr;
    // using DWORD = System.UInt32;
    // using BOOL = System.Int32;
    public class WindGL
    {
        internal const String USER32 = "user32.dll";
        internal const String GDI32 = "gdi32.dll";
        internal const String OPENGL32 = "opengl32.dll";

        #region Include/WinUser.h
        [DllImport (USER32, CharSet = CharSet.Auto, EntryPoint = "GetDC", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr WGetDC(HandleRef hWnd);
        [DllImport (USER32, CharSet = CharSet.Auto, EntryPoint = "ReleaseDC", ExactSpelling = true, SetLastError = true)]
        private static extern int WReleaseDC(HandleRef hWnd, HandleRef hDC);
        #endregion

        #region Include/WinGDI.h
        // shouldn't need HandleRef because its used in the form constructor
        [DllImport (GDI32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        static extern int ChoosePixelFormat([In] IntPtr hdc, [In] ref PIXELFORMATDESCRIPTOR ppfd);
        [DllImport (GDI32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        static extern int DescribePixelFormat([In] IntPtr hdc, [In] int iPixelFormat, [In] UInt32 nBytes, [Out] out PIXELFORMATDESCRIPTOR ppfd);
        [DllImport (GDI32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        static extern Int32 SetPixelFormat([In] IntPtr hdc, [In] int format, [In] ref PIXELFORMATDESCRIPTOR ppfd);
        const uint PFD_DRAW_TO_WINDOW = 0x00000004u;
        const uint PFD_SUPPORT_OPENGL = 0x00000020u;
        const uint PFD_DOUBLEBUFFER = 0x00000001u;
        // "PFD_SWAP_EXCHANGE is a hint only and might not be provided by a driver."
        const uint PFD_SWAP_EXCHANGE = 0x00000200u; // a.k.a. double-buffered
        const byte PFD_TYPE_RGBA = 0;
        const byte PFD_MAIN_PLANE = 0;

        [StructLayout (LayoutKind.Sequential)]
        internal struct PIXELFORMATDESCRIPTOR
        {
            internal static PIXELFORMATDESCRIPTOR Create()
            {
                PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR ();
                pfd.nSize = (ushort)Marshal.SizeOf (typeof (PIXELFORMATDESCRIPTOR));
                pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
                pfd.iPixelType = PFD_TYPE_RGBA;
                // PIXELFORMATDESCRIPTOR docs statement:
                // "Specifies the number of color bitplanes in each color buffer. For RGBA pixel types, it is the size of the
                //  color buffer, excluding the alpha bitplanes."
                // Yet DescribePixelFormat sets cColorBits to 32 ?!
                pfd.cColorBits = 24;
                pfd.cAlphaBits = 8;
                pfd.cAccumBits = 32; // DescribePixelFormat sets it to 64
                pfd.cDepthBits = 24; // 31 - won't work :)
                pfd.cStencilBits = 8;// 1 ?
                pfd.iLayerType = PFD_MAIN_PLANE;
                return pfd;
            }
            [MarshalAs (UnmanagedType.U2)]
            public UInt16 nSize;
            [MarshalAs (UnmanagedType.U2)]
            public UInt16 nVersion;
            [MarshalAs (UnmanagedType.U4)]
            public UInt32 dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits;
            public byte cRedShift;
            public byte cGreenBits;
            public byte cGreenShift;
            public byte cBlueBits;
            public byte cBlueShift;
            public byte cAlphaBits;
            public byte cAlphaShift;
            public byte cAccumBits;
            public byte cAccumRedBits;
            public byte cAccumGreenBits;
            public byte cAccumBlueBits;
            public byte cAccumAlphaBits;
            public byte cDepthBits;
            public byte cStencilBits;
            public byte cAuxBuffers;
            public byte iLayerType;
            public byte bReserved;
            [MarshalAs (UnmanagedType.U4)]
            public UInt32 dwLayerMask;
            [MarshalAs (UnmanagedType.U4)]
            public UInt32 dwVisibleMask;
            [MarshalAs (UnmanagedType.U4)]
            public UInt32 dwDamageMask;
        }

        [DllImport (OPENGL32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr wglCreateContext([In] IntPtr hdc);
        [DllImport (OPENGL32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        static extern int wglMakeCurrent([In] IntPtr hdc, [In] IntPtr hglrc);
        [DllImport (OPENGL32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        static extern int wglDeleteContext([In] IntPtr hglrc);
        [DllImport (OPENGL32, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr wglGetProcAddress(string name);
        [DllImport (GDI32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        static extern int SwapBuffers([In] IntPtr hdc);
        #endregion

        #region Include/gl/GL.h
        // GLubyte = byte
        // GLenum = UInt32
        // GLclampf = float
        // GLbitfield = UInt32
        const UInt32 GL_VENDOR = 0x1F00u;
        const UInt32 GL_RENDERER = 0x1F01u;
        const UInt32 GL_VERSION = 0x1F02u;
        const UInt32 GL_EXTENSIONS = 0x1F03u;
        const UInt32 GL_COLOR_BUFFER_BIT = 0x00004000u;
        [DllImport (OPENGL32, EntryPoint = "glGetString", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = false)]
        static extern IntPtr NglGetString([In] UInt32 name);
        static string glGetString(UInt32 name)
        {
            var t = NglGetString (name);
            return IntPtr.Zero == t ? "" : Marshal.PtrToStringAnsi (t);
        }
        [DllImport (OPENGL32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = false)]
        static extern void glClearColor([In] float red, [In] float green, [In] float blue, [In] float alpha);
        [DllImport (OPENGL32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = false)]
        static extern void glClear([In] UInt32 mask);
        [DllImport (OPENGL32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = false)]
        static extern void glViewport([In] int x, [In] int y, [In] int w, [In] int h);
        #endregion

        public class WindGLForm : Form
        {
            protected override void WndProc(ref Message messg)
            {
                const int WM_NULL = 0x0000; // "WinUser.h"
                const int WM_ERASEBKGND = 0x0014; // "WinUser.h"
                if (WM_ERASEBKGND == messg.Msg) // no erase backgnd
                    messg.Msg = WM_NULL; //TODO hitting here although ControlStyles.AllPaintingInWmPaint
                base.WndProc (ref messg); // never stand on the way
            }
            protected override void OnResize(EventArgs e)
            {
                base.OnResize (e);
                glViewport (0, 0, this.ClientRectangle.Width, this.ClientRectangle.Height);
            }
            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    const int CS_OWNDC = 0x0020; // Include/WinUser.h
                    cp.ClassStyle |= CS_OWNDC; // https://www.khronos.org/opengl/wiki/Creating_an_OpenGL_Context_(WGL)
                    return cp;
                }
            }
            IntPtr _hglrc;
            IntPtr _hdc;

            [UnmanagedFunctionPointer (System.Runtime.InteropServices.CallingConvention.StdCall, SetLastError = true)]
            delegate IntPtr wglGetExtensionsStringARB(IntPtr hdc);

            public WindGLForm()
                : base ()
            {
                _hglrc = IntPtr.Zero;
                this.SetStyle (ControlStyles.UserPaint | ControlStyles.Opaque
                                | ControlStyles.AllPaintingInWmPaint
                                | ControlStyles.OptimizedDoubleBuffer, true);
                this.ShowInTaskbar = false;
                this.Width = 640;
                this.Height = 480;
                // var gdc = Graphics.FromHwnd (this.Handle);
                // _hdc = gdc.GetHdc ();
                _hdc = WGetDC (new HandleRef (this, this.Handle)); // not the same
                try
                {
                    var fmt = PIXELFORMATDESCRIPTOR.Create ();
                    fmt.cColorBits = 32; // 24 works, but DescribePixelFormat returns 32, so this stays until the mystery is solved
                    int pfmt = ChoosePixelFormat (_hdc, ref fmt);
                    if (0 == pfmt)
                        throw new WException ("ChoosePixelFormat failed: " + new Win32Exception ().Message);
                    var actual_fmt = PIXELFORMATDESCRIPTOR.Create ();
                    var max_fmt = DescribePixelFormat (_hdc, pfmt, actual_fmt.nSize, out actual_fmt);
                    if (0 == max_fmt)
                        throw new WException ("DescribePixelFormat failed: " + new Win32Exception ().Message);
                    var flags_101 = Convert.ToString ((int)actual_fmt.dwFlags, 2);
                    //TODO perhaps I should for i in {0..max_fmt}
                    if ((actual_fmt.dwFlags & fmt.dwFlags) != fmt.dwFlags
                        || actual_fmt.iPixelType != fmt.iPixelType
                        || actual_fmt.cColorBits != fmt.cColorBits
                        || actual_fmt.cAlphaBits != fmt.cAlphaBits
                        || actual_fmt.cStencilBits <= 0 || actual_fmt.cAccumBits <= 0)
                        throw new WException ("Open GL init failed");
                    if (0 == SetPixelFormat (_hdc, pfmt, ref actual_fmt))
                        throw new WException ("SetPixelFormat failed: " + new Win32Exception ().Message);
                    _hglrc = wglCreateContext (_hdc);
                    if (IntPtr.Zero == _hglrc)
                        throw new WException ("wglCreateContext failed: " + new Win32Exception ().Message);
                    if (0 == wglMakeCurrent (_hdc, _hglrc))
                        WLog.Err ("wglMakeCurrent failed: " + new Win32Exception ().Message);
                    WLog.Info ("GL_VERSION: " + glGetString (GL_VERSION));
                    WLog.Info ("GL_VENDOR: " + glGetString (GL_VENDOR));
                    WLog.Info ("GL_RENDERER: " + glGetString (GL_RENDERER));
                    // WLog.Info ("GL_EXTENSIONS: " + glGetString (GL_EXTENSIONS));
                    this.Text = glGetString (GL_VERSION);// glGetString (GL_VERSION);

                    var q0 = wglGetProcAddress ("wglGetExtensionsStringARB");
                    if (IntPtr.Zero == q0) WLog.Err ("wglGetExtensionsStringARB not available: " + new Win32Exception ().Message);
                    var q0_call = (wglGetExtensionsStringARB)Marshal.GetDelegateForFunctionPointer (q0, typeof (wglGetExtensionsStringARB));
                    var q0_tptr = q0_call (_hdc);
                    if (IntPtr.Zero != q0_tptr)
                        ;// WLog.Info ("wglGetExtensionsStringARB: " + Marshal.PtrToStringAnsi (q0_tptr));
                    else
                        throw new WException ("wglGetExtensionsStringARB failed: " + new Win32Exception ().Message);
                    var q1 = wglGetProcAddress ("wglChoosePixelFormatARB");
                    if (IntPtr.Zero == q1) WLog.Err ("wglChoosePixelFormatARB not available: " + new Win32Exception ().Message);
                    var q2 = wglGetProcAddress ("wglCreateContextAttribsARB");
                    if (IntPtr.Zero == q2) WLog.Err ("wglCreateContextAttribsARB not available: " + new Win32Exception ().Message);

                    System.Windows.Forms.Timer t = new System.Windows.Forms.Timer ();
                    float t1 = 0;
                    t.Tick += (a, b) =>
                        {
                            glClearColor (
                                (float)Math.Sin (t1) * 0.5f + 0.5f,
                                (float)Math.Cos (t1) * 0.5f + 0.5f,
                                (float)Math.Sin (6.28f - t1) * 0.5f + 0.5f, 1.0f);
                            t1 += 0.05f;
                            if (t1 > 6.28f) t1 = 0;
                            // Invalidate();
                            glClear (GL_COLOR_BUFFER_BIT);
                            SwapBuffers (_hdc);
                        };
                    t.Interval = 1000 / 50;
                    t.Enabled = true;
                }
                finally
                {
                    // if (1 != WReleaseDC (new HandleRef (this, this.Handle), new HandleRef (this, _hdc)))
                    //     throw new WException ("ReleaseDC failed" + new Win32Exception ().Message);
                }
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (0 == wglMakeCurrent (IntPtr.Zero, IntPtr.Zero))
                        WLog.Err ("wglMakeCurrent(0, 0) failed: " + new Win32Exception ().Message);
                    if (IntPtr.Zero != _hglrc)
                        if (0 == wglDeleteContext (_hglrc))
                            WLog.Err ("wglDeleteContext failed: " + new Win32Exception ().Message);
                    if (IntPtr.Zero != _hdc)//TODO why is it already released by the dock content
                        if (1 != WReleaseDC (new HandleRef (this, this.Handle), new HandleRef (this, _hdc)))
                            WLog.Err ("ReleaseDC failed: " + new Win32Exception ().Message);
                }
                base.Dispose (disposing);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                // glClear (GL_COLOR_BUFFER_BIT);
                // if (0 == SwapBuffers (_hdc))
                //     WLog.Err ("SwapBuffers failed: " + new Win32Exception ().Message);
            }
        }
    }// public class WindGL
    #endregion
#endif
}

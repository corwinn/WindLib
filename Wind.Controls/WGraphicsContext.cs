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

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Wind.Controls
{
    // Bridge away the actual graphics-related utilities, thus allowing others, but System.Drawing.Graphics to render about:
    // OpenGL_GraphicsContext comes in mind.
    public class WGraphicsContext : IDisposable
    {
        /*private class CacheEntry
        {
            public CacheEntry(string t, Font f) { _text = t; _font = f; }
            private string _text;
            private Font _font;
            public override bool Equals(object obj)
            {
                var q = (CacheEntry)obj;
                return _text.Equals (q._text) && _font.Equals (q._font);
            }
            public override int GetHashCode() { return _font.GetHashCode () * _text.GetHashCode (); }
        }
        private static Dictionary<CacheEntry, Bitmap> naive_cache = new Dictionary<CacheEntry, Bitmap> ();*/

        public Size MeasureText(string text, Font font)
        {
            return TextRenderer.MeasureText (text, SystemFonts.DefaultFont);
            /*var ts = _gc.MeasureString (_text, SystemFonts.DefaultFont);
            var text_w = (int)Math.Round (ts.Width + .5d); the same result
            var text_h = (int)Math.Round (ts.Height + .5d);
            return new Size((int)text_w, (int)text_h); */
        }

        private Graphics _gc = null;
        private static WGraphicsContext _tmp_gc = new WGraphicsContext ();
        private WGraphicsContext SetSysGc(Graphics gc) { _gc = gc; return this; }
        public static WGraphicsContext FromGraphics(Graphics gc) { return _tmp_gc.SetSysGc (gc); }
        void IDisposable.Dispose() { _gc = null; }

        public void FillRectangle(Brush brush, int p1, int p2, int p3, int p4)
        {
            _gc.FillRectangle (brush, p1, p2, p3, p4);
        }

        public void DrawString(string _text, Font font, Brush brush, RectangleF rectangleF, StringFormat _text_fmt)
        {
            _gc.DrawString (_text, font, brush, rectangleF, _text_fmt);
        }

        public void DrawString(string text, Font font, Brush brush, int tx, int ty)
        {
            /*var key = new CacheEntry (text, font);
            Bitmap bmp = null;
            if (!naive_cache.TryGetValue (key, out bmp))
            {
                var sz = this.MeasureText (text, font);
                bmp = new Bitmap (sz.Width + tx, sz.Height + ty);
                var bmp_gc = Graphics.FromImage (bmp);
                bmp_gc.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);
                bmp_gc.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                bmp_gc.DrawString (text, font, Brushes.Black, tx, ty);
                bmp.MakeTransparent(Color.White);
                naive_cache[key] = bmp;
            }
            _gc.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            _gc.DrawImageUnscaled (bmp, 0, 0);*/
            // this is slow; lets try a glyph cache ... Graphics lacks alpha at font smoothing; just
            //TODO "freetype2"
            _gc.DrawString (text, font, brush, tx, ty);
        }
    }// public class GraphicsContext
}

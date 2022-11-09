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

namespace Wind.Controls
{
    /// <summary>Default styles.</summary>
    internal static class WStyle
    {
        public static Brush FillBack { get { return SystemBrushes.Window; } }
        public static Brush FillBtnInactive { get { return SystemBrushes.InactiveCaption; } }
        public static Pen FontPen { get { return SystemPens.ControlText; } }
        internal static class TableView
        {
            public static Brush FillBack { get { return SystemBrushes.Window; } }
            public static Pen FontPen { get { return SystemPens.ControlText; } }
            public static Color FontColor { get { return SystemColors.ActiveCaptionText; } }
            public static Font HeaderFont { get { return SystemFonts.CaptionFont; } }
            public static Font TextFont { get { return SystemFonts.DefaultFont; } }
            public static int Padding { get { return 4; } }

            private static Pen _grid_pen;
            public static Pen GridPen
            {
                get
                {
                    if (null == _grid_pen)
                    {
                        _grid_pen = new Pen (Color.Indigo);
                        _grid_pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    }
                    return _grid_pen;
                }
            }
        }// internal static class WStyle.TableView
    }// internal static class WStyle
}

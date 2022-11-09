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

namespace Wind.Controls
{
    // Running in a coordinate system where "size" and "position" are "int" and >= 0, and <= 1 << 15.
    // Enforces the rendering constraints - simplifies the renderers code.
    public static partial class WExtensions
    {
        const int MAX = 1 << 20; // should be enough for many bitmaps and screen resolutions in the next X years
        const int MIN = 0;
        //const int SKIP_STACK_FRAMES = 2; // us and WDebug
        const string MAX_ERR = "overflow";
        const string MIN_ERR = "underflow";
        const string NEGATIVE_ERR = "can't be < 0";
        const string INVALID_EXCLUSIVE_RANGE = "invalid exclusive range";
        const string OUT_OF_RANGE = "out of range: {0}{1};{2}{3}";

        // If "size" is funny, throws ArgumentException with the available stack trace info.
        // "size" gets funny when I delude myself I'm too good at math, or when I play compiler with integer under/over flows, etc.
        // Eliminates the need of "if (bla-bla-bla) throw ArgumentException("namea.nameb.... foo, bar, etc.")" at 10000 places;
        // just put "variable.PreventStrangeRendering()" instead.
        public static void PreventStrangeRendering(this int value)
        {
            if (value < MIN) throw new WArgumentException (MIN_ERR); // your computations are wrong
            if (value > MAX) throw new WArgumentException (MAX_ERR); // your computations are wrong
        }
        public static void PreventStrangeRendering(this long value)
        {
            if (value < MIN) throw new WArgumentException (MIN_ERR); // your computations are wrong
            if (value > MAX) throw new WArgumentException (MAX_ERR); // your computations are wrong
        }

        public static void PreventStrangeRenderingOffset(this int value)
        {
            if (value < -MAX) throw new WArgumentException (MIN_ERR); // your computations are wrong
            if (value > MAX) throw new WArgumentException (MAX_ERR); // your computations are wrong
        }

        public static void PositiveOrZero(this int value)
        {
            if (value < 0) throw new WArgumentException (NEGATIVE_ERR);
        }
        public static void PositiveOrZero(this long value)
        {
            if (value < 0) throw new WArgumentException (NEGATIVE_ERR);
        }

        // Throws when a >= b; throws when value is not in (a;b).
        public static void InExclusiveRange(this int value, int a, int b)
        {
            if (a >= b) throw new WArgumentException (INVALID_EXCLUSIVE_RANGE);
            if (value <= a || value >= b)
                throw new WArgumentException (string.Format (OUT_OF_RANGE, '[', a, b, ')'));
        }
        public static void InExclusiveRange(this long value, long a, long b)
        {
            if (a >= b) throw new WArgumentException (INVALID_EXCLUSIVE_RANGE);
            if (value <= a || value >= b)
                throw new WArgumentException (string.Format (OUT_OF_RANGE, '[', a, b, ')'));
        }
    }// public static partial class WExtensions
}

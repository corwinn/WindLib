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

namespace Wind
{
    public class WMath
    {
        // public static int LinearProjection(TYPE_FROM x, TYPE_FROM from, int to);
        //TODO rename me; usually these functions are called world_to_screen, screen_to_world; "Open GL" uses projection matrices;
        //     in this case the projection matrix is: [0] = (to/from); // 1D
        //     (the projected value lies on the hypotenuse of a right-angled triangle ( (x1,y1) (x2,y2) (x3,y3) ):
        //      (from,value) (to,value) (to,LinearProjection(value,from,to)) );
        //     All and all, it converts a value from one coordinate system into another one, preserving the ratio of said value.

        // Project a value from coordinate system axis "from" into coordinate system axis "to":
        //  - given "value" is  1, "from":  2, "to": 30, this function will return 15;
        //  - given "value" is 15, "from": 30, "to":  2, this function will return  1;
        // "to" and "from" are > 0; "x" is [0;from) for "from" > 0, or 0; the result is [0;to) for to > 0, 0 otherwise.
        // For "to" < 0 or "x" < 0, it will return a negative result.
        // For "to" == 0 or "from" <= 0 it will return 0.
        public static int LinearProjection(int value, int from, int to)//TODONT rename me to "lerp" - I'm not "lerp()"
        {
            return from > 0 ? (int)Math.Round (to * value / (double)from) : 0;
        }
        // For the StreamEditor: from long to int used by ComputeMidOffset() and ComputeMidSize().
        public static int LinearProjection(long value, long from, int to) { return from > 0 ? (int)Math.Round (to * value / (decimal)from) : 0; }
        // For the StreamEditor: from int to long; used by ScrollDistinctPixels().
        public static long LinearProjection(int value, int from, long to) { return from > 0 ? (long)Math.Round (to * value / (double)from) : 0; }
    }// class WMath
}

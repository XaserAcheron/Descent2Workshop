﻿/*
    Copyright (c) 2019 SaladBadger

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System;

namespace LibDescent.Data
{
    public enum SegSide
    {
        Left,
        Up,
        Right,
        Down,
        Back,
        Front,
    }

    public partial class Segment
    {
        public const int MaxSegmentSides = 6;
        public const int MaxSegmentVerts = 8;
        public static readonly int[,] SideVerts = { { 7, 6, 2, 3 }, { 0, 4, 7, 3 }, { 0, 1, 5, 4 }, { 2, 6, 5, 1 }, { 4, 5, 6, 7 }, { 3, 2, 1, 0 } };

        public byte special;
        public byte value;
        public byte flags;
        public int staticLight;

        public Side[] Sides { get; }
        public FixVector[] Vertices { get; }
        public MatCenter MatCenter { get; set; }

        #region Read-only convenience properties
        public FixVector Center { get; }
        #endregion

        public Segment(uint numSides = MaxSegmentSides, uint numVertices = MaxSegmentVerts)
        {
            Sides = new Side[numSides];
            Vertices = new FixVector[numVertices];
        }

        public Side GetSide(SegSide side) => Sides[(int)side];
    }
}

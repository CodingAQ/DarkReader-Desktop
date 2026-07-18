// This file is part of DarkReader.
// Copyright (C) 2026 DarkReader Contributors.
//
// Derived from NegativeScreen by mlaily (https://github.com/mlaily/NegativeScreen),
// originally licensed under GPL-3.0.
//
// DarkReader is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License version 3 as published
// by the Free Software Foundation.
//
// DarkReader is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with DarkReader. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DarkReader
{
    public static class BuiltinMatrices
    {
        public static float[,] Identity { get; }
        public static float[,] SimpleInversion { get; }
        public static float[,] SmartInversion1 { get; }
        public static float[,] SmartInversion2 { get; }
        public static float[,] SmartInversion3 { get; }
        public static float[,] SmartInversion4 { get; }
        public static float[,] SmartInversion5 { get; }
        public static float[,] Grayscale { get; }

        static BuiltinMatrices()
        {
            Identity = new float[,] {
                {  1.0f,  0.0f,  0.0f,  0.0f,  0.0f },
                {  0.0f,  1.0f,  0.0f,  0.0f,  0.0f },
                {  0.0f,  0.0f,  1.0f,  0.0f,  0.0f },
                {  0.0f,  0.0f,  0.0f,  1.0f,  0.0f },
                {  0.0f,  0.0f,  0.0f,  0.0f,  1.0f }
            };

            SimpleInversion = new float[,] {
                { -1.0f,  0.0f,  0.0f,  0.0f,  0.0f },
                {  0.0f, -1.0f,  0.0f,  0.0f,  0.0f },
                {  0.0f,  0.0f, -1.0f,  0.0f,  0.0f },
                {  0.0f,  0.0f,  0.0f,  1.0f,  0.0f },
                {  1.0f,  1.0f,  1.0f,  0.0f,  1.0f }
            };

            // NegativeHueShift180 — theoretical optimal transformation
            SmartInversion1 = Multiply(SimpleInversion, new float[,] {
                { -0.3333333f,  0.6666667f,  0.6666667f, 0.0f, 0.0f },
                {  0.6666667f, -0.3333333f,  0.6666667f, 0.0f, 0.0f },
                {  0.6666667f,  0.6666667f, -0.3333333f, 0.0f, 0.0f },
                {  0.0f,              0.0f,        0.0f, 1.0f, 0.0f },
                {  0.0f,              0.0f,        0.0f, 0.0f, 1.0f }
            });

            // NegativeHueShift180Variation1 — most simple working method
            SmartInversion2 = new float[,] {
                {  1.0f, -1.0f, -1.0f, 0.0f, 0.0f },
                { -1.0f,  1.0f, -1.0f, 0.0f, 0.0f },
                { -1.0f, -1.0f,  1.0f, 0.0f, 0.0f },
                {  0.0f,  0.0f,  0.0f, 1.0f, 0.0f },
                {  1.0f,  1.0f,  1.0f, 0.0f, 1.0f }
            };

            // NegativeHueShift180Variation2 — overall desaturated, relaxing
            SmartInversion3 = new float[,] {
                {  0.39f, -0.62f, -0.62f, 0.0f, 0.0f },
                { -1.21f, -0.22f, -1.22f, 0.0f, 0.0f },
                { -0.16f, -0.16f,  0.84f, 0.0f, 0.0f },
                {   0.0f,   0.0f,   0.0f, 1.0f, 0.0f },
                {   1.0f,   1.0f,   1.0f, 0.0f, 1.0f }
            };

            // NegativeHueShift180Variation3 — high saturation, quite readable
            SmartInversion4 = new float[,] {
                {     1.089508f,   -0.9326327f, -0.932633042f,  0.0f,  0.0f },
                {  -1.81771779f,    0.1683074f,  -1.84169245f,  0.0f,  0.0f },
                { -0.244589478f, -0.247815639f,    1.7621845f,  0.0f,  0.0f },
                {          0.0f,          0.0f,          0.0f,  1.0f,  0.0f },
                {          1.0f,          1.0f,          1.0f,  0.0f,  1.0f }
            };

            // NegativeHueShift180Variation4 — not so readable, good colors
            SmartInversion5 = new float[,] {
                {  0.50f, -0.78f, -0.78f, 0.0f, 0.0f },
                { -0.56f,  0.72f, -0.56f, 0.0f, 0.0f },
                { -0.94f, -0.94f,  0.34f, 0.0f, 0.0f },
                {   0.0f,   0.0f,   0.0f, 1.0f, 0.0f },
                {   1.0f,   1.0f,   1.0f, 0.0f, 1.0f }
            };

            // Grayscale — luminance-based, all pixels become gray
            Grayscale = new float[,] {
                { 0.299f, 0.299f, 0.299f, 0.0f, 0.0f },
                { 0.587f, 0.587f, 0.587f, 0.0f, 0.0f },
                { 0.114f, 0.114f, 0.114f, 0.0f, 0.0f },
                {   0.0f,   0.0f,   0.0f, 1.0f, 0.0f },
                {   0.0f,   0.0f,   0.0f, 0.0f, 1.0f }
            };
        }

        public static float[,] Multiply(float[,] a, float[,] b)
        {
            if (a.GetLength(1) != b.GetLength(0))
                throw new ArgumentException("Matrix dimensions don't match for multiplication.");

            float[,] c = new float[a.GetLength(0), b.GetLength(1)];
            for (int i = 0; i < c.GetLength(0); i++)
                for (int j = 0; j < c.GetLength(1); j++)
                    for (int k = 0; k < a.GetLength(1); k++)
                        c[i, j] += a[i, k] * b[k, j];
            return c;
        }

        public static List<float[,]> Interpolate(float[,] from, float[,] to, int steps = 10)
        {
            var result = new List<float[,]>(steps);
            for (int s = 1; s <= steps; s++)
            {
                float[,] m = new float[5, 5];
                for (int x = 0; x < 5; x++)
                    for (int y = 0; y < 5; y++)
                        m[x, y] = from[x, y] + s * (to[x, y] - from[x, y]) / steps;
                result.Add(m);
            }
            return result;
        }

        public static void ApplyMatrix(float[,] matrix)
        {
            var effect = new ColorEffect(matrix);
            if (!NativeMethods.MagSetFullscreenColorEffect(ref effect))
            {
                var ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                throw new InvalidOperationException("Failed to set color effect. Another application may be using the Magnification API.", ex);
            }
        }

        public static void ApplyWithTransition(float[,] from, float[,] to, int durationMs = 150)
        {
            var steps = Interpolate(from, to, 10);
            int delayPerStep = durationMs / 10;
            foreach (var step in steps)
            {
                ApplyMatrix(step);
                System.Threading.Thread.Sleep(delayPerStep);
            }
        }
    }
}

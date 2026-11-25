using System.Collections.Generic;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>Axis-aligned grey-box walls with door gaps and lintels, shared by level builders.</summary>
    public static class GreyboxWalls
    {
        public struct Gap
        {
            public float Center, Width, Height;
            public Gap(float center, float width = 1.6f, float height = 2.3f) { Center = center; Width = width; Height = height; }
        }

        /// <summary>Wall running along Z at fixed X.</summary>
        public static void WallZ(Transform p, string name, float x, float z0, float z1, float height, float thickness, Material mat, params Gap[] gaps)
            => Build(p, name, z0, z1, height, mat, gaps, (a, b, y0, y1) =>
                GreyboxKit.Span(p, name, new Vector3(x - thickness / 2, y0, a), new Vector3(x + thickness / 2, y1, b), mat));

        /// <summary>Wall running along X at fixed Z.</summary>
        public static void WallX(Transform p, string name, float z, float x0, float x1, float height, float thickness, Material mat, params Gap[] gaps)
            => Build(p, name, x0, x1, height, mat, gaps, (a, b, y0, y1) =>
                GreyboxKit.Span(p, name, new Vector3(a, y0, z - thickness / 2), new Vector3(b, y1, z + thickness / 2), mat));

        delegate GameObject Segment(float from, float to, float y0, float y1);

        static void Build(Transform p, string name, float start, float end, float height, Material mat, Gap[] gaps, Segment seg)
        {
            var sorted = new List<Gap>(gaps ?? new Gap[0]);
            sorted.Sort((a, b) => a.Center.CompareTo(b.Center));
            float cursor = start;
            foreach (var g in sorted)
            {
                float g0 = g.Center - g.Width / 2, g1 = g.Center + g.Width / 2;
                if (g0 > cursor) seg(cursor, g0, 0f, height);
                if (g.Height < height) seg(g0, g1, g.Height, height); // lintel
                cursor = g1;
            }
            if (end > cursor) seg(cursor, end, 0f, height);
        }
    }
}

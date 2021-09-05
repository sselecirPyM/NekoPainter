using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoPainter.Util
{
    public class CubicBezierCurve
    {
        public CubicBezierCurve(float p1x, float p1y, float p2x, float p2y)
        {
            cx = 3.0f * p1x;
            bx = 3.0f * (p2x - p1x) - cx;
            ax = 1.0f - cx - bx;

            cy = 3.0f * p1y;
            by = 3.0f * (p2y - p1y) - cy;
            ay = 1.0f - cy - by;
        }

        public float SampleCurveX(float t)
        {
            return ((ax * t + bx) * t + cx) * t;
        }

        public float SampleCurveY(float t)
        {
            return ((ay * t + by) * t + cy) * t;
        }

        public float SampleCurveDerivativeX(float t)
        {
            return (3.0f * ax * t + 2.0f * bx) * t + cx;
        }

        public float SolveCurveX(float x, float epsilon)
        {
            float t0;
            float t1;
            float t2;
            float x2;
            float d2;
            int i;

            // 牛顿迭代法快速计算
            for (t2 = x, i = 0; i < 8; i++)
            {
                x2 = SampleCurveX(t2) - x;
                if (MathF.Abs(x2) < epsilon)
                    return t2;
                d2 = SampleCurveDerivativeX(t2);
                if (MathF.Abs(d2) < 1e-6)
                    break;
                t2 = t2 - x2 / d2;
            }

            // 二分法保证可靠性
            t0 = 0.0f;
            t1 = 1.0f;
            t2 = x;

            if (t2 < t0) return t0;
            if (t2 > t1) return t1;

            while (t0 < t1)
            {
                x2 = SampleCurveX(t2);
                if (MathF.Abs(x2 - x) < epsilon)
                    return t2;
                if (x > x2)
                    t0 = t2;
                else
                    t1 = t2;
                t2 = (t1 - t0) * 0.5f + t0;
            }

            return t2;
        }

        public float GetValue(float x)
        {
            return Solve(x, 1e-6f);
        }

        float Solve(float x, float epsilon)
        {
            return SampleCurveY(SolveCurveX(x, epsilon));
        }

        readonly float ax;
        readonly float bx;
        readonly float cx;
        readonly float ay;
        readonly float by;
        readonly float cy;
    }
}

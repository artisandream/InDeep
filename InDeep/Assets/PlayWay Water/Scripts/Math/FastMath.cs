using UnityEngine;

namespace PlayWay.Water
{
	/// <summary>
	/// A collection of math utilities for the water.
	/// </summary>
	public class FastMath
	{
		static private float PIx2 = 2.0f * Mathf.PI;
		static private float[] sines;
		static private float[] cosines;

		static FastMath()
		{
			sines = new float[2048];

			const float p = Mathf.PI * 2 / 2048;

			for(int i = 0; i < 2048; ++i)
				sines[i] = Mathf.Sin(i * p);

			cosines = new float[2048];

			for(int i = 0; i < 2048; ++i)
				cosines[i] = Mathf.Cos(i * p);
		}

		/// <summary>
		/// A bit faster sine with lower precision.
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		static public float Sin2048(float x)
		{
			int icx = ((int)(x * 325.949f) & 2047);

			return sines[icx];
		}

		/// <summary>
		/// A bit faster cosine with lower precision.
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		static public float Cos2048(float x)
		{
			int icx = ((int)(x * 325.949f) & 2047);

			return cosines[icx];
		}

		/// <summary>
		/// Noticeably faster that calling Mathf.Sin and Mathf.Cos, but has lower precision.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="s"></param>
		/// <param name="c"></param>
		static public void SinCos2048(float x, out float s, out float c)
		{
			int icx = ((int)(x * 325.949f) & 2047);

			s = sines[icx];
			c = cosines[icx];
		}

		/// <summary>
		/// Fast power of 2.
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		static public float Pow2(float x)
		{
			return x * x;
		}

		/// <summary>
		/// Fast power of 4.
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		static public float Pow4(float x)
		{
			float t = x * x;
			return t * t;
		}
		
		/// <summary>
		/// Projects target on a-b line.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		static public Vector2 ProjectOntoLine(Vector2 a, Vector2 b, Vector2 target)
		{
			Vector2 u = b - a;
			Vector2 v = target - a;

			return a + Vector2.Dot(u, v) * u / u.sqrMagnitude;
		}

		/// <summary>
		/// Computes distance from target to a-b line.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		static public float DistanceToLine(Vector3 a, Vector3 b, Vector3 target)
		{
			Vector3 u = b - a;
			Vector3 v = target - a;

			Vector3 p = a + Vector3.Dot(u, v) * u / u.sqrMagnitude;

			return Vector3.Distance(p, target);
		}

		/// <summary>
		/// Computes distance from target to a-b segment.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		static public float DistanceToSegment(Vector3 a, Vector3 b, Vector3 target)
		{
			Vector3 u = b - a;
			Vector3 v = target - a;

			Vector3 p = a + Vector3.Dot(u, v) * u / u.sqrMagnitude;

			if(Vector3.Dot((a - p).normalized, (b - p).normalized) < 0.0f)
				return Vector3.Distance(p, target);
			else
				return Mathf.Min(v.magnitude, Vector3.Distance(b, target));
		}

		/// <summary>
		/// Computes distance from target to a-b segment.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		static public float DistanceToSegment(Vector2 a, Vector2 b, Vector2 target)
		{
			Vector2 u = b - a;
			Vector2 v = target - a;

			Vector2 p = a + Vector2.Dot(u, v) * u / u.sqrMagnitude;

			if(Vector2.Dot((a - p).normalized, (b - p).normalized) < 0.0f)
				return Vector2.Distance(p, target);
			else
				return Mathf.Min(v.magnitude, Vector2.Distance(b, target));
		}

		/// <summary>
		/// Finds the closest point to target on a-b segment.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		static public Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 target)
		{
			Vector2 u = b - a;
			Vector2 v = target - a;

			Vector2 p = a + Vector2.Dot(u, v) * u / u.sqrMagnitude;

			if(Vector2.Dot((a - p).normalized, (b - p).normalized) < 0.0f)
				return p;
			else if(Vector2.Distance(a, target) < Vector2.Distance(b, target))
				return a;
			else
				return b;
		}

		/// <summary>
		/// Checks if target is inside a-b-c triangle.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		static public bool IsPointInsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 target)
		{
			float diffx = target.x - a.x;
			float diffy = target.y - a.y;
			bool ab = (b.x - a.x) * diffy - (b.y - a.y) * diffx > 0;

			if((c.x - a.x) * diffy - (c.y - a.y) * diffx > 0 == ab)
				return false;

			if((c.x - b.x) * (target.y - b.y) - (c.y - b.y) * (target.x - b.x) > 0 != ab)
				return false;

			return true;
		}

		/// <summary>
		/// Returns random number with gaussian distribution.
		/// </summary>
		/// <returns></returns>
		static public float Gauss01()
		{
			return Mathf.Sqrt(-2.0f * Mathf.Log(Random.Range(0.000001f, 1.0f))) * Mathf.Sin(PIx2 * Random.value);
		}

		/// <summary>
		/// Returns random number with gaussian distribution.
		/// </summary>
		/// <returns></returns>
		static public float Gauss01(float u1, float u2)
		{
			return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(PIx2 * u2);
		}
	}
}

//
// Fingers Gestures
// (c) 2015 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace DigitalRubyShared
{
	public class FingersScript : MonoBehaviour
	{
		[Tooltip("True to treat the mouse as a finger, false otherwise. Only the primary mouse button is used.")]
		public bool TreatMousePointerAsFinger = false;

		[Tooltip("Objects that should ignore gestures. By default Text UI objects ignore gestures, but you can add " +
		         "additional objects such as panels.")]
		public List<GameObject> PassThroughObjects;

		private const int mousePointerId = int.MaxValue - 2;

		private readonly List<GestureRecognizer> gestures = new List<GestureRecognizer>();
		private readonly List<GestureTouch> touchesBegan = new List<GestureTouch>();
		private readonly List<GestureTouch> touchesMoved = new List<GestureTouch>();
		private readonly List<GestureTouch> touchesEnded = new List<GestureTouch>();
		private float previousMouseX = float.MinValue;
		private float previousMouseY = float.MinValue;
		private readonly Dictionary<int, GameObject> gameObjectsForTouch = new Dictionary<int, GameObject>();
		private readonly List<RaycastResult> captureRaycastResults = new List<RaycastResult>();
		private readonly List<GestureTouch> filteredTouches = new List<GestureTouch>();

		private IEnumerator MainThreadCallback(float delay, System.Action action)
		{
			if (action != null)
			{
				System.Diagnostics.Stopwatch w = System.Diagnostics.Stopwatch.StartNew();
				while (w.Elapsed.TotalSeconds < delay)
				{
					yield return null;
				}
				action();
			}
		}

		private GameObject GameObjectForTouch(int pointerId, float x, float y)
		{
			if (EventSystem.current == null)
			{
				return null;
			}

			captureRaycastResults.Clear();
			PointerEventData p = new PointerEventData(EventSystem.current);
			p.position = new Vector2(x, y);
			p.clickCount = 1;
			p.dragging = false;
			EventSystem.current.RaycastAll(p, captureRaycastResults);

			if (captureRaycastResults.Count == 0)
			{
				return null;
			}

			foreach (RaycastResult r in captureRaycastResults)
			{
				if (r.gameObject != null && r.gameObject.GetComponent<Text>() == null &&
				    (PassThroughObjects == null || !PassThroughObjects.Contains(r.gameObject)))
				{
					return r.gameObject;
				}
			}

			return null;
		}

		private void GestureTouchFromTouch(ref Touch t, out GestureTouch g)
		{
			g = new GestureTouch(t.fingerId, t.position.x, t.position.y, t.position.x - t.deltaPosition.x, t.position.y - t.deltaPosition.y, 0.0f);
		}

		private void GestureTouchFromMouse(float mx, float my, out GestureTouch g)
		{
			float prevX, prevY;
			if (previousMouseX == float.MinValue)
			{
				prevX = mx;
				prevY = my;
			}
			else
			{
				prevX = previousMouseX;
				prevY = previousMouseY;
			}

			g = new GestureTouch(mousePointerId, mx, my, prevX, prevY, 0.0f);
		}

		private void ProcessTouch(ref Touch t)
		{
			GestureTouch g;
			GestureTouchFromTouch(ref t, out g);

			switch (t.phase)
			{
			case TouchPhase.Began:
				touchesBegan.Add(g);
				break;
				
			case TouchPhase.Moved:
			case TouchPhase.Stationary:
				touchesMoved.Add(g);
				break;

			case TouchPhase.Ended:
			case TouchPhase.Canceled:
				touchesEnded.Add(g);
				break;
			}
		}

		private void ProcessMouse()
		{
			if (Input.mousePresent && TreatMousePointerAsFinger)
			{
				float mx = Input.mousePosition.x;
				float my = Input.mousePosition.y;
				GestureTouch g;
				GestureTouchFromMouse(mx, my, out g);

				if (Input.GetMouseButtonDown(0))
				{
					touchesBegan.Add(g);
				}
				else if (Input.GetMouseButton(0))
				{
					touchesMoved.Add(g);
					previousMouseX = mx;
					previousMouseY = my;
				}
				else if (Input.GetMouseButtonUp(0))
				{
					previousMouseX = float.MinValue;
					previousMouseY = float.MinValue;
					touchesEnded.Add(g);
				}
			}
		}

		private void Awake()
		{
			DeviceInfo.PixelsPerInch = (int)Screen.dpi;
			if (DeviceInfo.PixelsPerInch > 0)
			{
				DeviceInfo.UnitMultiplier = DeviceInfo.PixelsPerInch;
			}
			else
			{
				// pick a sensible dpi since we don't know the actual DPI
				DeviceInfo.UnitMultiplier = DeviceInfo.PixelsPerInch = 200;
			}

			GestureRecognizer.MainThreadCallback = (float delay, System.Action callback) =>
			{
				StartCoroutine(MainThreadCallback(delay, callback));
			};
		}

		private ICollection<GestureTouch> FilterTouches(ICollection<GestureTouch> touches, GestureRecognizer r)
		{
			filteredTouches.Clear ();
			foreach (GestureTouch t in touches)
			{
				if (gameObjectsForTouch[t.Id] == r.PlatformSpecificView as GameObject)
				{
					filteredTouches.Add (t);
				}
			}
			return filteredTouches;
		}

		private void Update()
		{
			touchesBegan.Clear();
			touchesMoved.Clear();
			touchesEnded.Clear();

			for (int i = 0; i < Input.touchCount; i++)
			{
				Touch t = Input.GetTouch(i);

				// string d = string.Format ("Touch: {0} {1}", t.position, t.phase);
				// Debug.Log (d);

				ProcessTouch(ref t);
			}
			ProcessMouse();

			foreach (GestureTouch t in touchesBegan)
			{
				gameObjectsForTouch[t.Id] = GameObjectForTouch(t.Id, t.X, t.Y);
			}
			
			foreach (GestureRecognizer r in gestures)
			{
				r.ProcessTouchesBegan(FilterTouches(touchesBegan, r));
				r.ProcessTouchesMoved(FilterTouches(touchesMoved, r));
				r.ProcessTouchesEnded(FilterTouches(touchesEnded, r));
			}

			foreach (GestureTouch t in touchesEnded)
			{
				gameObjectsForTouch.Remove(t.Id);
			}
		}

		public void AddGesture(GestureRecognizer gesture)
		{
			gestures.Add(gesture);
		}

		public void RemoveGesture(GestureRecognizer gesture)
		{
			gestures.Remove(gesture);
		}
	}
}

//
// Fingers Gestures
// (c) 2015 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 

using System;

namespace DigitalRubyShared
{
    /// <summary>
    /// Allows rotating an object with just one finger. Typically you would put this on a button
    /// with a rotation symbol and then when the user taps and drags off that button, something
    /// would then rotate.
    /// </summary>
    public class OneTouchRotateGestureRecognizer : RotateGestureRecognizer
    {
        public OneTouchRotateGestureRecognizer()
        {
            oneFinger = true;
        }

        /// <summary>
        /// The location that the touch will rotate around
        /// </summary>
        /// <value>The anchor x value</value>
        public float AnchorX
        {
            get { return anchorX; }
            set { anchorX = value; }
        }

        /// <summary>
        /// The location that the touch will rotate around
        /// </summary>
        /// <value>The anchor y value</value>
        public float AnchorY
        {
            get { return anchorY; }
            set { anchorY = value; }
        }
    }
}


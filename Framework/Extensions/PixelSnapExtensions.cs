﻿namespace Macabre2D.Framework {

    using Microsoft.Xna.Framework;
    using System;

    /// <summary>
    /// Extension methods for dealing with pixel snapping.
    /// </summary>
    public static class PixelSnapExtensions {

        /// <summary>
        /// Converts a <see cref="float"/> to a pixel snapped value according to the <see cref="IGameSettings"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A pixel snapped value</returns>
        public static float ToPixelSnappedValue(this float value) {
            return (int)Math.Round(value * GameSettings.Instance.PixelsPerUnit) * GameSettings.Instance.InversePixelsPerUnit;
        }

        /// <summary>
        /// Converts a <see cref="Vector2"/> to a pixel snapped value according to the <see cref="IGameSettings"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A pixel snapped value</returns>
        public static Vector2 ToPixelSnappedValue(this Vector2 value) {
            return new Vector2((int)Math.Round(value.X * GameSettings.Instance.PixelsPerUnit) * GameSettings.Instance.InversePixelsPerUnit, (int)Math.Round(value.Y * GameSettings.Instance.PixelsPerUnit) * GameSettings.Instance.InversePixelsPerUnit);
        }
    }
}
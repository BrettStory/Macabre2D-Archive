﻿namespace Macabre2D.Tests {

    using Macabre2D.Framework;
    using Microsoft.Xna.Framework;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CameraTests {

        [Test]
        [Category("Unit Test")]
        [TestCase(0f, 0f, 10f, 2f, 2f, 1f, 9f, 0.2f, 0.2f, TestName = "Camera_ZoomTo_WorldPoint_1")]
        [TestCase(0f, 0f, 10f, 10f, 10f, 5f, 5f, 5f, 5f, TestName = "Camera_ZoomTo_WorldPoint_2")]
        public static void Camera_ZoomTo_WorldPointTest(
            float startingX,
            float startingY,
            float startingViewHeight,
            float zoomX,
            float zoomY,
            float zoomAmount,
            float expectedViewHeight,
            float expectedX,
            float expectedY) {
            var camera = new Camera() {
                ViewHeight = startingViewHeight
            };

            camera.Initialize(Substitute.For<IScene>());

            camera.ZoomTo(new Vector2(zoomX, zoomY), zoomAmount);
            Assert.AreEqual(expectedViewHeight, camera.ViewHeight);
            Assert.AreEqual(expectedX, camera.LocalPosition.X, 0.001f);
            Assert.AreEqual(expectedY, camera.LocalPosition.Y, 0.001f);
        }
    }
}
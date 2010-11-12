﻿#region License
//
// (C) Copyright 2009 Patrick Cozzi and Deron Ohlarik
//
// Distributed under the Boost Software License, Version 1.0.
// See License.txt or http://www.boost.org/LICENSE_1_0.txt.
//
#endregion

using System.Drawing;
using System.IO;
using NUnit.Framework;

namespace OpenGlobe.Renderer
{
    [TestFixture]
    public class FrameBufferTests
    {
        [Test]
        public void ColorAttachment()
        {
            using (GraphicsWindow window = Device.CreateWindow(1, 1))
            using (FrameBuffer frameBuffer = window.Context.CreateFrameBuffer())
            using (Texture2D colorTexture = Device.CreateTexture2D(new Texture2DDescription(1, 1, TextureFormat.RedGreenBlue8, false)))
            {
                Assert.IsNull(frameBuffer.ColorAttachments[0]);
                frameBuffer.ColorAttachments[0] = colorTexture;
                Assert.AreEqual(colorTexture, frameBuffer.ColorAttachments[0]);

                //
                // Since frame buffer has a color attachment, it is complete, 
                // and should bind without error.
                //
                window.Context.FrameBuffer = frameBuffer;
                window.Context.FrameBuffer = null;

                frameBuffer.ColorAttachments[0] = null;
                Assert.IsNull(frameBuffer.ColorAttachments[0]);
            }
        }

        [Test]
        public void DepthAttachment()
        {
            using (GraphicsWindow window = Device.CreateWindow(1, 1))
            using (FrameBuffer frameBuffer = window.Context.CreateFrameBuffer())
            using (Texture2D depthTexture = Device.CreateTexture2D(new Texture2DDescription(1, 1, TextureFormat.Depth24, false)))
            {
                Assert.IsNull(frameBuffer.DepthAttachment);
                frameBuffer.DepthAttachment = depthTexture;
                Assert.AreEqual(depthTexture, frameBuffer.DepthAttachment);
                frameBuffer.DepthAttachment = null;
                Assert.IsNull(frameBuffer.DepthAttachment);
            }
        }

        [Test]
        public void DepthStencilAttachment()
        {
            using (GraphicsWindow window = Device.CreateWindow(1, 1))
            using (FrameBuffer frameBuffer = window.Context.CreateFrameBuffer())
            using (Texture2D depthStencilTexture = Device.CreateTexture2D(new Texture2DDescription(1, 1, TextureFormat.Depth32fStencil8, false)))
            {
                Assert.IsNull(frameBuffer.DepthStencilAttachment);
                frameBuffer.DepthStencilAttachment = depthStencilTexture;
                Assert.AreEqual(depthStencilTexture, frameBuffer.DepthStencilAttachment);
                frameBuffer.DepthStencilAttachment = null;
                Assert.IsNull(frameBuffer.DepthStencilAttachment);
            }
        }

        [Test]
        public void EnumerateColorAttachments()
        {
            Texture2DDescription description = new Texture2DDescription(1, 1, TextureFormat.RedGreenBlue8, false);

            using (GraphicsWindow window = Device.CreateWindow(1, 1))
            using (FrameBuffer frameBuffer = window.Context.CreateFrameBuffer())
            using (Texture2D color0 = Device.CreateTexture2D(description))
            using (Texture2D color1 = Device.CreateTexture2D(description))
            using (Texture2D color2 = Device.CreateTexture2D(description))
            {
                frameBuffer.ColorAttachments[0] = color0;
                frameBuffer.ColorAttachments[1] = color1;
                frameBuffer.ColorAttachments[2] = color2;
                Assert.AreEqual(3, frameBuffer.ColorAttachments.Count);

                frameBuffer.ColorAttachments[1] = null;
                Assert.AreEqual(2, frameBuffer.ColorAttachments.Count);

                frameBuffer.ColorAttachments[1] = color1;
                Assert.AreEqual(3, frameBuffer.ColorAttachments.Count);

                int count = 0;
                foreach (Texture2D texture in frameBuffer.ColorAttachments)
                {
                    Assert.AreEqual(description, texture.Description);
                    ++count;
                }
                Assert.AreEqual(frameBuffer.ColorAttachments.Count, count);
            }
        }

        [Test]
        public void HighResolutionSnapFrameBuffer()
        {
            using (GraphicsWindow window = Device.CreateWindow(1, 1))
            using (HighResolutionSnapFrameBuffer snap = new HighResolutionSnapFrameBuffer(window.Context, 2, 10, 2))
            {

                Assert.AreEqual(2, snap.AspectRatio);
                Assert.AreEqual(10, snap.DotsPerInch);

                Assert.AreEqual(2, snap.WidthInInches);
                Assert.AreEqual(20, snap.WidthInPixels);

                Assert.AreEqual(1, snap.HeightInInches);
                Assert.AreEqual(10, snap.HeightInPixels);

                window.Context.FrameBuffer = snap.FrameBuffer;
                window.Context.Viewport = new Rectangle(0, 0, snap.WidthInPixels, snap.HeightInPixels);
                window.Context.Clear(new ClearState() { Buffers = ClearBuffers.ColorAndDepthBuffer, Color = Color.Red, Depth = 0.5f });

                string colorFile = "color.bmp";
                string depthFile = "depth.bmp";

                snap.SaveColorBuffer(colorFile);
                snap.SaveDepthBuffer(depthFile);

                try
                {
                    using (Bitmap colorBitmap = new Bitmap(colorFile))
                    {
                        Assert.AreEqual(snap.WidthInPixels, colorBitmap.Width);
                        Assert.AreEqual(snap.HeightInPixels, colorBitmap.Height);
                    }

                    using (Bitmap depthBitmap = new Bitmap(depthFile))
                    {
                        Assert.AreEqual(snap.WidthInPixels, depthBitmap.Width);
                        Assert.AreEqual(snap.HeightInPixels, depthBitmap.Height);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    File.Delete(colorFile);
                    File.Delete(depthFile);
                }
            }
        }
    }
}

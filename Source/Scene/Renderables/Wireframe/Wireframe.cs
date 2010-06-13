﻿#region License
//
// (C) Copyright 2010 Patrick Cozzi and Deron Ohlarik
//
// Distributed under the Boost Software License, Version 1.0.
// See License.txt or http://www.boost.org/LICENSE_1_0.txt.
//
#endregion

using System;
using System.Drawing;
using MiniGlobe.Core;
using MiniGlobe.Core.Geometry;
using MiniGlobe.Renderer;

namespace MiniGlobe.Scene
{
    public sealed class Wireframe : IDisposable
    {
        public Wireframe(Context context, Mesh mesh)
        {
            RenderState renderState = new RenderState();
            renderState.Blending.Enabled = true;
            renderState.Blending.SourceRGBFactor = SourceBlendingFactor.SourceAlpha;
            renderState.Blending.SourceAlphaFactor = SourceBlendingFactor.SourceAlpha;
            renderState.Blending.DestinationRGBFactor = DestinationBlendingFactor.OneMinusSourceAlpha;
            renderState.Blending.DestinationAlphaFactor = DestinationBlendingFactor.OneMinusSourceAlpha;
            renderState.FacetCulling.FrontFaceWindingOrder = mesh.FrontFaceWindingOrder;
            renderState.DepthTest.Function = DepthTestFunction.LessThanOrEqual;

            //
            // This implementation is based on the 2006 SIGGRAPH Sketch:
            //
            //    Single-pass Wireframe Rendering
            //    http://www2.imm.dtu.dk/pubdb/views/edoc_download.php/4884/pdf/imm4884.pdf
            //
            // NVIDIA published a white paper with some enhancements we can consider:
            //
            //    Solid Wireframe
            //    http://developer.download.nvidia.com/SDK/10.5/direct3d/Source/SolidWireframe/Doc/SolidWireframe.pdf
            //
            // More recent work, which I was not aware of at the time, is:
            //
            //    Two Methods for Antialiased Wireframe Drawing with Hidden Line Removal
            //    http://orbit.dtu.dk/getResource?recordId=219956&objectId=1&versionId=1
            //
            ShaderProgram sp = Device.CreateShaderProgram(
                EmbeddedResources.GetText("MiniGlobe.Scene.Renderables.Wireframe.Shaders.WireframeVS.glsl"),
                EmbeddedResources.GetText("MiniGlobe.Scene.Renderables.Wireframe.Shaders.WireframeGS.glsl"),
                EmbeddedResources.GetText("MiniGlobe.Scene.Renderables.Wireframe.Shaders.WireframeFS.glsl"));
            _lineWidth = sp.Uniforms["u_halfLineWidth"] as Uniform<float>;
            Width = 1;

            _colorUniform = sp.Uniforms["u_color"] as Uniform<Vector3S>;
            Color = Color.Black;

            _drawState = new DrawState(renderState, sp, context.CreateVertexArray(mesh, sp.VertexAttributes, BufferHint.StaticDraw));
            _primitiveType = mesh.PrimitiveType;
        }

        public void Render(Context context, SceneState sceneState)
        {
            Verify.ThrowIfNull(context);
            Verify.ThrowIfNull(sceneState);

            _lineWidth.Value = (float)(0.5 * Width * sceneState.HighResolutionSnapScale);

            context.Draw(_primitiveType, _drawState, sceneState);
        }

        public double Width { get; set; }
        public Color Color 
        {
            get { return _color; }

            set
            {
                _color = value;
                _colorUniform.Value = new Vector3S(_color.R / 255.0f, _color.G / 255.0f, _color.B / 255.0f);
            }
        }

        public bool FacetCullingEnabled
        {
            get { return _drawState.RenderState.FacetCulling.Enabled; }
            set { _drawState.RenderState.FacetCulling.Enabled = value; }
        }

        public CullFace FacetCullingFace
        {
            get { return _drawState.RenderState.FacetCulling.Face; }
            set { _drawState.RenderState.FacetCulling.Face = value; }
        }
        
        public bool DepthTestEnabled
        {
            get { return _drawState.RenderState.DepthTest.Enabled; }
            set { _drawState.RenderState.DepthTest.Enabled = value; }
        }

        public bool Enabled { get; set; }
        public WindingOrder FrontFaceWindingOrder { get; set; }

        #region IDisposable Members

        public void Dispose()
        {
            _drawState.ShaderProgram.Dispose();
            _drawState.VertexArray.Dispose();
        }

        #endregion

        private readonly Uniform<float> _lineWidth;
        private readonly Uniform<Vector3S> _colorUniform;
        private Color _color;
        private readonly DrawState _drawState;
        private readonly PrimitiveType _primitiveType;
    }
}
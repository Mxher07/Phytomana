using Engine;
using Engine.Graphics;
using GLKit;
using OpenGLES;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl.iOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurvivalCraft.IOS {
    class GameViewController:GLKViewController {
        public override void ViewDidLoad() {
            base.ViewDidLoad();
            var gLKView = (GLKView)View;
            gLKView.Context = new EAGLContext(EAGLRenderingAPI.OpenGLES3);
            EAGLContext.SetCurrentContext(gLKView.Context);
        }
        public override void Update() {
            Engine.Window.m_view.DoRender();
        }
    }
}

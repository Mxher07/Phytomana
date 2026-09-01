using CoreGraphics;
using Engine;
using Foundation;
using UIKit;

namespace SurvivalCraft.IOS {
    class TouchView : UIView {
        private UIView parent;
        private float pixelScale;
        Dictionary<UITouch, int> idMap = [];
        private int globalId;
        public TouchView(UIView parentView) {
            parent = parentView;
            Bounds = new CGRect(0, 0, Engine.Window.Size.X, Engine.Window.Size.Y);
            pixelScale = (float)Bounds.Width / (float)parent.Bounds.Width;
            MultipleTouchEnabled = true;
            UserInteractionEnabled = true;
            //禁止多个视图同时接收触摸
            ExclusiveTouch = false;
            globalId = 0;
        }
        public override void TouchesBegan(NSSet touches, UIEvent? evt) {
            foreach (UITouch cc in touches.Cast<UITouch>()) {                
                var point = cc.LocationInView(parent);
                int x = (int)(point.X * pixelScale);
                int y = (int)(point.Y * pixelScale);
                Vector2 vector = new Vector2(x, y);
                idMap.Add(cc,++globalId);
                Engine.Input.Touch.ProcessTouchPressed(globalId, vector);
                //Console.WriteLine("[" + touches.GetHashCode() + "]touch start id:: " + globalId);
            }
        }
        public override void TouchesMoved(NSSet touches, UIEvent? evt) {
            foreach (UITouch cc in touches.Cast<UITouch>()) {
                var point = cc.LocationInView(parent);
                int x = (int)(point.X * pixelScale);
                int y = (int)(point.Y * pixelScale);
                Vector2 vector = new Vector2(x, y);
                if (idMap.TryGetValue(cc, out int id)) {
                    Engine.Input.Touch.ProcessTouchMoved(id, vector);
                    //Console.WriteLine("[" + touches.GetHashCode() + "]touch move id:: " + id);
                }
                //else
                //    Console.WriteLine("Unknown touch move id::"+cc.GetHashCode());
            }
        }
        public override void TouchesEnded(NSSet touches, UIEvent? evt) {
            foreach (UITouch cc in touches.Cast<UITouch>()) {
                var point = cc.LocationInView(parent);
                int x = (int)(point.X * pixelScale);
                int y = (int)(point.Y * pixelScale);
                int processId = cc.GetHashCode();
                Vector2 vector = new Vector2(x, y);
                if(idMap.TryGetValue(cc,out int id)) {
                    Engine.Input.Touch.ProcessTouchReleased(id, vector);
                    //Console.WriteLine("[" + touches.GetHashCode() + "]touch end id:: " + id);
                    idMap.Remove(cc);
                }
                //else
                //    Console.WriteLine("Unknown touch move id::" + cc.GetHashCode());
            }
        }

        public override void TouchesCancelled(NSSet touches, UIEvent? evt) {
            Engine.Input.Touch.Clear();
        }
    }
}

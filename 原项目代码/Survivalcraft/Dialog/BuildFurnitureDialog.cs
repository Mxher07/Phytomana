using System.Xml.Linq;
using Engine;

namespace Game {
    public class BuildFurnitureDialog : Dialog {
        public FurnitureDesign m_design;

        public FurnitureDesign m_sourceDesign;

        public int m_axis;
        public static string fName = "BuildFurnitureDialog";

        public Action<bool> m_handler;

        public bool m_isValid;

        public LabelWidget m_nameLabel;

        public LabelWidget m_statusLabel;

        public FurnitureDesignWidget m_designWidget2d;

        public FurnitureDesignWidget m_designWidget3d;

        public ButtonWidget m_axisButton;

        public ButtonWidget m_leftButton;

        public ButtonWidget m_rightButton;

        public ButtonWidget m_upButton;

        public ButtonWidget m_downButton;

        public ButtonWidget m_mirrorButton;

        public ButtonWidget m_turnRightButton;

        public ButtonWidget m_increaseResolutionButton;

        public ButtonWidget m_decreaseResolutionButton;

        public LabelWidget m_resolutionLabel;

        public ButtonWidget m_nameButton;

        public ButtonWidget m_buildButton;

        public ButtonWidget m_cancelButton;

        public BuildFurnitureDialog(FurnitureDesign design, FurnitureDesign sourceDesign, Action<bool> handler) {
            XElement node = ContentManager.Get<XElement>("Dialogs/BuildFurnitureDialog");
            LoadContents(this, node);
            m_nameLabel = Children.Find<LabelWidget>("BuildFurnitureDialog.Name");
            m_statusLabel = Children.Find<LabelWidget>("BuildFurnitureDialog.Status");
            m_designWidget2d = Children.Find<FurnitureDesignWidget>("BuildFurnitureDialog.Design2d");
            m_designWidget3d = Children.Find<FurnitureDesignWidget>("BuildFurnitureDialog.Design3d");
            m_nameButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.NameButton");
            m_axisButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.AxisButton");
            m_leftButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.LeftButton");
            m_rightButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.RightButton");
            m_upButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.UpButton");
            m_downButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.DownButton");
            m_mirrorButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.MirrorButton");
            m_turnRightButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.TurnRightButton");
            m_increaseResolutionButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.IncreaseResolutionButton");
            m_decreaseResolutionButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.DecreaseResolutionButton");
            m_resolutionLabel = Children.Find<LabelWidget>("BuildFurnitureDialog.ResolutionLabel");
            m_cancelButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.CancelButton");
            m_buildButton = Children.Find<ButtonWidget>("BuildFurnitureDialog.BuildButton");
            m_handler = handler;
            m_design = design;
            m_sourceDesign = sourceDesign;
            m_axis = 1;
            int num = 0;
            num += m_design.Geometry.SubsetOpaqueByFace.Sum(b => b != null ? b.Indices.Count / 3 : 0);
            num += m_design.Geometry.SubsetAlphaTestByFace.Sum(b => b != null ? b.Indices.Count / 3 : 0);
            num += m_design.Geometry.SubsetTransparentByFace.Sum(b => b != null ? b.Indices.Count / 3 : 0);
            if (m_design.Geometry.Draws != null) {
                foreach (var kv in m_design.Geometry.Draws) {
                    num += kv.Value.SubsetOpaqueByFace.Sum(b => b != null ? b.Indices.Count / 3 : 0);
                    num += kv.Value.SubsetAlphaTestByFace.Sum(b => b != null ? b.Indices.Count / 3 : 0);
                    num += kv.Value.SubsetTransparentByFace.Sum(b => b != null ? b.Indices.Count / 3 : 0);
                }
            }
            m_isValid = num <= FurnitureDesign.MaxTriangles;
            m_statusLabel.Text = string.Format(
                LanguageControl.Get(fName, 1),
                num,
                FurnitureDesign.MaxTriangles,
                m_isValid ? LanguageControl.Get(fName, 2) : LanguageControl.Get(fName, 3)
            );
            m_designWidget2d.Design = m_design;
            m_designWidget3d.Design = m_design;
        }

        public override void Update() {
            m_nameLabel.Text = string.IsNullOrEmpty(m_design.Name) ? m_design.GetDefaultName() : m_design.Name;
            m_designWidget2d.Mode = (FurnitureDesignWidget.ViewMode)m_axis;
            m_designWidget3d.Mode = FurnitureDesignWidget.ViewMode.Perspective;
            if (m_designWidget2d.Mode == FurnitureDesignWidget.ViewMode.Side) {
                m_axisButton.Text = LanguageControl.Get(fName, 4);
            }
            if (m_designWidget2d.Mode == FurnitureDesignWidget.ViewMode.Top) {
                m_axisButton.Text = LanguageControl.Get(fName, 5);
            }
            if (m_designWidget2d.Mode == FurnitureDesignWidget.ViewMode.Front) {
                m_axisButton.Text = LanguageControl.Get(fName, 6);
            }
            m_leftButton.IsEnabled = IsShiftPossible(DirectionAxisToDelta(0, m_axis));
            m_rightButton.IsEnabled = IsShiftPossible(DirectionAxisToDelta(1, m_axis));
            m_upButton.IsEnabled = IsShiftPossible(DirectionAxisToDelta(2, m_axis));
            m_downButton.IsEnabled = IsShiftPossible(DirectionAxisToDelta(3, m_axis));
            m_decreaseResolutionButton.IsEnabled = IsDecreaseResolutionPossible();
            m_increaseResolutionButton.IsEnabled = IsIncreaseResolutionPossible();
            m_resolutionLabel.Text = $"{m_design.Resolution}";
            m_buildButton.IsEnabled = m_isValid;
            if (m_nameButton.IsClicked) {
                List<Tuple<string, Action>> list = new();
                if (m_sourceDesign != null) {
                    list.Add(
                        new Tuple<string, Action>(
                            LanguageControl.Get(fName, 7),
                            delegate {
                                Dismiss(false);
                                DialogsManager.ShowDialog(
                                    ParentWidget,
                                    new TextBoxDialog(
                                        LanguageControl.Get(fName, 10),
                                        m_sourceDesign.Name,
                                        20,
                                        delegate(string s) {
                                            try {
                                                if (s != null) {
                                                    m_sourceDesign.Name = s;
                                                }
                                            }
                                            catch (Exception ex3) {
                                                DialogsManager.ShowDialog(
                                                    ParentWidget,
                                                    new MessageDialog(LanguageControl.Error, ex3.Message, LanguageControl.Ok, null, null)
                                                );
                                            }
                                        }
                                    )
                                );
                            }
                        )
                    );
                    list.Add(
                        new Tuple<string, Action>(
                            LanguageControl.Get(fName, 8),
                            delegate {
                                DialogsManager.ShowDialog(
                                    ParentWidget,
                                    new TextBoxDialog(
                                        LanguageControl.Get(fName, 11),
                                        m_design.Name,
                                        20,
                                        delegate(string s) {
                                            try {
                                                if (s != null) {
                                                    m_design.Name = s;
                                                }
                                            }
                                            catch (Exception ex2) {
                                                DialogsManager.ShowDialog(
                                                    ParentWidget,
                                                    new MessageDialog(LanguageControl.Error, ex2.Message, LanguageControl.Ok, null, null)
                                                );
                                            }
                                        }
                                    )
                                );
                            }
                        )
                    );
                }
                else {
                    list.Add(
                        new Tuple<string, Action>(
                            LanguageControl.Get(fName, 9),
                            delegate {
                                DialogsManager.ShowDialog(
                                    ParentWidget,
                                    new TextBoxDialog(
                                        LanguageControl.Get(fName, 11),
                                        m_design.Name,
                                        20,
                                        delegate(string s) {
                                            try {
                                                if (s != null) {
                                                    m_design.Name = s;
                                                }
                                            }
                                            catch (Exception ex) {
                                                DialogsManager.ShowDialog(
                                                    ParentWidget,
                                                    new MessageDialog(LanguageControl.Error, ex.Message, LanguageControl.Ok, null, null)
                                                );
                                            }
                                        }
                                    )
                                );
                            }
                        )
                    );
                }
                if (list.Count == 1) {
                    list[0].Item2();
                }
                else {
                    DialogsManager.ShowDialog(
                        ParentWidget,
                        new ListSelectionDialog(
                            LanguageControl.Get(fName, 11),
                            list,
                            64f,
                            t => ((Tuple<string, Action>)t).Item1,
                            delegate(object t) { ((Tuple<string, Action>)t).Item2(); }
                        )
                    );
                }
            }
            if (m_axisButton.IsClicked) {
                m_axis = (m_axis + 1) % 3;
            }
            if (m_leftButton.IsClicked) {
                Shift(DirectionAxisToDelta(0, m_axis));
            }
            if (m_rightButton.IsClicked) {
                Shift(DirectionAxisToDelta(1, m_axis));
            }
            if (m_upButton.IsClicked) {
                Shift(DirectionAxisToDelta(2, m_axis));
            }
            if (m_downButton.IsClicked) {
                Shift(DirectionAxisToDelta(3, m_axis));
            }
            if (m_mirrorButton.IsClicked) {
                m_design.Mirror(m_axis);
            }
            if (m_turnRightButton.IsClicked) {
                m_design.Rotate(m_axis, 1);
            }
            if (m_decreaseResolutionButton.IsClicked) {
                DecreaseResolution();
            }
            if (m_increaseResolutionButton.IsClicked) {
                IncreaseResolution();
            }
            if (m_buildButton.IsClicked && m_isValid) {
                Dismiss(true);
            }
            if (Input.Back
                || m_cancelButton.IsClicked) {
                Dismiss(false);
            }
        }

        public bool IsShiftPossible(Point3 delta) {
            int resolution = m_design.Resolution;
            Box box = m_design.Box;
            box.Location += delta;
            if (box.Left >= 0
                && box.Top >= 0
                && box.Near >= 0
                && box.Right <= resolution
                && box.Bottom <= resolution) {
                return box.Far <= resolution;
            }
            return false;
        }

        public void Shift(Point3 delta) {
            if (IsShiftPossible(delta)) {
                m_design.Shift(delta);
            }
        }

        public bool IsDecreaseResolutionPossible() {
            int resolution = m_design.Resolution;
            if (resolution > 2) {
                int num = MathUtils.Max(m_design.Box.Width, m_design.Box.Height, m_design.Box.Depth);
                return resolution > num;
            }
            return false;
        }

        public void DecreaseResolution() {
            if (IsDecreaseResolutionPossible()) {
                int resolution = m_design.Resolution;
                Point3 zero = Point3.Zero;
                if (m_design.Box.Right >= resolution) {
                    zero.X = -1;
                }
                if (m_design.Box.Bottom >= resolution) {
                    zero.Y = -1;
                }
                if (m_design.Box.Far >= resolution) {
                    zero.Z = -1;
                }
                m_design.Shift(zero);
                m_design.Resize(resolution - 1);
            }
        }

        public bool IsIncreaseResolutionPossible() => m_design.Resolution < FurnitureDesign.maxDesign;

        public void IncreaseResolution() {
            if (IsIncreaseResolutionPossible()) {
                m_design.Resize(m_design.Resolution + 1);
            }
        }

        public static Point3 DirectionAxisToDelta(int direction, int axis) {
            if (direction == 0) {
                switch (axis) {
                    case 0: return new Point3(0, 0, 1);
                    case 1: return new Point3(1, 0, 0);
                    case 2: return new Point3(1, 0, 0);
                }
            }
            if (direction == 1) {
                switch (axis) {
                    case 0: return new Point3(0, 0, -1);
                    case 1: return new Point3(-1, 0, 0);
                    case 2: return new Point3(-1, 0, 0);
                }
            }
            if (direction == 2) {
                switch (axis) {
                    case 0: return new Point3(0, 1, 0);
                    case 1: return new Point3(0, 0, 1);
                    case 2: return new Point3(0, 1, 0);
                }
            }
            if (direction == 3) {
                switch (axis) {
                    case 0: return new Point3(0, -1, 0);
                    case 1: return new Point3(0, 0, -1);
                    case 2: return new Point3(0, -1, 0);
                }
            }
            return Point3.Zero;
        }

        public void Dismiss(bool result) {
            DialogsManager.HideDialog(this);
            m_handler(result);
        }
    }
}
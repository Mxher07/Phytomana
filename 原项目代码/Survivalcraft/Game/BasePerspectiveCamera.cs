using Engine;
using Engine.Graphics;

namespace Game {
    public abstract class BasePerspectiveCamera : Camera {
        public Vector3 m_viewPosition;

        public Vector3 m_viewDirection;

        public Vector3 m_viewUp;

        public Vector3 m_viewRight;

        public Matrix? m_viewMatrix;

        public Matrix? m_invertedViewMatrix;

        public Matrix? m_projectionMatrix;

        public Matrix? m_invertedProjectionMatrix;

        public Matrix? m_screenProjectionMatrix;

        public Matrix? m_viewProjectionMatrix;

        public Vector2? m_viewportSize;

        public Matrix? m_viewportMatrix;

        public BoundingFrustum m_viewFrustum;

        public bool m_viewFrustumValid;

        public override Vector3 ViewPosition => m_viewPosition;

        public override Vector3 ViewDirection => m_viewDirection;

        public override Vector3 ViewUp => m_viewUp;

        public override Vector3 ViewRight => m_viewRight;

        public override Matrix ViewMatrix
        {
            get {
                if (!m_viewMatrix.HasValue) {
                    if (Eye == null) {
                        m_viewMatrix = Matrix.CreateLookAt(m_viewPosition, m_viewPosition + m_viewDirection, m_viewUp);
                    }
                    else {
                        Matrix eyeToHead = VrManager.GetEyeToHeadTransform(Eye.Value);
                        m_viewMatrix = Matrix.CreateLookAt(m_viewPosition, m_viewPosition + m_viewDirection, m_viewUp) * Matrix.Invert(eyeToHead);
                    }
                }
                return m_viewMatrix.Value;
            }
        }

        public override Matrix InvertedViewMatrix //转置视图矩阵
        {
            get {
                if (!m_invertedViewMatrix.HasValue) {
                    m_invertedViewMatrix = Matrix.Invert(ViewMatrix);
                }
                return m_invertedViewMatrix.Value;
            }
        }

        public override Matrix ProjectionMatrix {
            get {
                if (!m_projectionMatrix.HasValue) {
                    m_projectionMatrix = CalculateBaseProjectionMatrix();
                    ViewWidget viewWidget = GameWidget.ViewWidget;
                    if (!viewWidget.ScalingRenderTargetSize.HasValue && Eye == null) {
                        m_projectionMatrix *= MatrixUtils.CreateScaleTranslation(
                                0.5f * viewWidget.ActualSize.X,
                                -0.5f * viewWidget.ActualSize.Y,
                                viewWidget.ActualSize.X / 2f,
                                viewWidget.ActualSize.Y / 2f
                            )
                            * viewWidget.GlobalTransform
                            * MatrixUtils.CreateScaleTranslation(2f / Display.Viewport.Width, -2f / Display.Viewport.Height, -1f, 1f);
                    }
                }
                return m_projectionMatrix.Value;
            }
        }

        public override Matrix ScreenProjectionMatrix {
            get {
                if (!m_screenProjectionMatrix.HasValue) {
                    if (Eye == null) {
                        Point2 size = Window.Size;
                        ViewWidget viewWidget = GameWidget.ViewWidget;
                        m_screenProjectionMatrix = CalculateBaseProjectionMatrix()
                            * MatrixUtils.CreateScaleTranslation(
                                0.5f * viewWidget.ActualSize.X,
                                -0.5f * viewWidget.ActualSize.Y,
                                viewWidget.ActualSize.X / 2f,
                                viewWidget.ActualSize.Y / 2f
                            )
                            * viewWidget.GlobalTransform
                            * MatrixUtils.CreateScaleTranslation(2f / size.X, -2f / size.Y, -1f, 1f);
                    }
                    else {
                        m_screenProjectionMatrix = CalculateBaseProjectionMatrix();
                    }
                }
                return m_screenProjectionMatrix.Value;
            }
        }

        public override Matrix InvertedProjectionMatrix {
            get {
                if (!m_invertedProjectionMatrix.HasValue) {
                    m_invertedProjectionMatrix = Matrix.Invert(ProjectionMatrix);
                }
                return m_invertedProjectionMatrix.Value;
            }
        }

        public override Matrix ViewProjectionMatrix {
            get {
                if (!m_viewProjectionMatrix.HasValue) {
                    //世界坐标矩阵 * 投影矩阵得到屏幕矩阵，即将世界的坐标转换到屏幕的坐标
                    m_viewProjectionMatrix = ViewMatrix * ProjectionMatrix;
                    /*
                    //测试，将屏幕坐标转回到世界坐标
                    //将屏幕坐标的0,0转换到世界的坐标中
                    Vector3 vector = Vector3.Transform(Vector3.Zero, InvertedProjectionMatrix);
                    vector = Vector3.Transform(vector,InvertedViewMatrix);
                    */
                }
                return m_viewProjectionMatrix.Value;
            }
        }

        public override Vector2 ViewportSize {
            get {
                if (!m_viewportSize.HasValue) {
                    if (Eye != null) {
                        m_viewportSize = new Vector2(VrManager.SwapchainWidth, VrManager.SwapchainHeight);
                    }
                    else {
                        ViewWidget viewWidget = GameWidget.ViewWidget;
                        m_viewportSize = viewWidget.ScalingRenderTargetSize.HasValue
                            ? new Vector2(viewWidget.ScalingRenderTargetSize.Value)
                            : new Vector2(
                                viewWidget.ActualSize.X * viewWidget.GlobalTransform.Right.Length(),
                                viewWidget.ActualSize.Y * viewWidget.GlobalTransform.Up.Length()
                            );
                    }
                }
                return m_viewportSize.Value;
            }
        }

        public override Matrix ViewportMatrix {
            get {
                if (!m_viewportMatrix.HasValue) {
                    if (Eye != null) {
                        m_viewportMatrix = Matrix.Identity;
                    }
                    else {
                        ViewWidget viewWidget = GameWidget.ViewWidget;
                        if (viewWidget.ScalingRenderTargetSize.HasValue) {
                            m_viewportMatrix = Matrix.Identity;
                        }
                        else {
                            Matrix identity = Matrix.Identity;
                            identity.Right = Vector3.Normalize(viewWidget.GlobalTransform.Right);
                            identity.Up = Vector3.Normalize(viewWidget.GlobalTransform.Up);
                            identity.Forward = viewWidget.GlobalTransform.Forward;
                            identity.Translation = viewWidget.GlobalTransform.Translation;
                            m_viewportMatrix = identity;
                        }
                    }
                }
                return m_viewportMatrix.Value;
            }
        }

        public override BoundingFrustum ViewFrustum {
            get {
                if (!m_viewFrustumValid) {
                    if (m_viewFrustum == null) {
                        m_viewFrustum = new BoundingFrustum(ViewProjectionMatrix);
                    }
                    else {
                        m_viewFrustum.Matrix = ViewProjectionMatrix;
                    }
                    m_viewFrustumValid = true;
                }
                return m_viewFrustum;
            }
        }

        public override void PrepareForDrawing() {
            m_viewMatrix = null;
            m_invertedViewMatrix = null;
            m_projectionMatrix = null;
            m_invertedProjectionMatrix = null;
            m_screenProjectionMatrix = null;
            m_viewProjectionMatrix = null;
            m_viewportSize = null;
            m_viewportMatrix = null;
            m_viewFrustumValid = false;
        }

        public BasePerspectiveCamera(GameWidget gameWidget) : base(gameWidget) { }

        public void SetupPerspectiveCamera(Vector3 position, Vector3 direction, Vector3 up) {
            m_viewPosition = position;
            m_viewDirection = Vector3.Normalize(direction);
            m_viewUp = Vector3.Normalize(up);
            m_viewRight = Vector3.Normalize(Vector3.Cross(m_viewDirection, m_viewUp));
        }

        /// <summary>
        ///     计算基础投影矩阵，创建透视视野
        /// </summary>
        /// <returns></returns>
        public virtual Matrix CalculateBaseProjectionMatrix() {
            Matrix result;
            if (!Eye.HasValue) {
                float viewAngle = 80f * SettingsManager.ViewAngle;
                ViewWidget viewWidget = GameWidget.ViewWidget;
                float aspectRatio = viewWidget.ActualSize.X / viewWidget.ActualSize.Y; //视野长宽比
                result = Matrix.CreatePerspectiveFieldOfView(MathUtils.DegToRad(viewAngle), aspectRatio, 0.1f, 2048f); //参数1视野Y宽度，参数2纵横比，参数3近平面，参数4远平面
            }
            else {
                result = VrManager.GetProjectionMatrix(Eye.Value, 0.1f, 2048f);
            }
            ModsManager.HookAction(
                "RecalculateCameraProjection",
                loader => {
                    loader.RecalculateCameraProjection(this, ref result);
                    return false;
                }
            );
            return result;
        }
    }
}
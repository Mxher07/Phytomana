using Engine;
using GameEntitySystem;

namespace Game {
    public class SubsystemBodies : Subsystem, IUpdateable {
        public const float m_areaSize = 8f;

        public DynamicArray<ComponentBody> m_componentBodies = [];

        public Dictionary<ComponentBody, Point2> m_areaByComponentBody = [];

        public Dictionary<Point2, DynamicArray<ComponentBody>> m_componentBodiesByArea = [];

        public Dictionary<ComponentBody, Point2>.KeyCollection Bodies => m_areaByComponentBody.Keys;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public void FindBodiesAroundPoint(Vector2 point, float radius, DynamicArray<ComponentBody> result) {
            int num = (int)MathF.Floor((point.X - radius) / 8f);
            int num2 = (int)MathF.Floor((point.Y - radius) / 8f);
            int num3 = (int)MathF.Floor((point.X + radius) / 8f);
            int num4 = (int)MathF.Floor((point.Y + radius) / 8f);
            for (int i = num; i <= num3; i++) {
                for (int j = num2; j <= num4; j++) {
                    if (m_componentBodiesByArea.TryGetValue(new Point2(i, j), out DynamicArray<ComponentBody> value)) {
                        for (int k = 0; k < value.Count; k++) {
                            result.Add(value.Array[k]);
                        }
                    }
                }
            }
        }

        public void FindBodiesInArea(Vector2 corner1, Vector2 corner2, DynamicArray<ComponentBody> result) {
            Point2 point = new((int)MathF.Floor(corner1.X / 8f), (int)MathF.Floor(corner1.Y / 8f));
            Point2 point2 = new((int)MathF.Floor(corner2.X / 8f), (int)MathF.Floor(corner2.Y / 8f));
            int num = MathUtils.Min(point.X, point2.X) - 1;
            int num2 = MathUtils.Min(point.Y, point2.Y) - 1;
            int num3 = MathUtils.Max(point.X, point2.X) + 1;
            int num4 = MathUtils.Max(point.Y, point2.Y) + 1;
            for (int i = num; i <= num3; i++) {
                for (int j = num2; j <= num4; j++) {
                    if (m_componentBodiesByArea.TryGetValue(new Point2(i, j), out DynamicArray<ComponentBody> value)) {
                        for (int k = 0; k < value.Count; k++) {
                            result.Add(value.Array[k]);
                        }
                    }
                }
            }
        }

        public BodyRaycastResult? Raycast(Vector3 start, Vector3 end, float inflateAmount, Func<ComponentBody, float, bool> action) {
            float num = Vector3.Distance(start, end);
            Ray3 ray = new(start, num > 0f ? (end - start) / num : Vector3.UnitX);
            Vector2 corner = new(start.X, start.Z);
            Vector2 corner2 = new(end.X, end.Z);
            BodyRaycastResult bodyRaycastResult = default;
            bodyRaycastResult.Ray = ray;
            bodyRaycastResult.Distance = float.MaxValue;
            BodyRaycastResult value = bodyRaycastResult;
            m_componentBodies.Clear();
            FindBodiesInArea(corner, corner2, m_componentBodies);
            for (int i = 0; i < m_componentBodies.Count; i++) {
                ComponentBody componentBody = m_componentBodies.Array[i];
                if (componentBody == null) {
                    continue;
                }
                if (componentBody.IsRaycastTransparent) {
                    continue;
                }
                float? num2 = null;
                bool skipVanilla_ = false;
                ModsManager.HookAction(
                    "BodyCountInRaycast",
                    loader => {
                        num2 = loader.BodyCountInRaycast(componentBody, ray, out bool skip);
                        skipVanilla_ |= skip;
                        return false;
                    }
                );
                if (!skipVanilla_) {
                    if (inflateAmount > 0f) {
                        BoundingBox boundingBox = componentBody.BoundingBox;
                        boundingBox.Min -= new Vector3(inflateAmount);
                        boundingBox.Max += new Vector3(inflateAmount);
                        num2 = ray.Intersection(boundingBox);
                    }
                    else {
                        num2 = ray.Intersection(componentBody.BoundingBox);
                    }
                }
                if (num2.HasValue
                    && num2.Value <= num
                    && num2.Value < value.Distance
                    && action(componentBody, num2.Value)) {
                    value.Distance = num2.Value;
                    value.ComponentBody = componentBody;
                }
            }
            if (value.ComponentBody == null) {
                return null;
            }
            return value;
        }

        public override void OnEntityAdded(Entity entity) {
            foreach (ComponentBody item in entity.FindComponents<ComponentBody>()) {
                AddBody(item);
            }
        }

        public override void OnEntityRemoved(Entity entity) {
            foreach (ComponentBody item in entity.FindComponents<ComponentBody>()) {
                RemoveBody(item);
            }
        }

        public virtual void Update(float dt) {
            foreach (ComponentBody body in Bodies) {
                UpdateBody(body);
            }
        }

        public void AddBody(ComponentBody componentBody) {
            Vector3 position = componentBody.Position;
            Point2 point = new((int)MathF.Floor(position.X / 8f), (int)MathF.Floor(position.Z / 8f));
            m_areaByComponentBody.Add(componentBody, point);
            if (!m_componentBodiesByArea.TryGetValue(point, out DynamicArray<ComponentBody> value)) {
                value = [];
                m_componentBodiesByArea.Add(point, value);
            }
            value.Add(componentBody);
            componentBody.PositionChanged += ComponentBody_PositionChanged;
        }

        public void RemoveBody(ComponentBody componentBody) {
            Point2 key = m_areaByComponentBody[componentBody];
            m_areaByComponentBody.Remove(componentBody);
            m_componentBodiesByArea[key].Remove(componentBody);
            componentBody.PositionChanged -= ComponentBody_PositionChanged;
        }

        public virtual void UpdateBody(ComponentBody componentBody) {
            Vector3 position = componentBody.Position;
            Point2 point = new((int)MathF.Floor(position.X / 8f), (int)MathF.Floor(position.Z / 8f));
            Point2 point2 = m_areaByComponentBody[componentBody];
            if (point != point2) {
                m_areaByComponentBody[componentBody] = point;
                m_componentBodiesByArea[point2].Remove(componentBody);
                if (!m_componentBodiesByArea.TryGetValue(point, out DynamicArray<ComponentBody> value)) {
                    value = [];
                    m_componentBodiesByArea.Add(point, value);
                }
                value.Add(componentBody);
            }
        }

        public void ComponentBody_PositionChanged(ComponentFrame componentFrame) {
            UpdateBody((ComponentBody)componentFrame);
        }
    }
}
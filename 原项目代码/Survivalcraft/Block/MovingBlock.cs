using Engine;
using Engine.Serialization;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game {
    public class MovingBlock {
        public static bool IsNullOrStopped(MovingBlock movingBlock) {
            if (movingBlock == null) {
                return true;
            }
            IMovingBlockSet movingBlockSet = movingBlock.MovingBlockSet;
            if (movingBlockSet == null) {
                return true;
            }
            if (movingBlockSet.Stopped) {
                return true;
            }
            return false;
        }

        public Vector3 Position => MovingBlockSet.Position + new Vector3(Offset);

        public static MovingBlock LoadFromPositionAndOffset(Project project, Vector3 movingBlocksPosition, Point3 offset, bool throwOnError = true) {
            SubsystemMovingBlocks subsystemMovingBlocks = project.FindSubsystem<SubsystemMovingBlocks>();
            IMovingBlockSet movingBlockSet = subsystemMovingBlocks.MovingBlockSetEnumerable.FirstOrDefault(
                set => set.Position.ToString() == movingBlocksPosition.ToString(),
                null
            );
            if (movingBlockSet != null) {
                MovingBlock movingBlock = movingBlockSet.Blocks.FirstOrDefault(block => block.Offset == offset, null);
                if (!IsNullOrStopped(movingBlock)) {
                    return movingBlock;
                }
                if (throwOnError) {
                    throw new Exception($"Required moving block offset {offset} is not found in MovingBlockSet {movingBlocksPosition}");
                }
                return null;
            }
            if (throwOnError) {
                throw new Exception($"Required moving block set {movingBlocksPosition} is not found.");
            }
            return null;
        }

        public static MovingBlock LoadFromValuesDictionary(Project project,
            ValuesDictionary valuesDictionary,
            bool throwOnError = true,
            bool throwIfNotFound = false) {
            Vector3? movingBlocksPosition = valuesDictionary.GetValue<Vector3?>("MovingBlockSetPosition", null);
            Point3? point = valuesDictionary.GetValue<Point3?>("MovingBlockOffset", null);
            if (movingBlocksPosition.HasValue
                && point.HasValue) {
                return LoadFromPositionAndOffset(project, movingBlocksPosition.Value, point.Value);
            }
            if (throwIfNotFound) {
                throw new Exception("MovingBlockSetPosition is not contained in valuesDictionary.");
            }
            return null;
        }

        public static MovingBlock LoadFromString(Project project, string movingBlockInfo, out Exception exception) {
            exception = null;
            try {
                string[] str1 = movingBlockInfo.Split(';');
                if (str1.Length == 0
                    || !str1[0].Contains("MovingBlock")) {
                    exception = new InvalidDataException($"String \"{movingBlockInfo}\" is not valid for moving block load.");
                    return null;
                }
                Vector3 movingBlockSetPosition = HumanReadableConverter.ConvertFromString<Vector3>(str1[1]);
                Point3 movingBlockOffset = HumanReadableConverter.ConvertFromString<Point3>(str1[2]);
                return LoadFromPositionAndOffset(project, movingBlockSetPosition, movingBlockOffset);
            }
            catch (Exception e) {
                exception = e;
                return null;
            }
        }

        public void SetValuesDicionary(ValuesDictionary valuesDictionary, bool saveWhenStopped = false) {
            if (!IsNullOrStopped(this) || saveWhenStopped) {
                valuesDictionary.SetValue("MovingBlockSetPosition", MovingBlockSet.Position);
                valuesDictionary.SetValue("MovingBlockOffset", Offset);
            }
        }

        public override string ToString() =>
            $"MovingBlock;{HumanReadableConverter.ConvertToString(MovingBlockSet.Position)};{HumanReadableConverter.ConvertToString(Offset)}";

        public Point3 Offset;

        public int Value;

        public IMovingBlockSet MovingBlockSet;
    }
}
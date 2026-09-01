namespace Game {
    public class MatchBlock : FlatBlock {
        public static int Index = 108;
        public override int GetPriorityUse(int value, ComponentMiner componentMiner) => 1;
    }
}
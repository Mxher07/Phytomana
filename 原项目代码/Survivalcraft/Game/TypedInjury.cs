namespace Game {
    public class AttackInjury : Injury {
        public AttackInjury(float amount, Attackment attackment) : base(
            amount,
            attackment?.Attacker?.FindComponent<ComponentCreature>(),
            false,
            attackment.CauseOfDeath
        ) => Attackment = attackment;
    }

    public class FireInjury : Injury {
        public FireInjury(float amount, ComponentCreature attacker) : base(
            amount,
            attacker,
            false,
            LanguageControl.Get(typeof(ComponentHealth).Name, 5)
        ) { }
    }

    public class ExplosionInjury : Injury {
        public ExplosionInjury(float amount) : base(amount, null, false, LanguageControl.Get(typeof(ComponentHealth).Name, 8)) { }
    }

    public class BlockInjury : Injury {
        public CellFace? CellFace;

        protected int m_blockValue;

        public SubsystemTerrain SubsystemTerrain;

        public int BlockValue {
            get {
                if (m_blockValue > 0) {
                    return m_blockValue;
                }
                if (SubsystemTerrain == null
                    || CellFace == null) {
                    return -1;
                }
                return SubsystemTerrain.Terrain.GetCellValue(CellFace.Value.X, CellFace.Value.Y, CellFace.Value.Z);
            }
            set => m_blockValue = value;
        }

        public BlockInjury(float amount, CellFace? cellFace, string cause, SubsystemTerrain subsystemTerrain) : base(amount, null, false, cause) {
            CellFace = cellFace;
            SubsystemTerrain = subsystemTerrain;
        }
    }

    public class VitalStatsInjury : Injury {
        public VitalStatsInjury(float amount, string cause) : base(amount, null, false, cause) { }
    }

    public class SuicideInjury : Injury {
        public SuicideInjury(float amount) : base(amount, null, true, LanguageControl.GetContentWidgets(typeof(VitalStatsWidget).Name, "Choked")) { }
    }
}
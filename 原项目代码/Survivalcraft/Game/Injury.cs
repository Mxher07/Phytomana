using Engine;

namespace Game {
    public class Injury {
        public ComponentHealth ComponentHealth;
        public float Amount;
        public Attackment Attackment;
        public bool IgnoreInvulnerability;
        public string Cause;
        protected ComponentCreature m_attacker;

        public Injury(float amount, ComponentCreature attacker, bool ignoreInvulnerability, string cause) {
            Amount = amount;
            m_attacker = attacker;
            IgnoreInvulnerability = ignoreInvulnerability;
            Cause = cause;
        }

        float Health {
            get => ComponentHealth.Health;
            set => ComponentHealth.Health = value;
        }

        public virtual ComponentCreature Attacker {
            get {
                if (m_attacker != null) {
                    return m_attacker;
                }
                return m_attacker = Attackment?.Attacker?.FindComponent<ComponentCreature>();
            }
            set => m_attacker = value;
        }

        public virtual ComponentPlayer AttackerPlayer => Attacker as ComponentPlayer;

        public virtual void AddPlayerStats() {
            if (ComponentHealth.m_componentCreature.PlayerStats != null) {
                if (Attackment?.Attacker != null) {
                    ComponentHealth.m_componentCreature.PlayerStats.HitsReceived++;
                }
                ComponentHealth.m_componentCreature.PlayerStats.TotalHealthLost += MathUtils.Min(Amount, ComponentHealth.Health);
            }
        }

        public virtual void ProcessToLivingCreature() { }

        public virtual void Process() {
            if (ComponentHealth == null) {
                return;
            }
            ModsManager.HookAction(
                "CalculateCreatureInjuryAmount",
                loader => {
                    loader.CalculateCreatureInjuryAmount(this);
                    return false;
                }
            );
            if (!(Amount > 0f)
                || (!IgnoreInvulnerability && ComponentHealth.IsInvulnerable)) {
                return;
            }
            ComponentHealth.Injured?.Invoke(this);
            if (ComponentHealth.Health > 0f) {
                AddPlayerStats();
                Health = MathUtils.Max(Health - Amount, 0f);
            }
        }
    }
}
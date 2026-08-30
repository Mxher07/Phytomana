using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using Game;

namespace Game {
    public class ManaParticleSystem : ParticleSystem<ManaParticleSystem.Particle> {
        public class Particle : Game.Particle {
            public Vector3 Velocity;
            public float TimeToLive;
            public float MaxTimeToLive;
            public Vector3 StartPosition;
            public Vector3? TargetPosition;
            public Color[] Colors;
            public float CurrentColorIndex;
            public float StartSize;
            public bool IsFading;
        }

        private Random m_random = new Random();
        private bool m_isMultiColor;

        public ManaParticleSystem(Vector3 position, float size, float duration, Color color, Vector3? targetPosition = null, int count = 2) 
            : base(count) {
            InitializeParticles(position, size, duration, new Color[] { color }, targetPosition);
            m_isMultiColor = false;
            Log.Information($"[PhytoMana]SingleColor ManaParticle Created.");
        }

        public ManaParticleSystem(Vector3 position, float size, float duration, Color[] colors, Vector3? targetPosition = null) 
            : base(2) {
            if (colors == null || colors.Length < 2) {
                throw new ArgumentException("Colors are null!");
            }
            InitializeParticles(position, size, duration, colors, targetPosition);
            m_isMultiColor = true;
            Log.Information($"[PhytoMana]MultiColor ManaParticle Created.");
        }

        private void InitializeParticles(Vector3 position, float size, float duration, Color[] colors, Vector3? targetPosition) {
            Texture = ContentManager.Get<Texture2D>("Textures/PhytoMana/Mana");
            TextureSlotsCount = 1;
            
            Color[] colorsCopy = new Color[colors.Length];
            Array.Copy(colors, colorsCopy, colors.Length);

            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                particle.IsActive = true;
                particle.StartPosition = position + 0.4f * size * new Vector3(
                    m_random.Float(-1f, 1f), 
                    m_random.Float(-1f, 1f), 
                    m_random.Float(-1f, 1f)
                );
                particle.Position = particle.StartPosition;
                particle.TargetPosition = targetPosition;
                particle.TimeToLive = duration;
                particle.MaxTimeToLive = duration;
                particle.Colors = colorsCopy;
                particle.CurrentColorIndex = 0f;
                particle.StartSize = 0.3f * size;
                particle.Size = new Vector2(particle.StartSize);
                particle.IsFading = false;
                particle.FlipX = m_random.Bool();
                particle.FlipY = m_random.Bool();
                
                particle.Velocity = 0.5f * size * new Vector3(
                    m_random.Float(-1f, 1f), 
                    m_random.Float(-1f, 1f), 
                    m_random.Float(-1f, 1f)
                );
            }
        }

        public override bool Simulate(float dt) {
            dt = Math.Clamp(dt, 0f, 0.1f);
            float num = MathF.Pow(0.1f, dt);
            bool hasActiveParticles = false;

            for (int i = 0; i < Particles.Length; i++) {
                Particle particle = Particles[i];
                if (!particle.IsActive) continue;

                hasActiveParticles = true;
                particle.TimeToLive -= dt;

                if (particle.TimeToLive <= 0f) {
                    particle.IsActive = false;
                    continue;
                }

                float lifeProgress = 1f - (particle.TimeToLive / particle.MaxTimeToLive);
                float remainingRatio = particle.TimeToLive / particle.MaxTimeToLive;

                if (particle.TargetPosition.HasValue) {
                    float moveProgress = Math.Min(lifeProgress / 0.7f, 1f);
                    particle.Position = Vector3.Lerp(particle.StartPosition, particle.TargetPosition.Value, moveProgress);
                } else {
                    particle.Position += particle.Velocity * dt;
                    particle.Velocity *= num;
                }

                if (particle.Colors != null && particle.Colors.Length > 0) {
                    if (particle.Colors.Length == 1) {
                        particle.Color = particle.Colors[0];
                    } else {
                        float colorProgress = lifeProgress * (particle.Colors.Length - 1);
                        int index = (int)Math.Floor(colorProgress);
                        int nextIndex = Math.Min(index + 1, particle.Colors.Length - 1);
                        float lerpFactor = colorProgress - index;
                        
                        Color currentColor = particle.Colors[index];
                        Color nextColor = particle.Colors[nextIndex];
                        
                        particle.Color = Color.Lerp(currentColor, nextColor, lerpFactor);
                    }
                    
                    particle.Color.A = 255;
                }

                if (remainingRatio <= 0.3f) {
                    particle.IsFading = true;
                    float fadeProgress = 1f - (remainingRatio / 0.3f);
                    float currentSize = particle.StartSize * (1f - fadeProgress);
                    particle.Size = new Vector2(Math.Max(currentSize, 0.01f));
                    
                    if (remainingRatio <= 0f) {
                        // 这里会在循环末尾处理
                    }
                } else {
                    particle.Size = new Vector2(particle.StartSize);
                }

                // 销毁
                if (particle.IsFading && particle.Size.X <= 0.01f) {
                    particle.TimeToLive = Math.Min(particle.TimeToLive, -0.25f);
                }

                if (!particle.TargetPosition.HasValue) {
                    particle.Velocity.Y += 0.5f * dt;
                }
            }

            bool allInactive = true;
            for (int i = 0; i < Particles.Length; i++) {
                if (Particles[i].IsActive) {
                    allInactive = false;
                    break;
                }
            }

            return allInactive;
        }
    }
}
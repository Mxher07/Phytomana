#if !IOS && !BROWSER
using Engine;
using GameEntitySystem;
using Jint;
using Jint.Native.Function;

namespace Game {
    public class JsModLoader : ModLoader {
        public override void OnMinerDig(ComponentMiner miner, TerrainRaycastResult raycastResult, ref float DigProgress, out bool Digged) {
            bool Digged1 = false;
            if (JsInterface.handlersDictionary.TryGetValue("OnMinerDig", out List<Function> functions)) {
                float DigProgress1 = DigProgress;
                foreach (Function function in functions) {
                    Digged1 |= JsInterface.Invoke(function, miner, raycastResult, DigProgress1).AsBoolean();
                }
            }
            Digged = Digged1;
        }

        [Obsolete]
        public override void OnMinerPlace(ComponentMiner miner,
            TerrainRaycastResult raycastResult,
            int x,
            int y,
            int z,
            int value,
            out bool Placed) {
            bool Placed1 = false;
            if (JsInterface.handlersDictionary.TryGetValue("OnMinerPlace", out List<Function> functions)) {
                foreach (Function function in functions) {
                    Placed1 |= JsInterface.Invoke(
                            function,
                            miner,
                            raycastResult,
                            x,
                            y,
                            z,
                            value
                        )
                        .AsBoolean();
                }
            }
            Placed = Placed1;
        }

        public override bool OnPlayerSpawned(PlayerData.SpawnMode spawnMode, ComponentPlayer componentPlayer, Vector3 position) {
            if (JsInterface.handlersDictionary.TryGetValue("OnPlayerSpawned", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, spawnMode, componentPlayer, position);
                }
            }
            return false;
        }

        public override void OnPlayerDead(PlayerData playerData) {
            if (JsInterface.handlersDictionary.TryGetValue("OnPlayerDead", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, playerData);
                }
            }
        }

        public override void ProcessAttackment(Attackment attackment) {
            if (JsInterface.handlersDictionary.TryGetValue("ProcessAttackment", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, attackment);
                }
            }
        }

        public override void CalculateCreatureInjuryAmount(Injury injury) {
            if (JsInterface.handlersDictionary.TryGetValue("CalculateCreatureInjuryAmount", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, injury);
                }
            }
        }

        public override void OnProjectLoaded(Project project) {
            if (JsInterface.handlersDictionary.TryGetValue("OnProjectLoaded", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function, project);
                }
            }
        }

        public override void OnProjectDisposed() {
            if (JsInterface.handlersDictionary.TryGetValue("OnProjectDisposed", out List<Function> functions)) {
                foreach (Function function in functions) {
                    JsInterface.Invoke(function);
                }
            }
        }
    }
}

#elif __IOS


using Engine;
using GameEntitySystem;
using JavaScriptCore;
using static JavaScriptCore.JSValue;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Game {
    public class JsModLoader : ModLoader {
        public JSContext JSContext;
        public override void OnMinerDig(ComponentMiner miner, TerrainRaycastResult raycastResult, ref float DigProgress, out bool Digged) {
            bool Digged1 = false;
            if (JsInterface.handlersDictionary.TryGetValue("OnMinerDig", out List<JSValue> functions)) {
                float DigProgress1 = DigProgress;
                foreach (JSValue function in functions) {
                  Digged1 =  function.Call(JSValue.From(JSValue.FromObject(miner),JSContext), JSValue.From(JSValue.FromObject(raycastResult),JSContext), JSValue.From(DigProgress1,JSContext)).ToBool();
                }
            }
            Digged = Digged1;
        }

        public override void OnMinerPlace(ComponentMiner miner,
            TerrainRaycastResult raycastResult,
            int x,
            int y,
            int z,
            int value,
            out bool Placed) {
            bool Placed1 = false;
            if (JsInterface.handlersDictionary.TryGetValue("OnMinerPlace", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                    Placed1 |= function.Call(JSValue.From(JSValue.FromObject(miner),JSContext), JSValue.From(JSValue.FromObject(raycastResult),JSContext), JSValue.From(x,JSContext), JSValue.From(y,JSContext), JSValue.From(z,JSContext), JSValue.From(value,JSContext)).ToBool();
                }
            }
            Placed = Placed1;
        }

        public override bool OnPlayerSpawned(PlayerData.SpawnMode spawnMode, ComponentPlayer componentPlayer, Vector3 position) {
            if (JsInterface.handlersDictionary.TryGetValue("OnPlayerSpawned", out List<JSValue> functions)) {
                foreach (var function in functions) {
                    function.Call(JSValue.From(JSValue.FromObject(spawnMode), JSContext), JSValue.From(JSValue.FromObject(componentPlayer), JSContext), JSValue.From(JSValue.FromObject(position), JSContext));
                }

            }
            return false;
        }

        public override void OnPlayerDead(PlayerData playerData) {
            if (JsInterface.handlersDictionary.TryGetValue("OnPlayerDead", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                    function.Call(JSValue.From(JSValue.FromObject(playerData), JSContext));
                }
            }
        }

        public override void ProcessAttackment(Attackment attackment) {
            if (JsInterface.handlersDictionary.TryGetValue("ProcessAttackment", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                    function.Call(JSValue.From(JSValue.FromObject(attackment), JSContext));
                }
            }
        }

        public override void CalculateCreatureInjuryAmount(Injury injury) {
            if (JsInterface.handlersDictionary.TryGetValue("CalculateCreatureInjuryAmount", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                    function.Call(JSValue.From(JSValue.FromObject(injury), JSContext));
                }
            }
        }

        public override void OnProjectLoaded(Project project) {
            if (JsInterface.handlersDictionary.TryGetValue("OnProjectLoaded", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                    function.Call(JSValue.From(JSValue.FromObject(project), JSContext));
                }
            }
        }

        public override void OnProjectDisposed() {
            if (JsInterface.handlersDictionary.TryGetValue("OnProjectDisposed", out List<JSValue> functions)) {
                foreach (JSValue function in functions) {
                    function.Call();
                }
            }
        }
    }
}
#endif
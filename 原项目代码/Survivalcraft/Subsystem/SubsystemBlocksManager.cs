using GameEntitySystem;
using TemplatesDatabase;
using static Game.BlocksManager;

namespace Game {
    public class SubsystemBlocksManager : Subsystem {
        //以ClassName, BlockContent的形式存储和读取方块信息

        /*流程：
         * 在加载世界时Load调用（需要将这个调整为高优先级加载）
         * BlocksManager加载原版和所有mod的静态ID方块
         * 调用SubsystemBlocksManager.CallAllocate()调用分配动态ID方块
         * BlocksManager加载项目声明的动态ID方块
         * BlocksManager加载剩下的动态ID方块
         * PostProcess进行后处理
         */

        //BlocksManager存储的是类名Name，SubsystemBlockManager存储的也是类名
        public Dictionary<string, int> DynamicBlockNameToIndex = new();

        public ValuesDictionary m_savedValuesDictionary;

        public override void Initialize(Project project, ValuesDictionary valuesDictionary) {
            base.Initialize(project, valuesDictionary);
            DynamicBlockNameToIndex.Clear();
            m_savedValuesDictionary = valuesDictionary;
            InitializeBlocks(this);
            PostProcessBlocksLoad();
            CraftingRecipesManager.Initialize();
        }

        public virtual void CallAllocate() {
            //int tick1 = Environment.TickCount;
            for (int i = SurvivalCraftBlockCount + 1; i < 1024; i++) {
                string blockName = m_savedValuesDictionary.GetValue(i.ToString(), string.Empty);
                if (!string.IsNullOrEmpty(blockName)) {
                    DynamicBlockNameToIndex[blockName] = i;
                }
                /*
                if (!String.IsNullOrEmpty(fullName))
                {
                    foreach(var blockAllocateData in BlocksAllocateData)
                    {
                        if(blockAllocateData.Block.GetType().FullName == fullName)
                        {
                            DynamicBlockNameToIndex[blockAllocateData.Block.GetType().FullName] = i;
                            Log.Information("加载方块：" + fullName + ", Index = " + i);
                            break;
                        }
                    }
                }*/
            }
            //int tick2 = Environment.TickCount;
            //Engine.Log.Information("加载项目方块系统耗时" + (tick2 - tick1).ToString() + "ms");
        }

        public override void Save(ValuesDictionary valuesDictionary) {
            foreach (KeyValuePair<string, int> item in DynamicBlockNameToIndex) {
                valuesDictionary.SetValue(item.Value.ToString(), item.Key);
            }
        }
    }
}
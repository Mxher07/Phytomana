//init.js - 在JS引擎初始化后执行

//导入命名空间
var Game = importNamespace("Game");
var Engine = importNamespace("Engine");
var GameEntitySystem = importNamespace("GameEntitySystem");

//预定义方法
function getProject() {
    return Game.GameManager.Project;
}

function findSubsystem(name) {//根据名字寻找特定Subsystem，名字不带Subsystem
    let project = getProject();
    if (!project) {
        return null;
    }
    return project.FindSubsystem(name, false);
}

//键盘事件
var keyDownHandlers = new Array();//每次键盘按键被按下时执行
/*keyDownHandlers.push((key)=>{//参数是字符串
    Engine.Log.Information("Key Down :" + key);
});*/
var keyUpHandlers = new Array();//每次键盘按键弹起时执行
/*keyUpHandlers.push((key)=>{//参数是字符串
    Engine.Log.Information("Key Up: " + key);
});*/

//以下是ModLoader
var frameHandlers = new Array();//窗口每次刷新都会执行
/*frameHandlers.push(()=>{//无参数，无返回值
    let a = findSubsystem("Players");
    if (a != null) {
        Engine.Log.Information(a.PlayersData.Count);//在日志输出当前玩家数量
    }
});*/
var OnMinerDigHandlers = new Array();//当Miner挖掘方块时执行
/*OnMinerDigHandlers.push((miner, raycastResult, DigProgress) => {//ComponentMiner miner, TerrainRaycastResult raycastResult, ref float DigProgress
    return false;
});*/
var OnMinerPlaceHandlers = new Array();//当Miner放置方块时执行，任一返回true后不执行原放置操作
/*OnMinerPlaceHandlers.push((miner, raycastResult, x, y, z, value) => {//ComponentMiner miner, TerrainRaycastResult raycastResult, int x, int y, int z, int value
    return false;
});*/
var OnPlayerSpawnedHandlers = new Array();//玩家出生时执行
/*OnPlayerSpawnedHandlers.push((spawnMode, componentPlayer, position) => {//PlayerData.SpawnMode spawnMode, ComponentPlayer componentPlayer, Vector3 position，无返回值
});*/
var OnPlayerDeadHandlers = new Array();//玩家死亡时执行
/*OnPlayerDeadHandlers.push((playerData) => {//PlayerData playerData，无返回值
});*/
var ProcessAttackmentHandlers = new Array();//在攻击时执行
/*ProcessAttackmentHandlers.push((attackment) => {//Attackment attackment，无返回值
});*/
var CalculateCreatureInjuryAmountHandlers = new Array();//计算生物收到伤害的量
/*CalculateCreatureInjuryAmountHandlers.push((injury) => {//Injury injury，无返回值
});*/
var OnProjectLoadedHandlers = new Array();//当Project被加载时执行
/*OnProjectLoadedHandlers.push((project) => {//Project project，无返回值
})*/
var OnProjectDisposedHandlers = new Array();//当Project被卸载时执行
/*OnProjectDisposedHandlers.push(() => {//无参数，无返回值
});*/
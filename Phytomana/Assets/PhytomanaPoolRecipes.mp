<!--
  Phytomana 魔力池配方（外部配方文件）。一个文件即可为魔法池追加物品转化配方，
  规定「什么物品 + 多少魔力 → 什么产物」，无需改动源码。

  用法：把原料物品丢进魔法池，池内魔力足够时逐件转化，产物从池中弹出。

  每个 <Recipe>：
    Ingredient  原料方块类名（与代码类名一致），每种物品只能有一条配方
    Result      产物方块类名（与代码类名一致）
    ResultCount 每转化 1 件原料的产物数量，默认 1（可省略）
    ManaCost    每转化 1 件原料消耗的魔力，默认 0（可省略）
-->
<ManaPoolRecipes>
  <Recipe Ingredient="IronIngotBlock" Result="ManaIngotBlock" ResultCount="1" ManaCost="300" />
  <Recipe Ingredient="IronBlock" Result="ManaBlock" ResultCount="1" ManaCost="3000" />
  <Recipe Ingredient="DiamondChunkBlock" Result="ManaDiamondChunkBlock" ResultCount="1" ManaCost="400" />
  <Recipe Ingredient="DiamondBlock" Result="ManaDiamondBlock" ResultCount="1" ManaCost="4000" />
</ManaPoolRecipes>
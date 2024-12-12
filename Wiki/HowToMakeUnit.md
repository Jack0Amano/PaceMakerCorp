# Unitの制作方向
----  
#### 索敵と発見について  
敵UnitのPlayer発見に関してなるべくUnit内で完結するように。またRayCastが同一フレームに重複して出されないようにUnitsControllerでそのタイミングを調整する。  
しかしNPCと複数のPlayerの射線をすべて計算すると負荷が大きい。そのためActiveなPlayerとEnemyの射線のみ計算  
その後にActiveなPlayerに射線とEnemyからの被発見を保存しておいてAttributeTurnが終わる際に通信して仲間に伝える行動をする  
  
Item  
----
Unitに装備できるものは
- MainWeapon (アサルトライフルやスナイパーライフル、SAM等)
- Primary Weap (ハンドガンや小型な武器)
- Pouch (グレネードや回復薬)
- Backpack (食料など必要品)
on
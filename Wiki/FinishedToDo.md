# 終了したTODO  
  
- CounterAttackで攻撃する際のモーションとダメージ量、counterattack可能な武器の区別　(22/11/19)
- CounterAttack可能なサブ武器を所持している場合はそれに切り替えて反撃　(22/11/19)
- 敵が行動中の際にCameraをPlayer側から追う形に変更 (22/11/22)
- 被攻撃の際に敵がPlayerを強制発見するロジック (22/11/23)
- **敵の移動する際に振動しているようになるバグ**
- Counterattackを見越したAIの構築  (22/11/23~25)
- ForcastAiを利用したAIのDebug (22/11/25)
- AIのDebug用のメッセージ (22/11/23)
- CanvasをWorldSpaceにしてタブレットに表示されている風に変える (22/11/27)
- TabletCanvasを持ってくるアニメーション (22/11/27)
- Canvasの裏を光らせてディスプレイ風にする (22/11/28)
- MapSceneは立体画像風に (22/12/1)
- イベントの会話を[ノード風に](TalkEvent)
- TabletCanvas表示中はOverlayを消す (22/11/27)
- Lose画面とWin状態のTabletの位置分け (22/11/28)
- **SRPG部分との統合**
- SRPGのUIをWorldSpaceCanvasに対応した形に治す (22/12/26)
- SRPGのInfoControllerUIをTableに写っている形に直す 具体的にTextMeshproをCanvasに置くのではなく3Dで置く (~2022/12/31)
- Route-Crossing方式のマップの最短険路検索装置 (22/12/09)
- 最短経路検索装置の残り (Vector3-Vector3は完了, Vector3-Crossing, Crossing-Vector3の2つとCrossing-Crossingを綺麗にする) (22/12/09)
- Squadの選択を行えるように (22/12/19)
- 上記で得られた経路を進むSquadの簡単な移動アニメーション MoveToを (22/12/19)
- **MainMapの移動の際にアニメーションが進むとFPSが落ち込む**  answer 主にraycastが重くなっているためasyncにして10FPSでraycastを更新するようにした
- ミニフィグのアニメーション
- GameControllerの廃止とGameManagerへの統合 (InstantiateなClassが複数あると使いにくい)　(23/1/08)
- `MainMapController.EndEventAction(object, EventScene.AsyncEvents)`でSideMessageを表示しているが、ミニフィグからメッセージを出すようにする
- MapSpawnシステムを現在のMainMapControllerの形に適応するように直す (23/2/13)
- Node形式のStoryとEventの実装 (~23/2/13)
- LoadSaveDataの実装 (~23/2/13) 
  
**2023/2/14~** およそ2週間  
- EncountでのTacticsSceneへの画面遷移 (23/2/14)
- EncountLocationからTacticsSceneをID指定 ``Spawn.EncountEventArgs``にIDをstring指定 (23/2/14)
- PreparePanelMapのUIの作り直し (23/2/16)
- MainMapでのItem選択 (23/2/23)
- Result画面後にMapに戻る遷移と結果の移送 (23/2/23)
- 特殊な戦闘を行う場合 Event側のSpawnからtacticsSceneを指定する (23/2/23)
- Eventの発動タイミングの追加 (~23/2/26)
        - Prepare前
        - Prepare後、Battle前
        - Battle終了後、Result前
        - Result終了後、MainMap再表示の最初

**およそ1週間**
- EventをOnBattleで表示している時にはTPSControlからのUserControlを止める (23/2/27)
- ResultでMainMapを再度有効化するときにAwakeが呼ばれている (メモリ的に破棄されたのか？) の場合GameManagerからLoad系で既に行われている部分を除いてAwakeするようにする (23/2/27)
- Map->TacticsなどのScene遷移時の暗転等Load画面  Scene遷移のトランジション (23/2/28)
- Result画面とPrepare画面でのStartButtonで完了していないのに押せる状態を治す  (23/2/28)
- MainMapSceneでのUnitの補給の実装 (実装はしてあるのでこれを新システムに適用) (23/3/2)
- GameSettingからReloadする際の処理 (23/3/1)
- Result画面の結果によるUI変更の実装とその実装 (23/3/1)
        - 負けた場合 (Retry, Return to base)
        - 買った場合 (Retry, Return to base, Return to Map)
- MainMapSceneでの時間表示UI (23/3/2)
- MainMapSceneでのInfoPanelの復元 (23/3/3)
- Squadが歩いた際のSupply消費の増加 (23/3/3)
- Squadが基地に戻ったときのSupplyの回復期間 100%になるまでの早送り (23/3/8)
- SquadInfoCardを選択したときSquadをInteractiveにする (23/3/10)
- 早送りの場合の時間の高速アニメーション (23/3/10) 現在は10分ごとにHandlerを発火しているがこれでもかなり早いためx60では 1時間でもいいかもしれない
- ミニフィグは2D表示に (23/3/13)
- MainMapのRoadのObjectの作成とRayCastの設定 UnitがRoad上に移動できるように (6/14)
- MainMapでの各Object作成 (6/14)  
- 最短経路検索装置から出力された経路を表すメッシュの動的生成　経路はRoad上に止まれなくしたためCheckPoint間のものの表示非表示 (23/6/21)
**EventGraph系** (23/2/13 ほぼ終了 細かいNodeの作成等随時行う物のみ)  
- TimeTrigger, ItemTriggerの２つからTrigger系を実装
- TimeTrigger系はGameManagerから時間を取得する
- ItemTrigger系はGameManagerからData系にアクセスして所持アイテムを取得
- Spawn系はSpawnControllerにGameManagerから送って内容を送る
- 各ControllerにOutputを送った際にNodeのIDを記録。EventIDとNodeIDを終了済みEventとして保存。EventIDとNodeIDは保存系に1つずつしか置かない
- Message系のNodeはMessageControllerにEventGraphからのOutputを送り、選択が行われる場合はInputをGraphに入れる。
  
SelectEquipWindowはMainMapのUnitDetailでItemを装備するためのWindowだがScrollViewを使っており古い。SelectItemWindowはOSAを使っており新しいが、Tactics画面のPrepareのみに設置している状況でMainMapに移植しなければならない。  
又、SelectItemWindowにはGeneralなclassであるTabUIを採用しているが、このTabの使用例はこのSelectItemWindowのみでありより一般的なTabBarを採用すること。
さらにその際にPopUpWindow(Windowを下から持ち上げる形でポップアップさせるClass)が使用されておりこれを消去すること。  
PopUpWindowはTactics.UI.Overlay.UnitListWindow(すべてのUnitの状態を見るやつ)に使用されているが、これはTabletCanvasに移動させる。この際にSelectItemWindowと同様にFadeのみの簡単なアニメーションで出現させる。（とりあえず複雑なアニメーションは後日ということで)  
同じようなPopUpSelectionWindowというものがあるが、これは単にTabシステムとWindowを合体させただけのものであり採用したclassは前述の通り廃止予定のSelectEquipWindowのみである。これも消すこと。  
  
Escを押した際のDataWindowはMainMapとTacticsで同様なものを使用するため、SceneManagerにこれを作成すること。  
アイテムの購入をアンロック方式にしたため、アイテムを購入すれば全部隊で使用可能になった。これをバランス取るためにアイテムの導入と使用コストバフ・デバフを調整する必要が出た。  
また、一部のUIは依然としてアイテムの個数を計算する方式になっているためこれをアンロック方式に変更する。  
Squadの出撃を行うとMainMapのUIが消え、カーソルにSquadがついていく形になる。この状態で出撃可能なLocationをクリックするとSquadの出撃がその選択位置から行われる。  
そこらへんの実装が手つかず  
イベントの進行はNodeとこれに紐づけたIDとID指定の文章の形に早いとこ直す。これがないと多分旧式のXML方式では時間がかかる。  
- Squadが移動しているときTimeが早送りになる、具体的に数時間でMapの端からはしまで行けないように (23/6/25)
- Squadの移動カーソルでLocationの上に何時間かかるかのUI (23/6/25)
- 敵部隊の活動予想 敵がスポーンするとある程度時間が経つとlocationが赤くなっていく  通信塔みたいなの立ててそこにある光を赤くしていくとか (6/29)
- 周辺の敵を排除していくことによって上の光はだんだんオレンジ色っぽくなっていく (6/29)
- TacticsでのBeforeAfterBattleでのイベントの監視
- Squadの帰還  (23/07/11)
- 複数Squadの出撃のBugfix (23/07/11)
- Squadが同一Locationにいる場合の補給アイコンと補給、時間の早送り (23/07/11)
- MortarのAnimationCurveの調整と最短距離と最長距離の設定 (23/11/10)
- MOrtarの変更によってarcControllerのarcの描写が変わったためGrenadeのThrowタイプを変更する (23/11/14)
- ` EndMortarTypeGimmickMode`終了後のTurnの実行を実装 (23/11/17) 
- Mortarの加害範囲の設定と攻撃時の加害範囲内にあるUnitへDamageを与える (23/11/14)
- 爆発位置から各UnitまでRaycastを放つことでUnitまでの攻撃が通っているか確認 (23/11/14)
- Mortarの発射時カメラの工夫 最も高い位置までカメラが追従して行って敵を見下ろす形で停止 ダメージを与えている様子を映す
- Mortarで破壊できるGimmickが存在する 別Mortar, 機銃, 土のうなど
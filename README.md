# Peace Maker Corp.のWik 

主に後に使える知識やノートを作成する  
  
[MarkDownチートシート](https://qiita.com/Qiita/items/c686397e4a0f4f11683d)    
[How to start to make Games](Wiki/makeGames.md)  
__**警告** DokuWikiのファイルは最終行に改行を２回行わないとUTF-8で読み込まれない__  
__最終行には改行を２回行うこと__
  
Scene Planning
----
[Tacticsの設計](TacticsPlanning] )  
[Mapの設計](Wiki/MapPlanning.md)  
[Mapの操作と設計](Wiki/MapControl.md)  
[ルートの検索](Wiki/FindWays.md)  
[EventNodeGraph](Wiki/EventNodeGraph.md)  
[Figma](https://www.figma.com/files/recent?fuid=1213346488270654052)  

[Makeup Project](Wiki/Makeup.md)  
[Postprocessing](Wiki/Postprocessing.md)  
[Animation](Wiki/HowToAnimation.md)

[Python on Blender](Wiki/PythonBlender.md)  
[Iconの作成とか案出しにAIを使ってもいいかも](https://www.bing.com/images/create?FORM=GENILP)  
[How to build Start Scene](Wiki/StartScene.md)

[Reference](Wiki/Reference/Main.md)  
[Command](Wiki/Command.md)

Amply shader editorで影がなんか濃くて変になるとき  
A. Texture Sampleノードを選択し、画面左に表示されるパラメーターを変更していきます。 Default TextureをWhiteからBumpに変更し、UnpackNormalMapとAttributes欄のNormalにチェックを入れます。   
ノーマルマップ用の設定に切り替えた際、デフォルトではノーマルマップのScaleは1に設定されていますが、ScaleにFloatノードを接続することでマテリアルのインスペクタから値を調整できるようになります。  
[元記事](https://styly.cc/ja/tips/textureexample_rakurai_shader/)  
  
**UnityのAnimatorでEntry->SubStateMachineにした時、SubStateMachine内のEntry-Default->Stateに遷移が動かず別のに行く時**  
最初のEntry->SubStateMachineのDefaultの遷移をもう一度つけ直すこと  
この最初の遷移はSubStateMachine内のStateを対象に紐づけするため、SubStateMachine内のStateのDefaultEntryTransitionに関係なく以前transition先として登録されたStateに遷移する   
  
*文字サイズに関して*   
27inchのモニターで40cm離れたところから見る間隔で見やすいサイズにする  
現在の32inchのモニターでは

Articles
----
創作における知識等  
- チャールズ・ミルズ [人種契約](https://www.amazon.co.jp/gp/product/B00KNPMS2K/ref=dbs_a_def_rwt_hsch_vapi_taft_p1_i0)
        - 人種差別と社会的制限
- ジョン・ロールズ [正義論](https://www.amazon.co.jp/%E6%AD%A3%E7%BE%A9%E8%AB%96-%E6%94%B9%E8%A8%82%E7%89%88-%E3%82%B8%E3%83%A7%E3%83%B3%E3%83%BB%E3%83%AD%E3%83%BC%E3%83%AB%E3%82%BA-ebook/dp/B09RF3RYJP/ref=tmm_kin_swatch_0?_encoding=UTF8&qid=&sr=)
- [ベトナム戦争](http://www10.plala.or.jp/shosuzki/edit/asia/vietnam/vietnam3.htm)
- [アールデコ](https://ja.wikipedia.org/wiki/%E3%82%A2%E3%83%BC%E3%83%AB%E3%83%BB%E3%83%87%E3%82%B3)
        - 南ベトナムの1960年代の特徴としてアメリカの影響を受けたアールデコ装飾を施した建築物が挙げられる
- [歴史](Wiki/History.md)

  
TODO
----  
[終わったTODO](Wiki/FinishedToDo.md)  

EndTactics画面でのUnitの非表示化 (影がTabletに写って見にくい)  
MainMapでのF5での簡易セーブ & 時間ごとTacticsに移る際ののオートセーブ  
Tactics中のStartCanvasControllerの設定の表示  
StartCanvasControllerの名称変更  
MainMapでSpawnPointを通り過ようとしてtacticsに移行した際にSquadの位置がおかしくなる  
Loadした際の最初のFindEvent done
DebugControllerの削除とDebugConsoleへの移行 Done

- 負けてContinueする場合のMapでのSquadの位置 
- 負けた場合の選択肢として 
- "Retry" Mapに戻らずTacticsをもう一度最初から行う
- "Load Before Battle" エンカウントする直前に戻る (Squadが最後に通過したSpawnPointでクイックセーブこれを読み込み)　**二次目標** 
- "Return to base" 負けたSquadを基地に戻し費用等は戻らない
- SaveDataのリストに表示する内容 (Map内時間、実時間、ロックされたデータ、OperatingArea、
- 各UIの枠などのImageの作成
- Tacticsシーンでの敵のSpawnは兵科をSpawn位置に指定してこれになるべく沿う形での配置を行う
- EasyモードでのTacticsSceneでのクイックセーブロード
- Eventにおけるループの実装（複数回実行するイベント　デフォルトのスポーンとか）
- Tacticsシーンでの戦車の移動は固定位置の選択式になる これを実装
- カメラの位置を変更
- 地点のクリックで移動
- 移動した場合の車線のUI表示
- ``SpawnSquadData``でのUnitのAIの違いを指定する
- 敵戦車のAI
- Tacticsシーンのバグ取り (多分AI周りにある)
- Tacticsのデフォルトの音楽再生と、Eventからの音楽の指定  
- Squadのアニメーション 歩く 待機 戦闘開始 敵の出現  
- チャット形式の喋るのではなくTableのミニフィグから吹き出しを (Map上のミニフィグは2隊が限界にするためそこもどうにか)
- MapのBuildingのマテリアル作成
- Mapを草や木でごまかす  
- Squadの物資は後何時間活動できるかという値、戦闘を始めると活動時間が減っていく
- Prepareでの物資の使用で活動時間が減っていく
- Prepare画面で今はD&Dする形式だが出撃するものにチェックを付ける形にする
- 対象はPrepare画面 Result画面 (敗北の場合はタブレットが捨てられて割れている風にする
- SRPG MapのOceanShaderの作成
- SRPGMapの霧VFXの作成
- PrepareでUnitを選択したときに右にVietnam War battle map風のMapUIを表示し、Unitを選択すると打ち込んだ封のアニメーションとともにUnit名がmapに書き連ねられる
- Unity謹製の[ToonShader](https://docs.unity3d.com/ja/Packages/com.unity.toonshader@0.8/manual/GettingStarted.html)を設定

**戦車**
- 戦車の操作はTPSではなく上空からの見下ろし地点でのTile選択式
- Tileのクリックの実装 raycastか？
- TileはOverlayの形で発光する 侵入可と不可の位置を
- 現在はTileの侵入可能はバーで表示しているがこれの内側にタイルを表示 Objectのみを透過する
- 戦車の通貨によって破壊されるObjectの設定
- 移動で衝突したObjectを取得 TagでDestructibeを設定したものが壊れる 
- GimmickObjectでも同じようなDestructiveが存在するが基本これらは同じtagがつけられる

**FORCUS**
- FORCUSモードを開始した状態で攻撃可能なのに見えていない敵がいる場合これを壁を突き抜けて見えるようにする
- 上の機能は主に飛び出し攻撃を行う際にFORCUSモードで攻撃可能なのが見えないためこれを可視化
- CameraからUnitの各部位にRaycastを発射しObjectとUnitを対象としてUnitがなにかに遮られている場合上の透視モードに移行する

**迫撃砲**
- 残弾数の実装
- 残弾数のUI表示
- 爆発位置のUnitを赤く表示 Unitのみを対象としてステッカーを貼り付ける
- そのステッカーは赤色の透明で できるなら味方Unitから観測されている敵に対してはステッカーが壁を突き抜けて見えるようにする
- 敵UnitがMortarを使用する際に敵が設置したものしか利用しない
- 使用していないMortarの場合はこれを利用する遊撃部隊が存在する 
- 迫撃砲の攻撃による破壊はGimmick以外にも適用される (Done)
- tileControllerが生成時にGimmickを取得するが このときと同じようにtagのDestructible Objectも取得しておく GimmickObjectコンポーネントを紐付け(Done)
- 迫撃砲の攻撃時に破壊されるものの距離判定にIsDestructible listのobjectを参照して破壊アニメーションを出す アニメーションのみ (Done)
- 迫撃砲は被ダメージで破壊される際に使用者にExtraDamageを設定

**破壊可能なObject**
- 破壊可能Objectはgimmick以外にもあるためIsDestructibleではなくTagのDestructibleにする Tile形式のこのタイプではGimmickコンポーネントのほうがいい
- DestructibleControllerを作り内部の`SoftyDestructible`と`HardyDestructible`のtagのchildObjectsを取得
- Gimmickコンポーネントもつかず、tagのみの破壊可能ObjectはGameに関係ない基本賑やかし要員のため実装は遅くて良い
- 破壊して通行可能になるtileや射線の通るのを付ける予定だが、まだその時期じゃない

**土のうの処理**  
- 土のうに貼り付ける位置は左右で決まっている (Done)  
土のうの左右にTag AlongWall Layer ObjectのColliderを持つ透明板を設置  
土のうにPanelObjectを登録  
- 土のうに張り付いた際にUnitCOnのUseGimmickに登録 (Done)  
ThirdPersonCharacterのFollowingGimmickに登録 土のうなど壁ギミックと武器ギミックで異なる処理をするため  
- Rifle系で攻撃された際には myUnit-Sandbag-Enemyの角度を計算して角度が90度以上であれば土のう越しに攻撃とみなし命中率を下げる (実装済みだがDebugまち)  
Unitがカバーとして使えるobjectはすべてCoverGimmick.csをつける (Done)  
UnitControllerのGimmickにSandbagが登録されている場合RightLeftWallを参照して適用  
- 手榴弾等のGimmickを破壊できない攻撃に対しては爆発位置が90度以上であれば攻撃無効とみなす (Done)
- Mortar等のGImmickを破壊できる攻撃に対してはUnitへの攻撃は通らないが土のうはDestroiedとなり使用できなくなる (Done) 
- その際に使用しているUnitはGimmickの使用が中止され棒立ち状態に移行する
- 土のうに張り付いている場合はUnitは中腰モーション
- 中腰モーションになっている場合は敵からの発見度の上昇が低い
- 土のう等のGimmickを使用している場合はcounterAttackを行わない (Done)
- CounterAttackの際にNPCが侵入なら止めて攻撃 (Done)
- CounterAttackしないUnitを止める (Done)
- その旨を示すUI (Done)
- 敵AIが土のうのSafePositionに移動した際にTriggerが接していなくても隠れるモーションに移行する
- Wallに張り付く挙動はEキーで操作する TPSATestの方に合わせる

**環境変化**
- 夜と昼の発見度合いの違いを実装
- 朝などは霧の発生をTacticsに反映
- 霧が発生していることはMapにて各地点での霧のEffectをつけることで表現
- 雨が発生しているのをtacticsに実装 
- 霧 > 雨の夜 > 夜 > 朝=夕方=雨の昼間 > 昼間 の順に
- 時間の適用とTactics内での時間経過 (Done)
- Tactics内でたった時間をMapに戻る際に反映させる

**壁等への張り付きモーション**
- ~~張り付きしている際に攻撃を受けた場合、張り付き箇所-Unit-Enemyの角度が90度に近い場合、カバーに隠れた敵を狙っているとして命中率を下げる~~
- 張り付きしている状態からターン終了した際のテスト (Done)
- **張り付きしている状態で角に差し掛かった際にここから飛び出し打ちするようなFORCUSモード**
- 飛び出しうちモードはAIも使用するため飛び出しうち可能な箇所にPointをつけそれに十分近い場合これに紐付けられた飛び出し撃ち箇所から攻撃を行う事ができる
- 攻撃の際に飛び出し内箇所に移動するアニメーション TPSATestとの統合で飛び出し撃ちが実装可能
- UnitのCursorのサイズと侵入検知の厳密化 (Done)
- Unitの所在地検知はEnterExitのNOrtificationのみで管理しCollider等の実際の位置検知は初期配置の際にしか使わない (23/11/24)  
Unitが外側にいる場合などでColliderの検知がうまく行わない事があったため  

**Unitの持つスキル**
- Unitは固有のスキルを持っておりこれによって能力値が決まる
- スキルはある程度決まったのを処理する

**Tacticsの色々**
- Tileの囲いにEffectをつける
- 武器の表示に対人対戦車表示のアイコンをつける
- ForcusModeのバグ 武器選択ボタンを押すとForcusになる問題 移動しながらGrenadeをForcusすると移動が止まらない問題 (Done)
- 与えるダメージのその発射元の即時判別可能、攻撃の移動方法(直射、曲射、Throw、etc.)、攻撃時の被ダメージアニメーションの種別、エフェクトのアタッチ、破壊できるGimmickの種別
- FOCUSでGimmickに張り付いている敵のAnimationが終わらないのはRotationWithoutAnimationがGimmick張り付きで終わらないため 
- TPSAnimationTestとの統合 張り付き状態から的に対する壁からの離脱&もとに戻る挙動
- 上記のダメージ種別でGimmickに張り付いているUnitの被ダメージ時の挙動を定義する (Done)
- CounterAttackableのプロパティ取得とOnCoverIconの表示非表示、bulletの数の減少 (23/12/13)
- Tacticsの地面の線はすべて白線風で、進入不可等の様々なアイコンも白線風
- UnitがGimmickを使用してターン終了したときに RemainActionCount==0ならGimmickから離れる (23/12/21)
- UnitがGImmickを使用してターン終了後再びターンが始まった際にBottomUIがGimmickの欄ではなくItemsの内容が表示されるバグ (23/12/21)
- MortarとTurn周りのバグ (23/12/26)
- EndGame周りのバグ取り
- **Dayの経過とSupplyの計算がしにくいので出撃回数とコスト制にした方がいいかも**  
道を移動するたびにCost-1でSupplyは回数券制 10Supplyあれば 10回分の移動が可能であるというように  
その場合はSquadがMap上で待機しているときのSupplyの減少は起こらない  

**MapのUI**  
- Unitの詳細ボタンを押すと止まるバグ
  
**Prepare**  
準備画面でのスポーンポイント設定

**武器とアイテム**  
- 武器のremainingUseCountが0のときはIconを強調 FOCUS出来ないようにする (Done)
  
**二次目標**
- mainMapのLight Tableの上のMapのライトと 執務室内のライトのLayer変更 スカイマップはMapのみ適用するようにする
- TableWeatherControllerの実装 
- 各メインの戦闘イベントのダイアグラム型表示 いわゆるすべてのイベントとルートを探索しやすく
- この部分から過去回想型でイベントをもう一回行える
- 分岐を行う部分でのロックされたセーブデータ
- ミニフィグの会話イベント
- Chat形式の会話
- Squadを編成したときにメンバーの集合写真やスナップショットが自動的に作成され SquadPanelの右スペースに貼られる
- ポージングを登録しておいて Squadが戦闘を行った時のPrepareパネルに入った際に裏でこれを撮影しておく
- 一部の写真の場面は特定Unitの組み合わせで実行される
- 敵Unitの巡回のあとなどが足跡で表示される 偵察スキルで足跡が線に表示される等のビジュアル化
- Tacticsにおける雨の表現はAnimationShaderと合わせなければならずかなり奥深いので虹目標として後回し
  
Tactics
----
- [Unitの作成方向](Wiki/HowToMakeUnit.md)  
- テスト用文章  
- Tabletのサイズ比率は[レターサイズ](https://ja.wikipedia.org/wiki/%E3%83%AC%E3%82%BF%E3%83%BC%E3%82%B5%E3%82%A4%E3%82%BA#:~:text=%E7%B8%A6%E6%A8%AA%E6%AF%94%E3%81%AF%E3%81%8B%E3%81%AA%E3%82%8A%E7%9F%AD%E3%81%8F,%E9%9D%A2%E7%A9%8D%E3%81%AF3%25%E7%8B%AD%E3%81%84%E3%80%82) 216x279  
  
MainMap
----
MainMapのTable比率は3.4x1.8  
実際の画面ピクセルは2000x1060  
家のユニットは 3x4x4が一ユニット  
ドアのサイズは1x2
  
Hint
----
- WorldSpaceのcanvasを作成する場合は目的のサイズになるまでScaleを弄ること WHを小さくするとtableViewとかに不具合が出てくる
- [Ocean Shader](https://www.youtube.com/watch?v=bl8sAEDpiTs&t=0s)
- Iconなどのサイズの小さいImageはAddressableに登録すると訳わからなくなる -> 予めImageに入れておいてsetActive(false)にしておく
  
Feature
----
1. GoogleImage とかでどのようなデザインにするか想像を膨らませて参考画像を集める
2. 必要な機能の全部を書き出し
3. その機能を動かすための技術の一覧 
- Wikiに詳細を書きながら進めることで次回の作業の流れを効率化


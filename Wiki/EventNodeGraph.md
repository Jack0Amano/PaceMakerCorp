# Node Graph 型のEventシステムの構築  
はじめに
----
 Eventを保存再生する方式としてはNodeGraphを使用したものが視覚的にまとめやすく後に使いやすい  
JsonやXMLは他のIDEでも同じように利用できるが、書く文字数が多くバクも多くなると予想される  
GraphView以外にもPlaymakerとか他の人が作ったaddonがあるが調べた感じでは自分の使いたい Event操作の特化型のNodeにはならなさそう  
LoneStarではイベントの条件の分岐とテキスト内容及びイラストの登録程度をGraphViewで操作したいだけであるため、自作のNodeGraphを作成してもそう時間はかからないと考えられる  
**作成開始時 2023/01/08** から **一週間程度**を見込む

関連リンク
----
- [GraphView完全理解した](https://qiita.com/ma_sh/items/7627a6151e849f5a0ede)
- [GraphViewと戯れる](https://virtualcast.jp/blog/2020/04/playwithgraphview/)
- [PortのOnConnectedを実装したCustomPort](https://forum.unity.com/threads/callback-on-edge-connection-in-graphview.796290/)
- [UXML要素一覧](https://light11.hatenadiary.com/entry/2020/03/31/220033)

GraphViewの問題点
----
- 未だexperimentalっぽい作り　(親切な関数群が作られてないとか)
- NodeGraphと比べると自由度は高いが実装コード量が非常に多い　そのProjectで1回のみ使用するとかなら多少不自由でもAssetを探して使う方が良い
- NodeGraphとかだとEdgeの色やAnimationCurveNodeの作成などが難しいためやる気があるならGraphViewも選択肢に入る
- PortのOnConnectedなど普通に考えたら使うだろうというコードがないためいちいち拡張クラスを定義しなければならない
- 日本語での細かいQ&Aはほぼ無い 公式英語Referenceも不親切
- グラフィカルにElementを設置できるUIBuilderなども用意されているがXcodeのInterfaceBuilderとかを触っているとうんざりする出来
- IME系統での日本語の入力をTextFieldsに行った際のバグは 2023.1.0b.1で改善されていることを確認
  
不満をあげていったが最大の強みはとにかく自由に設計できることこれに尽きる  
  
TODO
----
**23/1/9**
- GraphWindowと保存関連を大まかに作った キーボードショートカットのイベントの受け取りに手間取っている
- GraphDataContainerにNodeの中身を保存しておく Serializeするだけでいいからそう時間はかからないはず
- 保存関連の動作チェックを以降に行うこと
- GraphView系は自由度も高いがそう難しいわけじゃないとの所見 3日ほどで大まかな形はできるか
  
Logical
----
EventGraphで待つのは WaitEventNodeのTriggerの条件待ち と ImageWindowNodeのUserの選択待ち  
ImageWindowNodeの仕様として Text-Text-Text-Choose(WaitForUser) で待っている状況では既にImageWindowNodeのText&Imageは表示済み  
あとはUserの選択肢を待つ  
もし IWN01 - IWN02 - IWN03(選択待ち) のEventを作った場合 最初のIWNをViewerからExecuteした時点で ViewrのExecuteのOutputは {IWN01Out, IWN02Out, IWN03Out(Wait)}  
となる すなわちExecuteFromMIddleで処理してOutするのはIWNにおいて選択肢からNextPort選択のみである。



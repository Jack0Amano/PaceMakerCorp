# Mapの設計  
Design
----
MapがTableの上に投影されているようなイメージで  
なるべくUIはOverlayCanvasに貼り付ける形ではなくTableに表示される形。Mapが一段沈んでUIがTableの上に出てくる、その際に視点はVirtualCameraでUI用の視点に変更される。  
Tableの縁には**時計**や必要InfoUIが3Dっぽく表示される  
Mapの上でSquadは3D表示される  

URL
----
- [Sci-Fiホログラフィック風](https://realtimevfx.com/t/made-an-holographic-map-terrain-scanner-with-unity-vfx-graph-and-a-camera/18799)  
  
仮置き
----
1. StartSceneからScene読み込みでMapを表示
2.  **MainMapScene**マップの最上位クラス 主に画面遷移を担当
        - GameControllerのDataControllerの読み込みを待つ
        - MainUIControllerはMainMapのUIのすべてをコントロール
        - MainMapControllerはMapの状況
4. **InfoWindow**は選択したものなどを表示する小窓
        - 現在のInfoWindowはOverlayCanvasに貼り付けている形だが、Sai-Fiオブジェクトに
5.
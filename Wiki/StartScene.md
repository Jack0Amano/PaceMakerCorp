# Start Sceneの内容

- [Start Sceneの内容](#start-sceneの内容)
  - [どのような形にするか](#どのような形にするか)
  - [何が必要か](#何が必要か)


どのような形にするか
----
シーンというよりシームレスな移動をしたい  
イメージとしては  
すでにゲームを行っている場合はMainMapの横に置かれたタブレットにStartSceneの内容が表示されている  
初めてゲームを行う場合やSaveしたものがない場合、最後のセーブから時間がたっている場合  
出社するような形でのオープニングにつなげれるような  
本社ビルの窓口に置かれたタブレット  
GameManager側にUIを置く  
GameSettingCanvasをmainMapが表示中の場合はこれのSettingTablet側に貼る  
この場合MainMapのtableに表示されているmapはLoadの後に設置される  
すなわちMainMapのTableは使いまわし  
StartSceneの周りの3DオブジェクトはこのMainMapに置かれている  
つまりStartSceneというシーンは存在せず、MainMapSceneとGameSettingCanvasと合わせてStartSceneを表現  
  
何が必要か
----
まずMainMapのLoadをSceneの開始とともに実施するこの方式を改めて、トリガーでLoadし始めるようにする  
MainMapのMapの選択した際にTableのMapを読み込む  
MainMapのtabletに合わせる形のCanvas  
全て GameSettingCanvas -> MainMapのMap選択 -> にする  

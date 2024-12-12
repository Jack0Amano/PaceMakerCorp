# Makeup project
前提Asset
----
- URP Project
- Amplify Shader editor
- TextMeshPro Resource
- Addressables
- AI Navigation
- Cinemachine
- UnityLogging
- Optimized ScrollView Adapter
- Azure Dynamic Skybox
- DOTween pro
- Advanced INI Parser
- VisualEffectGraph
- PostProcessing
  
InputManager
----
- Information [Tab] Key or mouse button
- Return [r] Key or mouse button
- UseItem [e] Key or mosue button
  
Tags
----
- CellWall  
TacticsのWallの接続に使用している。最初のloadでconnectorObjectsがこのtagを持ったgameobjectに接していることを確認すると、そのcellどうしは隣り合っている状態になる。ロード時しか使わない。
- SoftyDestructible  
Grenadeでも破壊可能なObject
- HardyDestructible  

  
Layer
----
- Object  
射線を遮るobjectのlayer 射線が通っているかのraycastで使用 Objects と Gimmick全体に適用  
- ShootTarget  
的となるtargetのlayer 射線が通っているかのraycastで使用 主にUnitのTargetとなりうる箇所のみに適用
- Ground  
Tacticsで地面に充てられるレイヤー Grenadeの投げる先とかのlaycastをする際に使う  
- ThroughUnit  
Tacticsのレイヤー Unit相手のみ衝突しないレイヤー Grenadeとかの投げ物を投げる時に ItemとUnitが衝突してしまって思った通りの軌道を描かないため一時的にUnitと衝突しないレイヤーに切り替える  
- Unit  
TacticsのUnitのレイヤー ThroughUnitやUnit同士の衝突を管理 Unitが衝突して動けないとストレスたまるため    
  
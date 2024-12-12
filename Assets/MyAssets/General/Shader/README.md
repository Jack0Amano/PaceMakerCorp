# Shaderの設定

No.1 Ground
----
Opaque Sub Zwrite ON Low and Equal  
Pass Zwrite ON Low and Equal
  
No.2 Ground以外の建築物
----
Transparent Sub Zwrite ON Low and Equal
Pass Zwrite ON Low and Equal

No.3 Groundを透過するもの
----
Groundを貫通し、それ以外のものは貫通しない  
Transparnet Sub Zwrite ON Always  
Pass Zwrite OFF Always  

No.4 すべてを透過するもの
----
Transparnent Sub Zwrite ON Always Queue +1  
Pass Zwrite OFF Always  
  
No.3 を優先的に透過するがNo.3と同様な透過
----
Transparent Sub Zwrite ON Always Queue -1  
Pass Zwrite OFF Always  
  
No.1越しに見えるが通常時や他のObject越しには見えない
----
Transparent SubZwrite ON Greater and Equal Queue -1  
Pass Zwirte OFF Greater and Equal  

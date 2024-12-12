# Postprocessing 

Unity URPはBuild-in Pipelineとは異なるためPostprocessingのアセットは必要ない  
Cameraの`Rendering.PostProcessing`をOnにし、`Environment.ValueMask`をAllwaysにする  
Universal Render Dataファイルの`Post-processing`をEnableにする  
Bloomの設定に関しては`Intensity`(二段目)がBloomの強さとなる  
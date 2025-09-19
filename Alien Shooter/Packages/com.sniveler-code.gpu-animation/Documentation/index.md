Sniveler Code Gpu Animation
======================

1.Bake animations multiple prefabs to textures
2.Using next frame for linear translation (low fps, less texture size)
3.Use job systems for calculate animation time
4.Use Unity Entities 1.0.0-pre.15 / Graphics 1.0.0-pre.15 / Burst 1.7.4
5.InstanceShader build in shadergraph only for URP (14.0)

======================

Baker Window -> Menu/Window/SnivelerCode/AnimatorBaker

Prefabs:
	Batch Name -> Name for multiple prefabs
	Add Prefab -> Add prefab from assets with SkinnedMeshRenderer
	Expand Setting Checkbox -> Open Animator and Lods setting
	
Animator:
	From prefab button -> adds animator from prefab component (if exists)
	Animation -> select animations to bake
		Fps -> animation speed (more fps more texture size)
		Interpolation Version -> animation speed adapted from application fps
			current frame interpolation to second frame (can set 15fps)
	
Lods:
	First Lod00 is from original prefab model
	Transition -> distance in percent to the camera (value from unity LodGroup)
	"+" button -> add lod instance
	
======================
- Documentation: https://github.com/igor-karpushin/com.sniveler-code.gpu-animation
- Source Models: https://github.com/igor-karpushin/com.sniveler-code.gpu-animation

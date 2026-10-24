using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HorrorJam.EditorTools
{
    // สร้างซอยลง scene เป็น GameObject จริง — จัดวาง/เปลี่ยนหน้าตาใน Editor ได้ (ห้ามเปลี่ยนชื่อ ดู Names)
    public static class BakeWorld
    {
        const string MaterialFolder = "Assets/_Prototype/Materials";

        [MenuItem("Tools/Horror Jam/Bake World To Scene")]
        static void Bake()
        {
            if (GameObject.Find(Names.World) != null &&
                !EditorUtility.DisplayDialog("Bake World", "สร้าง World ใหม่จากโค้ดล่าสุด?\nWorld เดิมจะถูกเก็บไว้เป็น \"World (backup ...)\" (ปิดไว้) ไม่หาย", "สร้างใหม่", "ยกเลิก"))
                return;
            Rebuild();
        }

        // ไม่ลบของเดิม — เปลี่ยนชื่อเป็น backup แล้วปิดไว้ ถ้าบาสจัดวางอะไรไว้ ยังลากกลับมาใช้ได้
        public static void Rebuild()
        {
            var old = GameObject.Find(Names.World);
            if (old != null)
            {
                Undo.RecordObject(old, "Bake World");
                old.name = $"World (backup {System.DateTime.Now:MMdd-HHmm})";
                old.SetActive(false);
            }

            var world = AlleyBuilder.Build();
            PersistMaterials(world);
            PersistPostFx(world);
            Undo.RegisterCreatedObjectUndo(world, "Bake World");

            var game = Object.FindFirstObjectByType<LoopGame>();
            if (game == null)
            {
                game = new GameObject("LoopGame").AddComponent<LoopGame>();
                Undo.RegisterCreatedObjectUndo(game.gameObject, "Bake World");
            }
            var so = new SerializedObject(game);
            so.FindProperty("worldTemplate").objectReferenceValue = world;
            so.ApplyModifiedProperties();
            // แสง/หมอกคู่กับ World รุ่นนี้ — รีเซ็ตเป็นค่าตั้งต้นของโค้ดล่าสุด (ค่าเวลา/บทพูดไม่แตะ)
            Undo.RecordObject(game, "Bake World");
            game.mood = new MoodSettings();
            game.cameraSettings = new CameraSettings();
            EditorUtility.SetDirty(game);

            var scene = world.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = world;
            Debug.Log("[Horror Jam] Bake World เสร็จ — กด Play ได้เลย");
        }

        // material ที่สร้างจากโค้ดไม่ถูกเซฟลง scene → ต้องเก็บเป็น .mat ไม่งั้นเปิดใหม่จะเป็นสีชมพู
        static void PersistMaterials(GameObject root)
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/_Prototype", "Materials");

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || AssetDatabase.Contains(mats[i])) continue;
                    string path = Path.Combine(MaterialFolder, mats[i].name + ".mat").Replace('\\', '/');
                    var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (existing == null)
                    {
                        AssetDatabase.CreateAsset(mats[i], path);
                        existing = mats[i];
                    }
                    mats[i] = existing;
                }
                renderer.sharedMaterials = mats;
            }
            AssetDatabase.SaveAssets();
        }

        // VolumeProfile + effect ข้างในต้องเป็น asset เหมือนกัน (Bake ซ้ำ = เขียนทับไฟล์เดิม)
        static void PersistPostFx(GameObject root)
        {
            const string path = "Assets/_Prototype/HorrorPostFX.asset";
            var volume = root.GetComponentInChildren<UnityEngine.Rendering.Volume>(true);
            if (volume == null || volume.sharedProfile == null || AssetDatabase.Contains(volume.sharedProfile)) return;

            AssetDatabase.DeleteAsset(path);
            var profile = volume.sharedProfile;
            AssetDatabase.CreateAsset(profile, path);
            foreach (var component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            AssetDatabase.SaveAssets();
        }
    }
}

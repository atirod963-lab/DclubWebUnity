using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;

public class JigsawSlicer
{
    // ==========================================
    //  โหมด MULTIPLAYER (เน้นนัว)
    // ==========================================
    [MenuItem("Assets/หั่นจิ๊กซอว์/Multi - ด่าน 1 (3 ชิ้น : 3x1)")]
    static void SliceMultiR1() => SliceImage(3, 1);

    [MenuItem("Assets/หั่นจิ๊กซอว์/Multi - ด่าน 2 (6 ชิ้น : 3x2)")]
    static void SliceMultiR2() => SliceImage(3, 2);

    [MenuItem("Assets/หั่นจิ๊กซอว์/Multi - ด่าน 3 (9 ชิ้น : 3x3)")]
    static void SliceMultiR3() => SliceImage(3, 3);


    // ==========================================
    //  โหมด SOLO (เล่นคนเดียวชิ้นเยอะๆ)
    // ==========================================
    [MenuItem("Assets/หั่นจิ๊กซอว์/Solo - ด่าน 1 (9 ชิ้น : 3x3)")]
    static void SliceSoloR1() => SliceImage(3, 3);

    [MenuItem("Assets/หั่นจิ๊กซอว์/Solo - ด่าน 2 (12 ชิ้น : 3x4)")]
    static void SliceSoloR2() => SliceImage(3, 4);

    [MenuItem("Assets/หั่นจิ๊กซอว์/Solo - ด่าน 3 (15 ชิ้น : 3x5)")]
    static void SliceSoloR3() => SliceImage(3, 5);


    // ฟังก์ชันหลักสำหรับสั่งหั่นตามพิกัดตาราง
    static void SliceImage(int cols, int rows)
    {
        Texture2D tex = Selection.activeObject as Texture2D;
        if (tex == null)
        {
            Debug.LogError("⚠️ กรุณาคลิกเลือกไฟล์รูปภาพ (PNG/JPG) ในหน้าต่าง Project ก่อนกดหั่นครับ!");
            return;
        }

        string path = AssetDatabase.GetAssetPath(tex);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        // 1. บังคับเปลี่ยนภาพเป็นโหมด Multiple อัตโนมัติ (แก้คำผิดตรงนี้แล้วครับ!)
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        float cellWidth = tex.width / (float)cols;
        float cellHeight = tex.height / (float)rows;

        List<SpriteRect> rects = new List<SpriteRect>();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                SpriteRect rect = new SpriteRect();
                rect.rect = new Rect(c * cellWidth, r * cellHeight, cellWidth, cellHeight);
                rect.alignment = SpriteAlignment.Center;
                rect.pivot = new Vector2(0.5f, 0.5f);

                // บังคับใส่ :00 เพื่อให้ชื่อออกมาเป็น _00, _01 ... _14
                rect.name = $"{tex.name}_{r * cols + c:00}";
                rect.spriteID = GUID.Generate();
                rects.Add(rect);
            }
        }

        // 2. บันทึกพิกัดการหั่นลงไฟล์ภาพทันที
        dataProvider.SetSpriteRects(rects.ToArray());
        dataProvider.Apply();
        importer.SaveAndReimport();

        Debug.Log($"[สำเร็จ!] หั่นภาพ '{tex.name}' เป็น {cols}x{rows} (รวม {cols * rows} ชิ้น) เรียบร้อยแล้วครับ");
    }
}
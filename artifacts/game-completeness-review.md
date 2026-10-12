# ผลตรวจและกู้คืน SideScrollingEscape

## ผลล่าสุดหลังซ่อม (10 ตุลาคม 2026)

กู้ Assets ที่ขาดจากไดรฟ์ D พร้อม .meta เดิมแล้ว ตรวจ dependency ของ MainMenu และ Level1–3 ซ้ำ ไม่พบ GUID ที่ขาดในฉากและ prefab ที่ฉากเรียกใช้

ทดสอบเปิด Level1, Level2, Level3 ใน Unity Play Mode ด่านละ 3 วินาที: โหลดสำเร็จทั้งหมด แต่ละด่านมี Player 1 ตัว, Killer 2 ตัว, KeyPickup 5 ตัว, ExitDoor 1 บาน, Missing Script 0 และไม่พบ Error/Exception ในช่วงจับผล ผลนี้เป็นการทดสอบการเปิดด่าน ไม่ใช่การเล่นผ่านทุกด่านหรือการทดสอบ build

ดูผลจริงใน `unity-three-level-validation.json` และ `game-audit-summary.json`

สำรองไฟล์งานจาก `D:\SideScrollingEscape` ไว้ที่ `artifacts/preserved-from-D` และเทียบ SHA-256 ของ 18,271 ไฟล์ (34,018,438 ไบต์) ตรงทั้งหมด ไม่สำรองโฟลเดอร์ cache/ไฟล์ที่สร้างใหม่ได้: Library, Temp, obj, .vs รายละเอียดใน `d-backup-verification.json`

โปรเจกต์ที่ใช้ต่อ: `C:\Users\aonpa\OneDrive\Desktop\SideScrollingEscape\SideScrollingEscape` เปิด MainMenu ด้วย Unity 6000.4.9f1 แล้วกด Play

## บันทึกผลก่อนซ่อม (ปัญหา Tile ของฉากด้านล่างกู้คืนแล้ว)

ตรวจไฟล์โปรเจกต์ใน `SideScrollingEscape` เมื่อ 10 ตุลาคม 2026

## ข้อสรุป

มีฉากและองค์ประกอบหลักครบ 3 ด่าน แต่ยังไม่สามารถยืนยันว่าไฟล์ตัวเกมครบทั้งหมดได้ เพราะด่าน 2 และ 3 มีการอ้างอิง Tile asset ที่ไม่พบ GUID ใน metadata ของ Assets และ package cache ที่ตรวจ

| ฉาก | กุญแจที่วางจาก KeyPickup prefab | Player prefab | ExitDoor prefab | GUID ของ Tile ที่ไม่พบ |
|---|---:|---:|---:|---:|
| MainMenu | — | — | — | 0 |
| Level1: The Forsaken Cellar | 5 | 1 | 1 | 0 |
| Level2: The Stone Gaze | 5 | 1 | 1 | 156 |
| Level3: Echoes of the Blind | 5 | 1 | 1 | 324 |

156/324 คือจำนวน GUID ที่ต่างกัน ไม่ใช่จำนวนช่องแผนที่ ด่าน 2 มี 439 การอ้างอิงที่ไม่พบ และด่าน 3 มี 778 การอ้างอิงที่ไม่พบ ทั้งหมดอยู่ในฟิลด์ `m_Data` ของ Tile asset array ในไฟล์ฉาก จำนวนของสองด่านอาจมี GUID ซ้ำกันจึงไม่ควรรวมเป็นจำนวนไฟล์ขาดทั้งหมด

ตัวอย่างด่าน 2: `9fdf091e886074b4dab10f062f89ca59`

ตัวอย่างด่าน 3: `1906de1630f0579418530f42c49531eb`

ผลที่อาจเกิด: Tile บางส่วนไม่ปรากฏ และหาก Tile เหล่านั้นใช้สร้าง collision อาจกระทบพื้นหรือเส้นทาง ต้องเปิดฉากและตรวจใน Unity เพื่อยืนยันผลจริง

## สิ่งที่ตรวจพบว่ามี

- MainMenu, Level1, Level2, Level3 อยู่ใน Build Settings และเปิดใช้งาน
- Resources/GameManager.prefab อ้างถึง Level1Config → Level2Config → Level3Config ตามลำดับ แต่ละด่านกำหนด requiredKeyCount = 5
- สคริปต์ระบบผู้เล่น ศัตรู สุขภาพ ไฟฉาย ซ่อนตัว อินเวนทอรี เก็บกุญแจ ประตูออก และ UI มีอยู่และเชื่อมจากฉาก/prefab
- GameHUD มีปุ่มไปด่านถัดไปและ panel สำหรับ LevelComplete และ GameWon; GameManager มี logic ไปด่านถัดไปและสถานะจบเกม
- มีสคริปต์ระบบคำสาปด่าน 2 และระบบสุ่มตำแหน่ง/กับดักด่าน 3
- พบ C# 38 ไฟล์, ไฟล์เสียง 17 ไฟล์ และภาพต้นทาง 68 ไฟล์ตามนามสกุลที่ตรวจ (ไม่รวม sprite sub-assets)
- ไม่พบ GUID ซ้ำใน Assets, metadata ที่ไม่มีไฟล์คู่ และข้อผิดพลาดการอ่านไฟล์ระหว่างการตรวจ
- MainMenu และ dependency ของ Level1 ไม่พบ GUID ที่หาไม่ได้ในการตรวจครั้งนี้

## ขอบเขตและข้อจำกัด

ตรวจจากไฟล์และกราฟการอ้างอิง ไม่ได้เล่นผ่านทั้ง 3 ด่าน ไม่ได้ทดสอบ build เกม การมี script/prefab ไม่ยืนยันว่าการเดิน AI ปริศนา และจบเกมทำงานถูกต้องทุกกรณี ไม่ได้ตรวจ fileID ของ sub-assets ทุกตัว ไม่พบตัวเกม Windows ที่ build สำหรับส่งผู้เล่นโดยตรงในไฟล์ที่ตรวจ (exe ใน Library เป็นเครื่องมือของ packages)

การลองเล่นก่อนหน้านี้เปิด MainMenu และ Level1 ได้ แต่หยุดก่อนเก็บกุญแจหรือผ่านด่าน จึงไม่นับเป็นการทดสอบจบด่าน

มีการอ้างอิงหาไม่ได้ใน prefab แผนที่อื่นด้วย; ให้ prioritise รายการที่เชื่อมจากฉากใช้งานจริง รายละเอียดดิบอยู่ใน game-audit.json และรายการ dependency ต่อฉากอยู่ใน game-audit-summary.json

## งานที่ควรทำต่อ

กู้ Tile asset พร้อมไฟล์ .meta เดิมของด่าน 2 และ 3 เพื่อรักษา GUID จากนั้นตรวจฉากใน Unity และทดสอบเก็บกุญแจ 5 ดอก เปิดประตู ไปด่านถัดไป และจบเกมครบสามด่าน ก่อน build สำหรับส่งผู้เล่น

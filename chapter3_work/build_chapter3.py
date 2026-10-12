from copy import deepcopy
from pathlib import Path

from docx import Document
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor


SOURCE = Path(r"D:\งาน\project.docx")
OUTPUT = Path(r"C:\Users\aonpa\OneDrive\Desktop\SideScrollingEscape\project_บทที่3_ปรับปรุง.docx")
FONT = "TH SarabunPSK"


def set_run_font(run, size=16, bold=False, italic=False):
    run.font.name = FONT
    run.font.size = Pt(size)
    run.bold = bold
    run.italic = italic
    run.font.color.rgb = RGBColor(0, 0, 0)
    rpr = run._element.get_or_add_rPr()
    fonts = rpr.rFonts
    if fonts is None:
        fonts = OxmlElement("w:rFonts")
        rpr.insert(0, fonts)
    for key in ("ascii", "hAnsi", "eastAsia", "cs"):
        fonts.set(qn("w:" + key), FONT)


def format_paragraph(paragraph, *, align=WD_ALIGN_PARAGRAPH.LEFT,
                     first_indent=True, before=0, after=0, keep=False):
    paragraph.alignment = align
    fmt = paragraph.paragraph_format
    fmt.first_line_indent = Inches(0.5) if first_indent else None
    fmt.left_indent = None
    fmt.right_indent = None
    fmt.space_before = Pt(before)
    fmt.space_after = Pt(after)
    fmt.line_spacing = 1.0
    fmt.keep_with_next = keep


def add_para(doc, text, *, size=16, bold=False, italic=False,
             align=WD_ALIGN_PARAGRAPH.LEFT, first_indent=True,
             before=0, after=0, keep=False):
    p = doc.add_paragraph()
    format_paragraph(p, align=align, first_indent=first_indent,
                     before=before, after=after, keep=keep)
    set_run_font(p.add_run(text), size=size, bold=bold, italic=italic)
    return p


def add_heading(doc, text, level=1):
    size = 18 if level == 1 else 16
    before = 8 if level == 1 else 4
    return add_para(doc, text, size=size, bold=True, first_indent=False,
                    align=WD_ALIGN_PARAGRAPH.LEFT, before=before,
                    after=2, keep=True)


def add_chapter_title(doc):
    p = add_para(doc, "บทที่ 3", size=20, bold=True,
                 align=WD_ALIGN_PARAGRAPH.CENTER, first_indent=False,
                 after=0, keep=True)
    p.paragraph_format.page_break_before = True
    add_para(doc, "วิธีการดำเนินงาน", size=20, bold=True,
             align=WD_ALIGN_PARAGRAPH.CENTER, first_indent=False,
             after=12, keep=True)


def set_cell_shading(cell, fill):
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = tc_pr.find(qn("w:shd"))
    if shd is None:
        shd = OxmlElement("w:shd")
        tc_pr.append(shd)
    shd.set(qn("w:fill"), fill)


def set_cell_margins(cell, top=80, start=100, bottom=80, end=100):
    tc = cell._tc
    tc_pr = tc.get_or_add_tcPr()
    tc_mar = tc_pr.first_child_found_in("w:tcMar")
    if tc_mar is None:
        tc_mar = OxmlElement("w:tcMar")
        tc_pr.append(tc_mar)
    for name, value in (("top", top), ("start", start), ("bottom", bottom), ("end", end)):
        node = tc_mar.find(qn("w:" + name))
        if node is None:
            node = OxmlElement("w:" + name)
            tc_mar.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def set_table_borders(table):
    tbl_pr = table._tbl.tblPr
    borders = tbl_pr.find(qn("w:tblBorders"))
    if borders is None:
        borders = OxmlElement("w:tblBorders")
        tbl_pr.append(borders)
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        tag = borders.find(qn("w:" + edge))
        if tag is None:
            tag = OxmlElement("w:" + edge)
            borders.append(tag)
        tag.set(qn("w:val"), "single")
        tag.set(qn("w:sz"), "4")
        tag.set(qn("w:color"), "D9D9D9")


def add_table(doc, headers, rows, widths=None, font_size=14):
    table = doc.add_table(rows=1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = True
    set_table_borders(table)
    header = table.rows[0]
    header._tr.get_or_add_trPr().append(OxmlElement("w:tblHeader"))
    for i, label in enumerate(headers):
        cell = header.cells[i]
        set_cell_shading(cell, "1F4E78")
        set_cell_margins(cell)
        cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
        p = cell.paragraphs[0]
        format_paragraph(p, align=WD_ALIGN_PARAGRAPH.CENTER, first_indent=False)
        run = p.add_run(label)
        set_run_font(run, font_size, bold=True)
        run.font.color.rgb = RGBColor(255, 255, 255)
    for row_index, values in enumerate(rows):
        cells = table.add_row().cells
        for i, value in enumerate(values):
            cell = cells[i]
            set_cell_margins(cell)
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            if row_index % 2:
                set_cell_shading(cell, "EAF2F8")
            p = cell.paragraphs[0]
            align = WD_ALIGN_PARAGRAPH.CENTER if i == 0 else WD_ALIGN_PARAGRAPH.LEFT
            format_paragraph(p, align=align, first_indent=False)
            set_run_font(p.add_run(str(value)), font_size)
    if widths:
        for row in table.rows:
            for i, width in enumerate(widths):
                row.cells[i].width = Inches(width)
    doc.add_paragraph()
    return table


def add_figure(doc, number, title, description, placeholder):
    add_para(doc, description)
    add_para(doc, f"[แทรกภาพ: {placeholder}]", size=14, italic=True,
             align=WD_ALIGN_PARAGRAPH.CENTER, first_indent=False,
             before=6, after=4, keep=True)
    add_para(doc, f"ภาพที่ 3-{number} {title}", size=14,
             align=WD_ALIGN_PARAGRAPH.CENTER, first_indent=False,
             after=8, keep=True)


def delete_chapter_three(doc):
    body = doc._element.body
    start = None
    for child in list(body):
        if child.tag == qn("w:p"):
            text = "".join(node.text or "" for node in child.xpath(".//w:t")).strip()
            if text == "บทที่ 3":
                start = child
                break
    if start is None:
        raise RuntimeError("Chapter 3 start paragraph was not found")
    deleting = False
    for child in list(body):
        if child is start:
            deleting = True
        if deleting and child.tag != qn("w:sectPr"):
            body.remove(child)


def build():
    doc = Document(SOURCE)
    delete_chapter_three(doc)

    add_chapter_title(doc)
    add_para(doc, "การพัฒนาเกมคอมพิวเตอร์ 2 มิติแนว Side-scrolling Hide-and-Seek Horror Game ในโครงงานนี้ดำเนินการด้วย Unity Game Engine และภาษา C# โดยประยุกต์ใช้เทคนิค Finite State Machine หรือ FSM ในการควบคุมสถานะของตัวละครผู้เล่นและพฤติกรรมของศัตรู ระบบถูกแบ่งเป็นโมดูลเพื่อให้ส่วนควบคุมผู้เล่น ระบบศัตรู ระบบไอเท็ม ระบบฉาก ระบบเสียง และส่วนติดต่อผู้ใช้งานทำงานร่วมกันได้อย่างเป็นระบบและสามารถปรับปรุงต่อได้")
    add_para(doc, "เกมประกอบด้วย 3 ด่านซึ่งมีระดับความยากและค่าพารามิเตอร์แตกต่างกัน ผู้เล่นต้องสำรวจพื้นที่ เก็บกุญแจให้ครบ หลีกเลี่ยงศัตรู และเดินทางไปยังประตูทางออก โดยสามารถใช้ไฟฉาย จุดซ่อนตัว และก้อนหินสร้างเสียงล่อศัตรูเพื่อช่วยในการหลบหนี ขั้นตอนการดำเนินงานประกอบด้วยการกำหนดปัญหา การรวบรวมข้อมูล การวิเคราะห์ระบบ การออกแบบระบบ การพัฒนา การทดสอบ และการบำรุงรักษา")

    add_heading(doc, "3.1 การกำหนดปัญหา")
    sections_31 = [
        ("3.1.1 ปัญหาด้านการควบคุมตัวละครผู้เล่น", "ตัวละครผู้เล่นต้องรองรับการเดิน การวิ่ง การกระโดด การไต่บันได การซ่อนตัว การใช้ไอเท็ม การได้รับความเสียหาย และการเปลี่ยนฉาก การทำงานเหล่านี้อาจเกิดขึ้นต่อเนื่องหรือขัดแย้งกัน หากไม่มีระบบควบคุมสถานะที่ชัดเจนอาจทำให้ตัวละครรับคำสั่งในช่วงที่ไม่ควรควบคุมได้ โครงงานจึงใช้ FSM กำหนดสถานะ Normal, Hidden, Frozen, Hurt, UsingItem, Transitioning และ Dead เพื่อควบคุมสิทธิ์การรับคำสั่งและพฤติกรรมในแต่ละช่วง"),
        ("3.1.2 ปัญหาด้านพฤติกรรมของศัตรู", "ศัตรูต้องตอบสนองต่อผู้เล่นได้หลายลักษณะ ทั้งการลาดตระเวน การหันหาเสียง การตรวจสอบพื้นที่ การค้นหา การไล่ล่า และการโจมตี หากไม่มีลำดับสถานะที่ชัดเจน ศัตรูอาจเปลี่ยนพฤติกรรมอย่างไม่สมเหตุสมผลหรือไม่สามารถกลับไปลาดตระเวนได้หลังจากสูญเสียเป้าหมาย จึงใช้ Enemy FSM ควบคุมลำดับการตัดสินใจจากข้อมูลการมองเห็น การได้ยิน ระดับความสงสัย และตำแหน่งล่าสุดของผู้เล่น"),
        ("3.1.3 ปัญหาด้านระบบตรวจจับ", "ระบบตรวจจับต้องพิจารณาระยะทาง มุมมอง สิ่งกีดขวาง การเปิดไฟฉาย และเสียงที่เกิดจากกิจกรรมต่าง ๆ ของผู้เล่น การวิ่งมีความสำคัญมากกว่าการเดิน ขณะที่ก้อนหินและกับดักกระจกสร้างเสียงจากตำแหน่งอื่นเพื่อเบี่ยงเบนความสนใจของศัตรู ระบบจึงจำแนกประเภทเสียงเป็น Footstep, Sprint, Rock, Glass และ Environmental ก่อนประเมินความดัง ระยะห่าง และอายุของเสียง"),
        ("3.1.4 ปัญหาด้านตรรกะและเป้าหมายของเกม", "ผู้เล่นต้องเก็บกุญแจตามจำนวนที่กำหนดก่อนเปิดประตูทางออก ระบบต้องตรวจสอบจำนวนกุญแจ สถานะประตู ระยะการโต้ตอบ และอันตรายรอบตัวผู้เล่น การปลดล็อกต้องกดปุ่มโต้ตอบค้างไว้และยกเลิกได้เมื่อผู้เล่นปล่อยปุ่ม เดินออกจากระยะ ถูกโจมตี หรือมีศัตรูอยู่ใกล้ เพื่อให้เป้าหมายของเกมชัดเจนและมีความท้าทาย"),
        ("3.1.5 ปัญหาด้านส่วนติดต่อผู้ใช้งาน", "ผู้เล่นต้องรับรู้พลังชีวิต แบตเตอรี่ไฟฉาย พลังงานสำหรับวิ่ง จำนวนก้อนหิน จำนวนกุญแจ ข้อความโต้ตอบ และสถานการณ์อันตรายแบบเรียลไทม์ โดยข้อมูลต้องไม่บดบังพื้นที่เล่น ระบบจึงแสดง HIDDEN เมื่อซ่อนตัว แสดง DANGER เมื่อถูกไล่ล่า และใช้เสียงหัวใจร่วมกับขอบจอสีแดงเมื่อศัตรูอยู่ใกล้"),
        ("3.1.6 ปัญหาด้านการออกแบบและปรับสมดุลด่าน", "แต่ละด่านต้องมีบรรยากาศและระดับความยากต่างกัน ค่าเกี่ยวกับผู้เล่น ศัตรู และไฟฉายจึงต้องปรับได้โดยไม่แก้โค้ดหลัก โครงงานใช้ LevelConfig แยกค่าของแต่ละด่าน และออกแบบ Level 3 เป็นป่ามืดที่มีหมอก ต้นไม้หลายชั้น ทางเดินต่างระดับ กับดักเสียง และตำแหน่งกุญแจที่เปลี่ยนแปลงได้")
    ]
    for heading, text in sections_31:
        add_heading(doc, heading, 2)
        add_para(doc, text)

    add_heading(doc, "3.2 การรวบรวมข้อมูล")
    add_heading(doc, "3.2.1 ข้อมูลด้านการพัฒนาเกม", 2)
    add_para(doc, "ผู้พัฒนาได้ศึกษาการพัฒนาเกม 2 มิติด้วย Unity ระบบ Sprite และ Tilemap ระบบ Physics 2D การรับข้อมูลด้วย Unity Input System การจัดการฉาก การสร้าง UI และการใช้ ScriptableObject สำหรับจัดเก็บค่าตั้งต้นของด่าน นอกจากนี้ยังศึกษาการเขียนโปรแกรมเชิงวัตถุเพื่อแยกความรับผิดชอบของแต่ละระบบให้สามารถพัฒนาและทดสอบได้ง่าย")
    add_heading(doc, "3.2.2 ข้อมูลด้าน Finite State Machine", 2)
    add_para(doc, "ศึกษาหลักการ FSM ซึ่งแบ่งการทำงานออกเป็นสถานะและกำหนดเงื่อนไขการเปลี่ยนสถานะอย่างชัดเจน หลักการดังกล่าวถูกนำมาใช้กับตัวละครผู้เล่น ศัตรู ประตูทางออก คลังไอเท็ม และระบบความตึงเครียด เพื่อป้องกันการทำงานซ้อนกันและช่วยให้ตรวจสอบพฤติกรรมได้เป็นลำดับ")
    add_heading(doc, "3.2.3 ข้อมูลด้านเกมแนวซ่อนตัวและสยองขวัญ", 2)
    add_para(doc, "ศึกษากลไกการมองเห็นและการได้ยินของศัตรู การใช้เสียงเป็นความเสี่ยงและเครื่องมือล่อ การออกแบบจุดซ่อน การจำกัดทรัพยากรไฟฉาย และการใช้เสียงดนตรี เสียงหัวใจ หมอก แสง และฉากหลังหลายชั้นเพื่อสร้างความกดดันแก่ผู้เล่น")

    add_heading(doc, "3.3 การวิเคราะห์ระบบ")
    add_heading(doc, "3.3.1 การวิเคราะห์ระบบควบคุมผู้เล่น", 2)
    add_para(doc, "PlayerController รับคำสั่งจาก Unity Input System และควบคุมการเคลื่อนที่แนวนอน การวิ่ง การใช้ Stamina การกระโดด การไต่บันได การโต้ตอบ การเปิดไฟฉาย การซ่อนตัว และการหยุดเกม การวิ่งใช้พลังงานซึ่งลดลงระหว่างใช้งานและฟื้นกลับหลังจากหยุดพัก ส่วนระบบกระโดดมีการตรวจสอบพื้นและ Coyote Time เพื่อช่วยให้การควบคุมตอบสนองเป็นธรรมชาติ")
    add_table(doc, ["คำสั่ง", "แป้นพิมพ์", "ผลการทำงาน"], [
        ("เคลื่อนที่", "W A S D หรือปุ่มลูกศร", "เดินและกำหนดทิศทาง"),
        ("วิ่ง", "Left Shift", "เพิ่มความเร็ว ใช้ Stamina และสร้างเสียงมากขึ้น"),
        ("กระโดด", "Space", "กระโดดเมื่ออยู่บนพื้นหรือในช่วง Coyote Time"),
        ("โต้ตอบ", "E", "เก็บไอเท็ม ใช้วัตถุ หรือกดค้างเพื่อปลดล็อกประตู"),
        ("ไฟฉาย", "F", "เปิดหรือปิดไฟฉายและใช้พลังงานแบตเตอรี่"),
        ("ซ่อนตัว", "H", "เข้าและออกจากจุดซ่อน"),
        ("คลังไอเท็ม", "C", "เปิดหรือปิดหน้าต่างคลังไอเท็ม"),
        ("ก้อนหิน", "Q", "ขว้างก้อนหินเพื่อสร้างเสียงล่อศัตรู"),
        ("หยุดเกม", "Escape", "เปิดหรือปิด Pause Menu")
    ], widths=[1.2, 2.0, 3.4])

    add_heading(doc, "3.3.2 การวิเคราะห์ Player FSM", 2)
    add_table(doc, ["สถานะ", "หน้าที่"], [
        ("Normal", "สถานะปกติที่รับคำสั่งเดิน วิ่ง กระโดด และโต้ตอบ"),
        ("Hidden", "ผู้เล่นอยู่ในจุดซ่อนและถูกจำกัดการเคลื่อนไหว"),
        ("Frozen", "หยุดการเคลื่อนไหวชั่วคราวและต้องทำตามเงื่อนไขเพื่อหลุดออก"),
        ("Hurt", "สถานะตอบสนองเมื่อได้รับความเสียหาย"),
        ("UsingItem", "ล็อกคำสั่งบางส่วนระหว่างเปิดคลังหรือใช้ไอเท็ม"),
        ("Transitioning", "ล็อกการควบคุมระหว่างการเปลี่ยนด่าน"),
        ("Dead", "หยุดการควบคุมเมื่อพลังชีวิตหมด")
    ], widths=[1.5, 5.1])

    add_heading(doc, "3.3.3 การวิเคราะห์ระบบปัญญาประดิษฐ์ของศัตรู", 2)
    add_para(doc, "KillerAI ประมวลผลแนวสายตาด้วย Raycasting และประมวลผลเสียงจากคิวเหตุการณ์ โดยให้ค่าน้ำหนักตามประเภทเสียง ความดัง ระยะห่าง และเวลาที่เสียงเกิดขึ้น ศัตรูบันทึกตำแหน่งล่าสุดของผู้เล่นหรือเสียงไว้ชั่วคราว เพื่อนำไปใช้ในสถานะตรวจสอบและค้นหา การเปิดไฟฉายเพิ่มระยะที่ศัตรูสามารถตรวจพบผู้เล่นด้วยสายตา ส่วนผู้เล่นที่อยู่ในจุดซ่อนจะไม่ถูกตรวจจับตามเงื่อนไขปกติ")
    add_table(doc, ["สถานะ", "พฤติกรรม"], [
        ("Patrol", "เดินลาดตระเวนตามจุดที่กำหนด"),
        ("Alert", "หันไปยังทิศทางของเหตุการณ์ผิดปกติ"),
        ("Listening", "หยุดเพื่อพิจารณาทิศทางและความสำคัญของเสียง"),
        ("InvestigateSound", "เคลื่อนที่ไปยังต้นกำเนิดเสียง"),
        ("ConfirmSound", "ตรวจสอบบริเวณต้นกำเนิดเสียง"),
        ("Confused", "ลังเลหลังพบการใช้ก้อนหินล่อซ้ำบริเวณเดิม"),
        ("Investigate", "ไปยังตำแหน่งล่าสุดของผู้เล่น"),
        ("Search", "ค้นหาบริเวณรอบตำแหน่งล่าสุด"),
        ("Chase", "ไล่ล่าผู้เล่นที่ถูกตรวจพบ"),
        ("Attack", "โจมตีผู้เล่นเมื่ออยู่ในระยะ"),
        ("AttackRecover", "หยุดชั่วคราวหลังการโจมตี"),
        ("ReturnToPatrol", "กลับไปยังเส้นทางลาดตระเวน"),
        ("Scream", "กรีดร้องเพื่อทำให้ผู้เล่น Stun หรือ Frozen ตามการตั้งค่า")
    ], widths=[1.7, 4.9])

    add_heading(doc, "3.3.4 การวิเคราะห์ระบบไอเท็มและภารกิจ", 2)
    add_para(doc, "InventoryManager ใช้โครงสร้าง List เก็บไอเท็มที่ผู้เล่นรวบรวมและแยกกุญแจออกจากไอเท็มทั่วไป ระบบส่งเหตุการณ์ไปยัง UI เมื่อจำนวนกุญแจเปลี่ยนแปลง และตรวจสอบว่าผู้เล่นมีกุญแจครบตามค่า RequiredKeyCount หรือไม่ คลังไอเท็มเปิดด้วยปุ่ม C แสดงไอเท็มตามช่องที่กำหนด และอนุญาตให้ใช้ไอเท็มที่รองรับการใช้งาน")
    add_heading(doc, "3.3.5 การวิเคราะห์ข้อมูลระดับความยาก", 2)
    add_para(doc, "ค่าของด่านถูกจัดเก็บใน LevelConfig ซึ่งเป็น ScriptableObject ทำให้ปรับสมดุลเกมได้โดยไม่เปลี่ยนตรรกะหลัก ตารางต่อไปแสดงค่าหลักที่ใช้ในเกมเวอร์ชันปัจจุบัน")
    add_table(doc, ["พารามิเตอร์", "Level 1", "Level 2", "Level 3"], [
        ("ระดับความยาก", "Easy", "Normal", "Nightmare"),
        ("กุญแจที่ต้องใช้", "5", "5", "5"),
        ("พลังชีวิตผู้เล่น", "100", "100", "100"),
        ("ความเร็วเดิน", "4.5", "4.5", "4.5"),
        ("ความเร็ววิ่ง", "5.7", "5.7", "5.7"),
        ("แบตเตอรี่ไฟฉาย", "80", "60", "45"),
        ("ความเร็วลาดตระเวน", "2.0", "1.3", "2.8"),
        ("ความเร็วไล่ล่า", "4.0", "3.0", "7.2"),
        ("ระยะมองเห็น", "6.0", "4.55", "0"),
        ("ระยะได้ยิน", "4.0", "3.25", "10.0"),
        ("พลังโจมตี", "25", "50", "35")
    ], widths=[2.2, 1.45, 1.45, 1.45])
    add_para(doc, "Level 3 ใช้ชื่อ Echoes of the Blind และกำหนดระยะมองเห็นของศัตรูเป็นศูนย์ แต่เพิ่มระยะได้ยินเป็น 10 หน่วย จึงเน้นการหลบเลี่ยงด้วยการควบคุมเสียงมากกว่าการหลบแนวสายตา")

    add_heading(doc, "3.4 การออกแบบระบบ")
    add_heading(doc, "3.4.1 การออกแบบข้อมูลนำเข้า", 2)
    add_para(doc, "ข้อมูลนำเข้าประกอบด้วยคำสั่งจากแป้นพิมพ์ ตำแหน่งและทิศทางของผู้เล่น สถานะของ Player FSM ระยะห่างจากศัตรู สถานะไฟฉาย เหตุการณ์เสียง จำนวนกุญแจ ไอเท็มที่เก็บได้ และข้อมูล LevelConfig ของด่านปัจจุบัน ข้อมูลเหล่านี้ถูกส่งไปยังโมดูลที่รับผิดชอบเพื่อประมวลผลตามเงื่อนไข")
    add_heading(doc, "3.4.2 การออกแบบกระบวนการทำงาน", 2)
    for item in [
        "1) GameManager โหลด LevelConfig และฉากของด่านปัจจุบัน",
        "2) PlayerController รับคำสั่งและตรวจสอบ PlayerState ก่อนดำเนินการ",
        "3) ระบบคำนวณการเคลื่อนที่ Stamina การกระโดด การไต่บันได และการโต้ตอบ",
        "4) FootstepSystem หรือวัตถุในฉากส่งเหตุการณ์เสียงไปยัง KillerAI",
        "5) KillerAI ประเมินการมองเห็น เสียง และระดับความสงสัยก่อนเปลี่ยนสถานะ",
        "6) InventoryManager ตรวจสอบไอเท็มและจำนวนกุญแจ",
        "7) ExitDoor ตรวจสอบเงื่อนไขและความคืบหน้าการปลดล็อก",
        "8) UIManager และระบบเสียงอัปเดตผลลัพธ์ให้ผู้เล่นรับรู้",
        "9) GameManager เปลี่ยนด่านหรือแสดงผลการจบเกมเมื่อเงื่อนไขสำเร็จ"
    ]:
        add_para(doc, item, first_indent=False)

    add_heading(doc, "3.4.3 การออกแบบเอาต์พุต", 2)
    figures = [
        (1, "หน้าจอเมนูหลัก", "หน้าจอเมนูหลักประกอบด้วยปุ่ม Start สำหรับเริ่มเกม Options สำหรับเปิดหน้าตั้งค่าเสียง และ Quit สำหรับออกจากเกม โดยใช้โทนสีเข้มให้สอดคล้องกับบรรยากาศสยองขวัญ", "Main Menu ที่มีปุ่ม Start Options และ Quit"),
        (2, "หน้าจอตั้งค่าเสียง", "หน้าจอตั้งค่าประกอบด้วย Slider สำหรับ Master Volume, SFX Volume และ Music Volume โดยค่าที่ปรับถูกเก็บด้วย PlayerPrefs เพื่อเรียกใช้ในการเปิดเกมครั้งต่อไป", "Options Menu และ Slider เสียงทั้งสามรายการ"),
        (3, "หน้าจอระหว่างเล่น", "HUD แสดงชื่อด่าน ระดับความยาก จำนวนกุญแจ พลังชีวิต แบตเตอรี่ไฟฉาย Stamina จำนวนก้อนหิน และข้อความโต้ตอบ รวมทั้ง HIDDEN และ DANGER ตามสถานการณ์", "HUD ขณะเล่นที่เห็นข้อมูลหลักครบ"),
        (4, "การวิ่งและระบบ Stamina", "เมื่อกด Left Shift ระหว่างเคลื่อนที่ ผู้เล่นจะวิ่งเร็วขึ้นและ Stamina ลดลง แถบจะเปลี่ยนสีเมื่อเหลือพลังงานต่ำและฟื้นกลับหลังหยุดใช้", "ผู้เล่นกำลังวิ่งพร้อมแถบ Stamina"),
        (5, "การโต้ตอบกับไอเท็ม", "เมื่อเข้าใกล้วัตถุที่โต้ตอบได้ ระบบแสดง Prompt เพื่อแจ้งคำสั่ง ผู้เล่นกด E เพื่อเก็บไอเท็มหรือใช้งานวัตถุ", "Interaction Prompt ขณะเข้าใกล้กุญแจ"),
        (6, "หน้าจอคลังไอเท็ม", "ผู้เล่นกด C เพื่อเปิดและปิดคลังไอเท็ม หน้าต่างแสดงช่องไอเท็มที่เก็บได้และรองรับการเลือกใช้ไอเท็มบางประเภท", "Inventory จริงจากเกม"),
        (7, "ระบบซ่อนตัว", "เมื่อผู้เล่นอยู่ใกล้จุดซ่อนและกด H ระบบเปลี่ยนสถานะเป็น Hidden พร้อมแสดง HIDDEN บนหน้าจอ", "ผู้เล่นในจุดซ่อนและข้อความ HIDDEN"),
        (8, "ระบบไฟฉายและแบตเตอรี่", "ผู้เล่นกด F เพื่อเปิดหรือปิดไฟฉาย แบตเตอรี่ลดลงระหว่างใช้งาน และการเปิดไฟฉายเพิ่มระยะตรวจพบด้วยสายตาของศัตรู", "แสงไฟฉายและแถบ Battery"),
        (9, "การใช้ก้อนหินล่อศัตรู", "ผู้เล่นขว้างก้อนหินเพื่อสร้างเสียงจากตำแหน่งอื่น ศัตรูรับเสียงประเภท Rock และเดินไปตรวจสอบ หากใช้ซ้ำบริเวณเดิมหลายครั้งศัตรูจะลดความเชื่อถือของเสียงล่อ", "ก้อนหินตกและศัตรูเดินไปตรวจสอบ"),
        (10, "การตรวจพบและไล่ล่าผู้เล่น", "เมื่อศัตรูตรวจพบผู้เล่น ระบบเปลี่ยนเข้าสู่ Chase HUD แสดง DANGER และระบบเสียงปรับเข้าสู่บรรยากาศการไล่ล่า", "Killer ไล่ล่าผู้เล่นและ HUD แสดง DANGER"),
        (11, "การได้รับความเสียหายและ Game Over", "เมื่อศัตรูอยู่ในระยะโจมตี ระบบลดพลังชีวิตของผู้เล่น หากพลังชีวิตเป็นศูนย์ Player FSM เปลี่ยนเป็น Dead และแสดงหน้าจอ Game Over", "ผู้เล่นได้รับความเสียหายและหน้าจอ Game Over"),
        (12, "การปลดล็อกประตูทางออก", "หลังเก็บกุญแจครบ ผู้เล่นต้องกด E ค้างที่ประตูจนความคืบหน้าครบ 100 เปอร์เซ็นต์ การปลดล็อกจะถูกยกเลิกเมื่อปล่อยปุ่ม ออกจากระยะ ถูกโจมตี หรือมีศัตรูอยู่ใกล้", "ข้อความ Hold E to unlock และเปอร์เซ็นต์"),
        (13, "หน้าจอผ่านด่านและจบเกม", "เมื่อปลดล็อกประตูสำเร็จ ระบบแสดง Level Complete และโหลดด่านถัดไป หากผ่านด่านสุดท้ายระบบแสดง Game Won", "หน้าจอ Level Complete และ Game Won"),
        (14, "ระบบคำสาปใน Level 2", "Level 2 มีสถานะคำสาป Safe, Exposed, Warning, Critical, Petrified และ Recovering โดย HUD แสดงแถบคำสาป ภาพบดบัง และข้อความเตือนตามระดับอันตราย", "Curse HUD ของ Level 2"),
        (15, "สภาพแวดล้อมป่าของ Level 3", "Level 3 ออกแบบเป็นป่ามืดโดยใช้หมอก ต้นสนระยะไกล ต้นไม้และพุ่มไม้ชั้นกลาง รวมถึงพื้นหญ้าและเส้นทางต่างระดับเพื่อสร้างความลึกของฉาก", "ภาพรวมฉากป่าของ Level 3"),
        (16, "กับดักเสียงใน Level 3", "กับดักกระจกทำงานเมื่อผู้เล่นเดินชน โดยเล่นเสียงแตก เปลี่ยนลักษณะวัตถุ และส่งเสียงประเภท Glass ไปยัง KillerAI", "ก่อนและหลังเหยียบกับดักกระจก"),
        (17, "ระบบแจ้งเตือนเมื่อศัตรูอยู่ใกล้", "เมื่อศัตรูอยู่ในระยะเตือน เกมเล่นเสียงหัวใจและแสดงขอบจอสีแดง ความถี่และความเข้มเพิ่มขึ้นตามระดับอันตราย", "ขอบจอสีแดงขณะ Killer อยู่ใกล้")
    ]
    for number, title, description, placeholder in figures:
        add_figure(doc, number, title, description, placeholder)

    add_heading(doc, "3.5 การพัฒนาระบบ")
    dev_sections = [
        ("3.5.1 สถาปัตยกรรมของระบบ", "ระบบประกอบด้วย PlayerController, PlayerHealth, FlashlightController, HidingSystem, FootstepSystem, KillerAI, InventoryManager, ExitDoor, GameManager, UIManager, AudioManager และ TensionDirector โดย PlayerController รับข้อมูลจากผู้เล่นและส่งเหตุการณ์ให้ระบบที่เกี่ยวข้อง FootstepSystem ส่งเหตุการณ์เสียงให้ KillerAI ขณะที่ InventoryManager ส่งจำนวนกุญแจให้ UIManager และ ExitDoor ส่วน GameManager ควบคุมสถานะเกมและการเปลี่ยนฉาก"),
        ("3.5.2 การพัฒนา Player FSM", "Player FSM ใช้สถานะ Normal, Hidden, Frozen, Hurt, UsingItem, Transitioning และ Dead เพื่อควบคุมการรับคำสั่ง Normal เป็นสถานะที่อนุญาตการควบคุมทั่วไป Hidden ใช้เมื่ออยู่ในจุดซ่อน Frozen และ Hurt ใช้หยุดหรือจำกัดการควบคุมชั่วคราว UsingItem ป้องกันคำสั่งที่ขัดแย้งระหว่างใช้ไอเท็ม Transitioning ใช้ระหว่างเปลี่ยนฉาก และ Dead ใช้เมื่อพลังชีวิตหมด"),
        ("3.5.3 การพัฒนา Enemy FSM", "Enemy FSM รับข้อมูลจากระบบการมองเห็นและการได้ยิน จากนั้นประเมินความสงสัยและเลือกสถานะ เสียง Glass และ Sprint มีความสำคัญมากกว่าเสียง Footstep ทั่วไป ศัตรูเก็บตำแหน่งล่าสุดไว้ชั่วคราวเพื่อใช้ตรวจสอบและค้นหา หากไม่พบเป้าหมายจะกลับไปยังเส้นทางลาดตระเวน"),
        ("3.5.4 การพัฒนาระบบความตึงเครียด", "TensionDirector รวมสถานะของศัตรูทุกตัวเป็น Calm, Suspicious, Hunted, Escaped และ Cooldown ข้อมูลนี้ใช้ควบคุมระดับความตึงเครียด ดนตรีไล่ล่า เสียงหัวใจ ภาพเตือน และตัวคูณความเร็วของศัตรูระหว่างการไล่ล่า"),
        ("3.5.5 การพัฒนาระบบไอเท็มและประตูทางออก", "InventoryManager เก็บไอเท็มใน List และส่งเหตุการณ์เมื่อจำนวนกุญแจเปลี่ยน ExitDoor มีสถานะ Locked, Ready, Unlocking, Open, Transitioning และ Completed การใช้ FSM กับประตูช่วยควบคุมการปลดล็อกแบบกดค้างและรองรับการยกเลิกเมื่อสถานการณ์ไม่ปลอดภัย"),
        ("3.5.6 การพัฒนาด่าน Level 3", "Level 3 ใช้ Tilemap และ Sprite ที่มีอยู่ในโครงการจัดวางเป็นป่า โดยแบ่งเป็นชั้นหมอกและต้นไม้ระยะไกล ชั้นต้นไม้และพุ่มไม้ระยะกลาง และชั้นพื้นสำหรับการเล่น ระบบ Level3RandomEvents ค้นหาพื้นที่เดินได้และกระจายกุญแจกับกับดักระหว่างเส้นทางชั้นล่างและชั้นบน เพื่อลดการรวมตัวของวัตถุในตำแหน่งเดียว"),
        ("3.5.7 การจัดการเงื่อนไขพิเศษ", "ระบบป้องกันการกระโดดซ้ำกลางอากาศ รองรับการกระโดดออกจากบันได ล็อกการเคลื่อนไหวขณะซ่อน ใช้ไอเท็ม เปลี่ยนฉาก หรือเสียชีวิต ลบเสียงที่หมดอายุจากหน่วยความจำของศัตรู ลดประสิทธิภาพการล่อด้วยก้อนหินซ้ำ และยกเลิกการปลดล็อกประตูเมื่อผู้เล่นอยู่ในอันตราย")
    ]
    for heading, text in dev_sections:
        add_heading(doc, heading, 2)
        add_para(doc, text)
    add_figure(doc, 18, "แผนภาพสถาปัตยกรรมของระบบ", "แผนภาพแสดงการรับข้อมูลจาก Input System ผ่าน PlayerController การส่งเหตุการณ์ไปยังระบบไอเท็ม ระบบเสียง KillerAI UI และ GameManager", "System Architecture Diagram")
    add_figure(doc, 19, "แผนภาพ Player FSM", "แผนภาพแสดงสถานะของผู้เล่นและเหตุการณ์สำคัญที่ทำให้เปลี่ยนระหว่าง Normal, Hidden, Frozen, Hurt, UsingItem, Transitioning และ Dead", "Player FSM Diagram")
    add_figure(doc, 20, "แผนภาพ Enemy FSM", "แผนภาพแสดงกระบวนการตั้งแต่ Patrol การรับรู้เสียงหรือการมองเห็น การตรวจสอบ การค้นหา การไล่ล่า การโจมตี และการกลับไปลาดตระเวน", "Enemy FSM Diagram")
    add_figure(doc, 21, "แผนภาพสถานะความตึงเครียด", "แผนภาพแสดงสถานะ Calm, Suspicious, Hunted, Escaped และ Cooldown ซึ่งควบคุมเสียงและภาพเตือนผู้เล่น", "Tension State Diagram")

    add_heading(doc, "3.6 การทดสอบระบบ")
    add_para(doc, "การทดสอบใช้กรณีทดสอบหรือ Test Case กำหนดข้อมูลนำเข้า ขั้นตอน และผลลัพธ์ที่คาดหวังอย่างชัดเจน โดยต้องบันทึกผลจริงจากการเล่นทดสอบก่อนระบุสถานะผ่านหรือไม่ผ่าน ตารางในหัวข้อนี้จึงเว้นผลจริงไว้สำหรับกรอกหลังดำเนินการทดสอบ")
    add_heading(doc, "3.6.1 การทดสอบระบบควบคุมพื้นฐาน", 2)
    add_table(doc, ["รหัส", "รายการทดสอบ", "ขั้นตอน", "ผลที่คาดหวัง", "ผลจริง/สถานะ"], [
        ("TC-01", "การเดิน", "กด A D หรือปุ่มลูกศร", "ผู้เล่นเคลื่อนที่และหันถูกทิศ", "รอบันทึกผล"),
        ("TC-02", "การวิ่ง", "กด Shift พร้อมเคลื่อนที่", "ความเร็วเพิ่ม Stamina ลด และเกิดเสียง Sprint", "รอบันทึกผล"),
        ("TC-03", "การกระโดด", "กด Space บนพื้นและกลางอากาศ", "กระโดดได้ครั้งเดียวและไม่กระโดดซ้ำกลางอากาศ", "รอบันทึกผล"),
        ("TC-04", "การไต่บันได", "กดขึ้นหรือลงเมื่ออยู่บนบันได", "ผู้เล่นไต่บันไดและกระโดดออกได้", "รอบันทึกผล"),
        ("TC-05", "ไฟฉาย", "กด F และเปิดทิ้งไว้", "ไฟฉายสลับสถานะและแบตเตอรี่ลดลง", "รอบันทึกผล")
    ], widths=[0.7, 1.2, 1.5, 2.4, 1.0], font_size=13)

    add_heading(doc, "3.6.2 การทดสอบ Player FSM", 2)
    add_table(doc, ["รหัส", "การเปลี่ยนสถานะ", "เงื่อนไขทดสอบ", "ผลที่คาดหวัง", "ผลจริง/สถานะ"], [
        ("TC-FSM-P01", "Normal > Hidden", "เข้าใกล้จุดซ่อนและกด H", "ผู้เล่นซ่อนและ HUD แสดง HIDDEN", "รอบันทึกผล"),
        ("TC-FSM-P02", "Hidden > Normal", "กด H เพื่อออกจากจุดซ่อน", "กลับมาเคลื่อนที่ได้ตามปกติ", "รอบันทึกผล"),
        ("TC-FSM-P03", "Normal > Hurt > Normal", "ให้ศัตรูโจมตีโดย HP ยังเหลือ", "HP ลดและกลับสู่ Normal หลังช่วง Hurt", "รอบันทึกผล"),
        ("TC-FSM-P04", "Normal > Frozen > Normal", "รับผล Frozen และทำเงื่อนไขหลุด", "หยุดเคลื่อนที่และกลับมาควบคุมได้", "รอบันทึกผล"),
        ("TC-FSM-P05", "Normal > UsingItem > Normal", "เปิดและปิด Inventory", "คำสั่งการเล่นถูกล็อกและคืนกลับถูกต้อง", "รอบันทึกผล"),
        ("TC-FSM-P06", "Normal > Transitioning", "ปลดล็อกประตูสำเร็จ", "ล็อกการควบคุมระหว่างเปลี่ยนด่าน", "รอบันทึกผล"),
        ("TC-FSM-P07", "Hurt > Dead", "รับความเสียหายจน HP เป็นศูนย์", "เข้าสู่ Dead และแสดง Game Over", "รอบันทึกผล")
    ], widths=[0.9, 1.5, 1.7, 2.2, 0.9], font_size=13)

    add_heading(doc, "3.6.3 การทดสอบ Enemy FSM", 2)
    add_table(doc, ["รหัส", "การเปลี่ยนสถานะ", "เงื่อนไขทดสอบ", "ผลที่คาดหวัง", "ผลจริง/สถานะ"], [
        ("TC-FSM-E01", "Patrol > Listening", "สร้างเสียงในระยะได้ยิน", "ศัตรูหยุดลาดตระเวนและฟังเสียง", "รอบันทึกผล"),
        ("TC-FSM-E02", "Listening > InvestigateSound", "รอจนศัตรูเลือกเสียง", "ศัตรูเคลื่อนไปยังต้นกำเนิดเสียง", "รอบันทึกผล"),
        ("TC-FSM-E03", "InvestigateSound > ConfirmSound", "ศัตรูถึงตำแหน่งเสียง", "ศัตรูตรวจสอบบริเวณดังกล่าว", "รอบันทึกผล"),
        ("TC-FSM-E04", "Patrol/Investigate > Chase", "ให้ศัตรูตรวจพบผู้เล่น", "ศัตรูไล่ล่าและ HUD แสดง DANGER", "รอบันทึกผล"),
        ("TC-FSM-E05", "Chase > Attack", "ผู้เล่นอยู่ในระยะโจมตี", "ศัตรูโจมตีและลด HP", "รอบันทึกผล"),
        ("TC-FSM-E06", "Attack > AttackRecover", "รอหลังการโจมตี", "ศัตรูพักตามช่วง cooldown", "รอบันทึกผล"),
        ("TC-FSM-E07", "Chase > Investigate/Search", "หลบออกจากการตรวจจับ", "ศัตรูตรวจตำแหน่งล่าสุดและค้นหา", "รอบันทึกผล"),
        ("TC-FSM-E08", "Search > ReturnToPatrol", "รอจนหมดเวลาค้นหา", "ศัตรูกลับเส้นทางลาดตระเวน", "รอบันทึกผล"),
        ("TC-FSM-E09", "Listening > Confused", "ขว้างก้อนหินซ้ำบริเวณเดิม", "ศัตรูเข้าสู่ Confused ตามเงื่อนไข", "รอบันทึกผล"),
        ("TC-FSM-E10", "สถานะทั่วไป > Scream", "ทำให้ศัตรูที่ตั้งค่า Scream เข้าเงื่อนไข", "ผู้เล่นถูก Stun หรือ Frozen", "รอบันทึกผล")
    ], widths=[0.9, 1.5, 1.7, 2.2, 0.9], font_size=13)

    add_heading(doc, "3.6.4 การทดสอบระบบเกมและ Level 3", 2)
    add_table(doc, ["รหัส", "รายการทดสอบ", "ขั้นตอน", "ผลที่คาดหวัง", "ผลจริง/สถานะ"], [
        ("TC-06", "เก็บกุญแจ", "เก็บกุญแจหนึ่งชิ้น", "จำนวนกุญแจและ Inventory อัปเดต", "รอบันทึกผล"),
        ("TC-07", "ประตูยังล็อก", "ใช้ประตูก่อนกุญแจครบ", "แสดงจำนวนกุญแจที่ยังขาด", "รอบันทึกผล"),
        ("TC-08", "ปลดล็อกประตู", "เก็บครบและกด E ค้าง", "ความคืบหน้าครบและผ่านด่าน", "รอบันทึกผล"),
        ("TC-09", "ยกเลิกการปลดล็อก", "ปล่อย E เดินออก หรือถูกโจมตี", "ความคืบหน้าถูกยกเลิก", "รอบันทึกผล"),
        ("TC-10", "กับดักกระจก", "เหยียบกับดักใน Level 3", "เกิดเสียง Glass และศัตรูตรวจสอบ", "รอบันทึกผล"),
        ("TC-11", "การสุ่มวัตถุ", "เข้า Level 3 หลายรอบ", "กุญแจและกับดักกระจายตามจุดที่เดินได้", "รอบันทึกผล"),
        ("TC-12", "เสียงหัวใจ", "เข้าใกล้ศัตรู", "ความถี่เสียงและขอบจอแดงเพิ่มตามอันตราย", "รอบันทึกผล"),
        ("TC-13", "จบเกม", "ผ่าน Level 3", "ระบบแสดง Game Won", "รอบันทึกผล")
    ], widths=[0.7, 1.2, 1.6, 2.3, 1.0], font_size=13)

    add_heading(doc, "3.6.5 การทดสอบโดยผู้ใช้งาน", 2)
    add_para(doc, "การทดสอบโดยผู้ใช้งานมีวัตถุประสงค์เพื่อประเมินความถูกต้อง ความเสถียร ความเข้าใจง่ายของการควบคุม ความชัดเจนของ UI ความเหมาะสมของระดับความยาก และบรรยากาศของเกม ผู้ทดสอบให้คะแนนด้วยมาตราส่วน 5 ระดับ ได้แก่ 5 ดีมาก 4 ดี 3 ปานกลาง 2 น้อย และ 1 น้อยมาก")
    add_para(doc, "ผลการประเมินจะวิเคราะห์ด้วยค่าเฉลี่ยและส่วนเบี่ยงเบนมาตรฐาน ทั้งนี้ต้องระบุจำนวนผู้ทดสอบ คุณสมบัติของกลุ่มตัวอย่าง วันที่หรือช่วงเวลาทดสอบ และผลคะแนนจริงก่อนนำเสนอข้อสรุป")

    add_heading(doc, "3.7 การบำรุงรักษาระบบ")
    maintenance = [
        ("3.7.1 การบำรุงรักษาเพื่อแก้ไขข้อผิดพลาด", "ตรวจสอบและแก้ไขปัญหาการเปลี่ยนสถานะของผู้เล่นและศัตรู การติดขอบพื้น การเดินบนสะพาน การตรวจจับจุดซ่อน การปลดล็อกประตู และการติดตามกล้องในพื้นที่ต่างระดับ"),
        ("3.7.2 การบำรุงรักษาเพื่อปรับตัว", "ปรับ Input System ความละเอียด อัตราส่วนภาพ ขอบเขตกล้อง และค่าประสิทธิภาพ เพื่อให้รองรับอุปกรณ์หรือสภาพแวดล้อมที่แตกต่างกัน"),
        ("3.7.3 การบำรุงรักษาเพื่อพัฒนาเพิ่มเติม", "เพิ่มชนิดศัตรู ไอเท็ม กับดัก ด่าน และสถานะ FSM ใหม่ได้ผ่านโครงสร้างโมดูล พร้อมปรับความยากของด่านผ่าน LevelConfig โดยไม่เปลี่ยนตรรกะหลัก"),
        ("3.7.4 การบำรุงรักษาเชิงป้องกัน", "ทดสอบทุกด่านหลังแก้ไขระบบส่วนกลาง ตรวจสอบ Unity Console ไม่ให้มีข้อผิดพลาด สำรอง Scene และ Prefab ก่อนแก้ไข และอัปเดตแผนภาพ ตาราง รวมถึงภาพหน้าจอในรายงานให้ตรงกับเกมเวอร์ชันล่าสุด")
    ]
    for heading, text in maintenance:
        add_heading(doc, heading, 2)
        add_para(doc, text)

    # Normalize added empty paragraphs and request Word field refresh when opened.
    settings = doc.settings._element
    update_fields = settings.find(qn("w:updateFields"))
    if update_fields is None:
        update_fields = OxmlElement("w:updateFields")
        settings.append(update_fields)
    update_fields.set(qn("w:val"), "true")

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    doc.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    build()

from pathlib import Path
from docx import Document
from docx.shared import Inches,Pt,RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn
from PIL import Image
import re

r=Path(__file__).resolve().parent
assets=r.parents[1]/'SideScrollingEscape'/'Assets'
src=r/'project_บทที่3_เพิ่มแอนิเมชันตัวละครและKiller.docx'
out=r/'project_บทที่3และ4_เพิ่มแอนิเมชันตัวละครและKiller.docx'
d=Document(src)
anchor=next(p for p in d.paragraphs if p.text.startswith('4.9 ผลการทดสอบ'))
for para in d.paragraphs:
    for old,new in [('4.9 ผลการทดสอบ','4.10 ผลการทดสอบ'),('4.10 ผลการประเมิน','4.11 ผลการประเมิน'),('4.11 สรุปผล','4.12 สรุปผล')]:
        if para.text.startswith(old):
            txt=para.text.replace(old,new); para.runs[0].text=txt
            for run in para.runs[1:]: run.text=''
def p(text='',bold=False,center=False,page=False,size=16):
    x=anchor.insert_paragraph_before()
    f=x.paragraph_format; f.line_spacing=1;f.space_after=Pt(5);f.keep_with_next=bold;f.page_break_before=page;f.widow_control=True
    x.alignment=WD_ALIGN_PARAGRAPH.CENTER if center else WD_ALIGN_PARAGRAPH.LEFT
    if not bold and not center:f.first_line_indent=Inches(.5)
    run=x.add_run(text);run.bold=bold;run.font.name='TH SarabunPSK';run.font.size=Pt(size);run.font.color.rgb=RGBColor(0,0,0)
    for key in ['ascii','hAnsi','eastAsia','cs']:run._element.get_or_add_rPr().rFonts.set(qn('w:'+key),'TH SarabunPSK')
    return x
def sprite(path,caption):
    # Extract four consecutive frames from the actual 128-pixel sprite sheet.
    im=Image.open(assets/path)
    dst=r/(path.parent.name+'_'+path.stem+'_frames.png')
    strip=im.crop((0,0,512,128)); box=strip.getbbox(); strip.crop((0,max(0,box[1]-4),512,min(128,box[3]+4))).save(dst)
    x=p(center=True);x.paragraph_format.keep_with_next=True
    x.add_run().add_picture(str(dst),width=Inches(5.5))
    p(caption,center=True)

p('4.9 ผลการพัฒนาระบบแอนิเมชันของตัวละคร',True,page=True,size=18)
p('ผลการพัฒนาในส่วนนี้ต่อเนื่องจากการออกแบบ Animator ในหัวข้อ 3.5.8–3.5.11 โดยตรวจสอบไฟล์ Animation Clip, Animator Controller และสคริปต์ที่เชื่อมต่อกัน ภาพประกอบเป็นเฟรมจาก Sprite Sheet ที่ใช้ในโครงการ มิใช่ภาพหน้าจอขณะทดลองเล่น จึงใช้แสดงลักษณะของท่าทางและผลการจัดเตรียมคลิป ส่วนผลการตอบสนองระหว่างเล่นต้องยืนยันด้วยการทดสอบเพิ่มเติม')
p('4.9.1 ผลการจัดทำแอนิเมชันของผู้เล่น',True,size=18)
sprite(Path('Sprites/Player/Idle.png'),'ภาพที่ 4-16 ตัวอย่างสี่เฟรมแรกของแอนิเมชัน Idle ของผู้เล่น')
sprite(Path('Sprites/Player/Walk.png'),'ภาพที่ 4-17 ตัวอย่างสี่เฟรมแรกของแอนิเมชัน Walk ของผู้เล่น')
p('ภาพที่ 4-16 และ 4-17 แสดงท่ายืนและท่าเดินที่นำไปใช้กับ PlayerIdle.anim และ PlayerWalk.anim ตามลำดับ ทั้งสองคลิปกำหนด Sample Rate เท่ากับ 12 และเปิด Loop Time เพื่อเล่นวนขณะตัวละครคงอยู่ในสถานะเดิม โดยคลิป Idle มีช่วงเวลา 0.75 วินาที ส่วน Walk มีช่วงเวลา 1 วินาที')
p('จากการตรวจสอบโครงสร้าง PlayerAnimator พบสถานะ Idle และ Walk เชื่อมด้วย isWalking ชนิด Bool และ PlayerController มีคำสั่ง SetBool(IsWalkingHash, IsMoving) ตรงกับชื่อพารามิเตอร์ ผลที่ยืนยันได้คือมีคลิป สถานะ และจุดส่งค่าจากสคริปต์ครบสำหรับการเลือกท่ายืนและเดิน ส่วนความถูกต้องของภาพที่แสดงเมื่อกดหรือปล่อยปุ่มยังต้องตรวจใน Play Mode')
p('Controller ของผู้เล่นในรุ่นที่ตรวจสอบยังไม่มีสถานะ Run แยกต่างหาก จึงรายงานเฉพาะผลการจัดทำ Idle และ Walk การเพิ่มความเร็ววิ่งในระบบควบคุมไม่ได้เป็นหลักฐานว่ามีแอนิเมชัน Run เพิ่มขึ้นด้วย')

p('4.9.2 ผลการจัดทำแอนิเมชันของ Killer',True,page=True,size=18)
p('KillerAnimator มีสถานะ Idle, Walk, Run, Attack1 และ Scream โดยเชื่อมกับไฟล์ KillerIdle.anim, KillerWalk.anim, KillerRun.anim, KillerAttack1.anim และ KillerScream.anim ภาพต่อไปนี้แสดงเฟรมต้นคลิปจากไฟล์ภาพที่ใช้กับศัตรู เพื่อให้เห็นความแตกต่างระหว่างการเคลื่อนที่ การโจมตี และการกรีดร้อง')
sprite(Path('Sprites/Enemy/Walk.png'),'ภาพที่ 4-18 ตัวอย่างสี่เฟรมแรกของแอนิเมชัน Walk ของ Killer')
sprite(Path('Sprites/Enemy/Run.png'),'ภาพที่ 4-19 ตัวอย่างสี่เฟรมแรกของแอนิเมชัน Run ของ Killer')
sprite(Path('Sprites/Enemy/Attack_1.png'),'ภาพที่ 4-20 ตัวอย่างสี่เฟรมแรกของแอนิเมชัน Attack1 ของ Killer')
sprite(Path('Sprites/Enemy/Scream.png'),'ภาพที่ 4-21 ตัวอย่างสี่เฟรมแรกของแอนิเมชัน Scream ของ Killer')
p('ผลการตรวจไฟล์คลิปพบว่า Idle, Walk และ Run เปิด Loop Time เพื่อแสดงท่าทางต่อเนื่อง ส่วน Attack1 และ Scream ปิด Loop Time เพื่อให้เล่นเป็นครั้งตามเหตุการณ์ การกลับสู่ Idle หลังสองท่านี้กำหนดด้วย Transition ที่เปิด Has Exit Time และตั้ง Exit Time เท่ากับ 1 ตามแผนผังในภาพที่ 3-23')

p('4.9.3 ผลการตรวจสอบการเชื่อมต่อและกรณีทดสอบแอนิเมชัน',True,page=True,size=18)
p('การตรวจสอบไฟล์ Controller และสคริปต์พบว่า isWalking และ isRunning ของ Killer เป็น Bool และมีคำสั่ง SetBool ใน KillerAI ส่วน attack1 และ scream เป็น Trigger และมีคำสั่ง SetTrigger ที่ใช้เรียกเหตุการณ์โจมตีและกรีดร้อง ชื่อพารามิเตอร์ที่สคริปต์เรียกตรงกับรายการใน Controller จึงยืนยันการเชื่อมต่อได้ในระดับโครงสร้างไฟล์')
p('Bool ทำหน้าที่คงสถานะการเคลื่อนที่ ขณะที่ Trigger ใช้เริ่มการกระทำเฉพาะครั้ง ผลการตรวจสอบนี้สอดคล้องกับเหตุผลการเลือกชนิดพารามิเตอร์ในหัวข้อ 3.5.11 ไม่พบ Float หรือ Int ใน Animator ทั้งสองตัว จึงไม่มีผลทดสอบการทำงานของพารามิเตอร์สองชนิดนี้ในรุ่นปัจจุบัน')
p('เพื่อยืนยันผลขณะทำงานจริง กำหนดกรณีทดสอบต่อไปนี้ โดยทุกกรณียังมีสถานะรอทดสอบใน Play Mode และต้องบันทึกภาพสถานะ Animator พร้อมผลที่สังเกตได้ก่อนสรุปว่าผ่าน')
for title,body in [
('AN-P01 การเริ่มต้นและหยุดเคลื่อนที่ของผู้เล่น','เริ่มฉากโดยไม่กดปุ่มเคลื่อนที่ แล้วตรวจว่า Idle ทำงานและ isWalking เป็น false จากนั้นเดินและปล่อยปุ่ม ผลที่คาดหวังคือเปลี่ยนไป Walk เมื่อเป็น true และกลับ Idle เมื่อเป็น false โดยภาพไม่ค้าง'),
('AN-K01 การเดินลาดตระเวนและการไล่ล่า','ให้ Killer เดินลาดตระเวนแล้วตรวจ isWalking จากนั้นกระตุ้นให้เข้าสู่ Chase ผลที่คาดหวังคือใช้ Walk ขณะเดิน และเปลี่ยนเป็น Run เมื่อ isRunning เป็น true'),
('AN-K02 การโจมตีและกลับสู่สถานะเคลื่อนที่','ให้ผู้เล่นอยู่ในระยะโจมตีจน KillerAI ส่ง attack1 ผลที่คาดหวังคือเล่น Attack1 ตามเหตุการณ์ แล้วกลับ Idle ตาม Exit Time ก่อนเลือกสถานะเคลื่อนที่ตามค่าพารามิเตอร์ โดยบันทึกทั้งภาพและความเสียหายที่เกิดขึ้น'),
('AN-K03 การกรีดร้องจากสถานะอื่น','ใช้ศัตรูหรือเงื่อนไขที่เปิดความสามารถกรีดร้อง แล้วกระตุ้นการส่ง scream ผลที่คาดหวังคือเปลี่ยนจากสถานะปัจจุบันผ่าน Any State ไป Scream และกลับ Idle หลังคลิปถึง Exit Time'),
('AN-K04 การหยุดไล่ล่าและกลับไปเดิน','ทำให้ Killer ออกจาก Chase และกลับไปเดิน ผลที่คาดหวังคือหยุด Run และแสดงท่าที่สัมพันธ์กับพฤติกรรม โดยตรวจลำดับ Transition เมื่อ isRunning เป็น false และ isWalking เป็น true พร้อมกัน')]:
    p(title,True)
    p(body)

anchor.paragraph_format.page_break_before=True
summary=next(x for x in d.paragraphs if x.text.startswith('4.12 สรุปผล'))
extra=summary.insert_paragraph_before('ผลการตรวจโครงสร้างแอนิเมชันพบว่ามีคลิป Idle และ Walk สำหรับผู้เล่น และ Idle, Walk, Run, Attack1 และ Scream สำหรับ Killer พร้อมการส่งพารามิเตอร์ Bool และ Trigger จากสคริปต์ตามหัวข้อ 4.9 ทั้งนี้ผลการเปลี่ยนภาพระหว่างเล่นยังต้องยืนยันจากกรณีทดสอบ AN-P01 และ AN-K01–AN-K04')
# Place the new summary paragraph beneath the existing summary text.
next_para=summary._p.getnext()
next_para.addnext(extra._p)
for run in extra.runs:
    run.font.name='TH SarabunPSK';run.font.size=Pt(16)
d.save(out)
print(out)



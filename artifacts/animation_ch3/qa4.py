import pypdfium2 as pdfium
from pathlib import Path
from PIL import Image, ImageDraw
r=Path(__file__).resolve().parent
d=pdfium.PdfDocument(r/'project_บทที่3และ4_เพิ่มแอนิเมชันตัวละครและKiller.pdf')
q=r/'qa4'; q.mkdir(exist_ok=True)
print('pages',len(d))
for start in range(0,len(d),20):
    sheet=Image.new('RGB',(1000,5*340),'#aaaaaa')
    draw=ImageDraw.Draw(sheet)
    for i in range(start,min(start+20,len(d))):
        im=d[i].render(scale=.38).to_pil()
        im.thumbnail((238,314))
        x=(i-start)%4*250; y=(i-start)//4*340
        sheet.paste(im,(x,y+20));draw.text((x+4,y+2),str(i+1),fill='black')
    sheet.save(q/f'contact-{start+1}.jpg')
for i,p in enumerate(d):
    text=p.get_textpage().get_text_range()
    if any(s in text for s in ['4.9','4-16','4-17','4-18','4-19','4-20','4-21','AN-K','4.12']):
        p.render(scale=1.5).to_pil().save(q/f'page-{i+1}.png')
        print('added',i+1,text[:110].replace('\n',' '))



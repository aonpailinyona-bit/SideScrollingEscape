from pathlib import Path
import pypdfium2 as f
from PIL import Image,ImageDraw
r=Path(__file__).resolve().parent
d=f.PdfDocument(r/'ch4_only.pdf');q=r/'qa_ch4_only';q.mkdir(exist_ok=True)
active=False;pages=[]
for i,p in enumerate(d):
    s=p.get_textpage().get_text_range()
    if 'บทที่ 4' in s:active=True
    if 'บทที่ 5' in s:active=False
    if active:
        pages.append(i);p.render(scale=1.5).to_pil().save(q/f'page-{i+1}.png')
for start in range(0,len(pages),12):
    sheet=Image.new('RGB',(1200,1140),'#cccccc');draw=ImageDraw.Draw(sheet)
    for j,i in enumerate(pages[start:start+12]):
        im=d[i].render(scale=.46).to_pil();im.thumbnail((285,350));x=j%4*300;y=j//4*380
        sheet.paste(im,(x,y+20));draw.text((x+4,y+2),str(i+1),fill='black')
    sheet.save(q/f'contact-{start}.jpg')
print('Chapter 4 pages:',[i+1 for i in pages])

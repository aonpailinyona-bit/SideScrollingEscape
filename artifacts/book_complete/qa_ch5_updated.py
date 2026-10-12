from pathlib import Path
import pypdfium2 as f
from PIL import Image,ImageDraw
r=Path('artifacts/book_complete');d=f.PdfDocument(r/'ch5_updated.pdf');q=r/'qa_ch5_updated';q.mkdir(exist_ok=True);pages=[]
active=False
for i,p in enumerate(d):
 s=p.get_textpage().get_text_range()
 if 'บทที่ 5' in s and i>30:active=True
 if active:pages.append(i);p.render(scale=1.3).to_pil().save(q/f'page-{i+1}.png')
sheet=Image.new('RGB',(1200,800),'#cccccc');dr=ImageDraw.Draw(sheet)
for j,i in enumerate(pages):
 im=d[i].render(scale=.46).to_pil();im.thumbnail((285,370));x=j%4*300;y=j//4*400;sheet.paste(im,(x,y+20));dr.text((x,y),str(i+1),fill='black')
sheet.save(q/'contact.jpg');print([i+1 for i in pages])

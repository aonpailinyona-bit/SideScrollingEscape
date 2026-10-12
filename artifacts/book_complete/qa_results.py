from pathlib import Path
import pypdfium2 as f
from PIL import Image,ImageDraw
r=Path('artifacts/book_complete'); d=f.PdfDocument(r/'results.pdf');q=r/'qa_results';q.mkdir(exist_ok=True);pages=[]
for i,p in enumerate(d):
 s=p.get_textpage().get_text_range()
 if any(x in s for x in ['TC-01','TC-FSM','TC-06','AN-P01','AN-K01','ค่าเฉลี่ยสมมติ','ตารางที่ 4-9']):
  pages.append(i);p.render(scale=1.3).to_pil().save(q/f'page-{i+1}.png')
for start in range(0,len(pages),8):
 sheet=Image.new('RGB',(1200,800),'#cccccc');dr=ImageDraw.Draw(sheet)
 for j,i in enumerate(pages[start:start+8]):
  im=d[i].render(scale=.46).to_pil();im.thumbnail((285,370));x=j%4*300;y=j//4*400;sheet.paste(im,(x,y+20));dr.text((x,y),str(i+1),fill='black')
 sheet.save(q/f'contact-{start}.jpg')
print([i+1 for i in pages])

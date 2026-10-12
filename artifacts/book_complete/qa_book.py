import pypdfium2 as f
from PIL import Image,ImageDraw
from pathlib import Path
r=Path(__file__).resolve().parent
d=f.PdfDocument(r/'book.pdf');q=r/'qa';q.mkdir(exist_ok=True)
texts=[]
for i,p in enumerate(d):
    p.render(scale=1.5).to_pil().save(q/f'page-{i+1}.png')
    texts.append(p.get_textpage().get_text_range())
(r/'pdf_text.txt').write_text('\n'.join('PAGE '+str(i+1)+'\n'+s for i,s in enumerate(texts)),encoding='utf-8')
for start in range(0,len(d),20):
    sheet=Image.new('RGB',(1200,1900),'#cccccc');dr=ImageDraw.Draw(sheet)
    for i in range(start,min(start+20,len(d))):
        im=d[i].render(scale=.46).to_pil();im.thumbnail((285,350))
        x=(i-start)%4*300;y=(i-start)//4*380;sheet.paste(im,(x,y+20));dr.text((x+4,y+2),str(i+1),fill='black')
    sheet.save(q/f'contact-{start+1}.jpg')
print('Pages',len(d),'blank-ish',[i+1 for i,s in enumerate(texts) if len(s.strip())<15])

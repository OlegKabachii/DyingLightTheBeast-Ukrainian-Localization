from pathlib import Path
import struct


class RP6L:
    def __init__(self, path):
        self.path=Path(path)
        self.data=bytearray(self.path.read_bytes())
        d=self.data
        assert d[:4]==b'RP6L'
        (self.version,)=struct.unpack_from('<I',d,4)
        self.compression=bytes(d[8:12])
        self.parts_count,self.sections_count,self.files_count,self.names_size,self.names_count,self.block=struct.unpack_from('<6I',d,12)
        pos=36
        self.sections=[]
        for _ in range(self.sections_count):
            ft,t2,t3,t4,off,unpacked,packed,unk=struct.unpack_from('<4B4I',d,pos);pos+=20
            self.sections.append(dict(filetype=ft,flags=(t2,t3,t4),offset=off*16,unpacked=unpacked,packed=packed,unk=unk))
        self.parts=[]
        for _ in range(self.parts_count):
            section,unk1,fileidx,off,size,unk2=struct.unpack_from('<2BH3I',d,pos);pos+=16
            self.parts.append(dict(section=section,fileidx=fileidx,offset=off,size=size,unk1=unk1,unk2=unk2))
        self.filemap=[]
        for _ in range(self.names_count):
            count,unk1,ft,unk2,fileidx,first=struct.unpack_from('<4B2I',d,pos);pos+=12
            self.filemap.append(dict(count=count,filetype=ft,fileidx=fileidx,first=first,unk1=unk1,unk2=unk2))
        idx=struct.unpack_from(f'<{self.names_count}I',d,pos);pos+=self.names_count*4
        self.names=[]
        for off in idx:
            end=d.find(0,pos+off)
            self.names.append(bytes(d[pos+off:end]).decode('utf-8','replace'))

    def index(self,name): return self.names.index(name)

    def extract(self,index):
        m=self.filemap[index]
        out=bytearray()
        for pi in range(m['first'],m['first']+m['count']):
            p=self.parts[pi];s=self.sections[p['section']]
            assert not s['packed'], 'compressed sections not supported'
            at=s['offset']+p['offset']*16
            out += self.data[at:at+p['size']]
        return bytes(out)

    def replace_same_size(self,index,payload):
        m=self.filemap[index];at=0
        total=sum(self.parts[pi]['size'] for pi in range(m['first'],m['first']+m['count']))
        assert len(payload)==total,(len(payload),total)
        for pi in range(m['first'],m['first']+m['count']):
            p=self.parts[pi];s=self.sections[p['section']];n=p['size']
            where=s['offset']+p['offset']*16
            self.data[where:where+n]=payload[at:at+n];at+=n

    def save(self,path): Path(path).write_bytes(self.data)

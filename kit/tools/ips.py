import sys
def apply(rom, path):
    d=bytearray(rom); p=open(path,'rb').read(); assert p[:5]==b'PATCH'; i=5
    while p[i:i+3]!=b'EOF':
        o=int.from_bytes(p[i:i+3],'big'); n=int.from_bytes(p[i+3:i+5],'big'); i+=5
        if n==0:
            r=int.from_bytes(p[i:i+2],'big'); v=p[i+2]; i+=3
            if o+r>len(d): d.extend(b'\0'*(o+r-len(d)))
            d[o:o+r]=bytes([v])*r
        else:
            if o+n>len(d): d.extend(b'\0'*(o+n-len(d)))
            d[o:o+n]=p[i:i+n]; i+=n
    return bytes(d)
def diff(a,b): return {i for i in range(len(a)) if a[i]!=b[i]}

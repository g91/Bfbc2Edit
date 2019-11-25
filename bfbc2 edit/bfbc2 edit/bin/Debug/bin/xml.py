##Requires Python 2.7. Rename to .py and drag and drop files onto the script to convert from dbx to xml and vice versa.

##The script does not read Levels\SP_008_B.dbx correctly because one of the strings contains a \n character. Look out for
##    <field name="DebugText">Go around
##</field>
##If you really need the file just remove that character from the xml file, i.e. "<field name="DebugText">Go around</field>"

roundnums=0 #set to 1 for a slightly inaccurate float conversion from dbx to xml
TABLEN="  " #adjust indentation level
#Python does not have single precision floats. The struct module allows retrieving either single or double precision floats,
#but eventually the numbers are treated as doubles anyway. Python then tries to find the shortest string representation
#for the number. Now, this is the shortest representation for DOUBLES, so for singles there are more digits than necessary.
#There is another function to make floats look nice, but occasionally it cuts off too many digits so dbx->xml->dbx will
#store a slightly different float. I haven't run many tests, but I don't recommend using roundnums if you want to go to dbx again.



import os
import sys
from struct import unpack,pack
from binascii import hexlify,unhexlify
from cStringIO import StringIO
from collections import OrderedDict


XMLHEADER="""<?xml version="1.0"?>\r\n"""

HALVES=("SphereKeyW","SphereKeyY","SphereKeyZ","TargetId","SourceId","SphereKeyX")
DOUBLES=("AwareForgetTime","LineOfSightTestTime","SensingTimeSpan","FireKeepTime","LostForgetTime",
         "TimeUntilUnseenIsLost","AttackerTrackTime")

#hashes are always integer
HASHES=("OriginalHashedWaveName","HashedName","HashedWaveName","OnRoadMaterialNameHashes","Hash","Id","CompositeMeshPartNames")
TYPE2=("Name","TextureFile","LocationName")

def read128():
    """Reads the next few bytes in global file f as LEB128 and returns an integer"""
    result,i = 0,0
    while 1:
        byte=ord(f.read(1))
        result|=(byte&127)<<i
        i+=7
        if byte>>7==0: break
    return result

def write128(integer):
    """Converts an integer to LEB128 and returns a byte string"""
    bytestring=""
    while integer:
        byte=integer&127
        integer>>=7
        if integer: byte|=128
        bytestring+=chr(byte)
    return bytestring


if roundnums:
    def formatfloat(num):
        tmp="{:g}".format(num)
        if "." not in tmp and "e" not in tmp: return tmp+".0"
        else: return tmp
else:
    def formatfloat(num):
        return str(num)

##integer in either of these cases:
##  if hash
##  if small positive number (first byte is 00)
##  if float would be nan/inf
def intfloat(rawnum,name):
    intnum=unpack(">i",rawnum)[0]
    if name in HASHES: return `intnum`
    if intnum>>24==0 or intnum>>23 in (255,-1): return `intnum`
    return formatfloat(unpack(">f",rawnum)[0])
    

def toxml(filename):
    if filename[-4:]!=".dbx": return
    fi=open(filename,"rb")
    if fi.read(8)!="{binary}":
        fi.close()
        return
    print filename
    global f
    f=StringIO(fi.read()) # dump the file in memory
    fi.close()
    out=open(filename[:-3]+"xml","wb")
    out.write(XMLHEADER) ###
    # offset to PAYLOAD; relative offset is 24 less
    totoffset,zero,reloffset,numofstrings=unpack(">IIII",f.read(16))
    stringoffsets=unpack(">"+"I"*numofstrings,f.read(4*numofstrings))
    #calculate the length of the strings and grab them
    lengths=[stringoffsets[i+1]-stringoffsets[i] for i in xrange(numofstrings-1)]
    lengths.append(reloffset-4*numofstrings-stringoffsets[-1])
    strings=[f.read(l)[:-1] for l in lengths]
    
    # do payload
    tablevel=0
    opentags=[] # need the prefixes to close the tag, e.g. <array> -> </array>
    try:
        while 1:
            # example entry: 4ca30490014b28506f00 -> 4c(prefix) a(type) 3(numofattribs) 04 9001 4b 28 50 6f (3 attribs with 2 parts each) 00 (null)
            # -> <strings[4c] strings[04]="strings[9001]" strings[4b]="strings[28]" strings[50]="strings[6f]">
            prefixnumber=read128()
            if prefixnumber==0:
                tablevel-=1
                out.write(tablevel*TABLEN+"</"+opentags.pop()+">\r\n")
                continue
            prefix=strings[prefixnumber]
            typ,numofattrib=hexlify(f.read(1))
            numofattrib=int(numofattrib)


            attribs=[[strings[read128()],strings[read128()]] for i in xrange(numofattrib)]
            if numofattrib: tag=tablevel*TABLEN+"<"+prefix+" "+" ".join([attrib[0]+'="'+attrib[1]+'"' for attrib in attribs])
            else: tag=tablevel*TABLEN+"<"+prefix

            if typ=="a": # contains other elements
                f.seek(1,1) #null
                tablevel+=1
                opentags.append(prefix)
                out.write(tag+">\r\n")
                
            elif typ=="2":
                content=strings[read128()] 
                if content: out.write(tag+">"+content+"</"+prefix+">\r\n")
                else: out.write(tag+" />\r\n") # close bracket immediately for \x00 (empty) content

            elif typ=="7":
                numofnums,numlength = read128(),read128()
                if numlength==4:
                    # need to go through every single number and evaluate whether int or float
                    if numofnums%4==0 and numofnums:
                        contentlist=[None]*numofnums
                        for i in xrange(numofnums):
                            rawnum=f.read(4)
                            if i%4==3:
                                hexnum=hexlify(rawnum)
                                if hexnum=="00000000": contentlist[i]=("*zero*")
                                elif hexnum=="cdcdcdcd": contentlist[i]=("*nonzero*")
                                else: contentlist[i]=(intfloat(rawnum,attribs[0][1]))
                            else:
                                contentlist[i]=(intfloat(rawnum,attribs[0][1]))
                        content="/".join(contentlist)
                    else: content="/".join([intfloat(f.read(4),attribs[0][1]) for x in xrange(numofnums)])
                    
                elif numlength==8: content="/".join([`x` for x in unpack(">"+"d"*numofnums,f.read(8*numofnums))])
                else: content="/".join([`x` for x in unpack(">"+"H"*numofnums,f.read(2*numofnums))])
                out.write(tag+">"+content+"</"+prefix+">\r\n")

            else: #typ 6
                f.seek(1,1) #\x01
                bol=f.read(1)
                if bol=="\x01": content="true"
                elif bol=="\x00": content="false"
                else: content=str(ord(bol)) # <field name="ChannelCount"> is stored here with values 2,4,6. Needs some testing.
                out.write(tag+">"+content+"</"+prefix+">\r\n")
    except:
        f.close()
        out.close()



def todic(word):
    try: return dic[word]
    except:
        value=write128(len(dic))
        dic[word]=value
        return value

def readline(line):
    tagstart,tagend=line.find("<")+1,line.find(">")
    if line[tagstart]=="/": return "\x00" #ENDER

    tag=line[tagstart:tagend]
    prefixlen=tag.find(" ")
    attribs=[]
    if prefixlen==-1: prefix=todic(tag.strip(" /"))
    else:
        prefix=todic(tag[:prefixlen])
        stuff=tag[prefixlen+1:].split('"')
        for i in xrange(0,len(stuff)-1,2):
            attribs+=(stuff[i].strip('= '),stuff[i+1])
    numofattribs=len(attribs)/2
    attribbytes="".join([todic(attrib) for attrib in attribs])
    
    if line[tagend-1]=="/": return prefix+unhexlify("2"+str(numofattribs))+attribbytes+"\x00" #EMPTY CONTENT FOR TYPE 2
        
    contentend=line.rfind("<",tagend+1)
    if contentend==-1: return prefix+unhexlify("a"+str(numofattribs))+attribbytes+"\x00" #TYPE A

    content=line[tagend+1:contentend]
    if numofattribs!=1 or attribs[0]!="name" or attribs[1] in TYPE2: return prefix+unhexlify("2"+str(numofattribs))+attribbytes+todic(content)  #TYPE 2, parts of it
      
    #TYPE 6:
    if content=="true": return prefix+"\x61"+attribbytes+"\x01\x01"
    elif content=="false": return prefix+"\x61"+attribbytes+"\x01\x00"
    elif attribs[1]=="ChannelCount": return prefix+"\x61"+attribbytes+"\x01"+pack("B",int(content))

    #Types 2 and 7
    if len(content)==0: return prefix+"\x71"+attribbytes+"\x00\x04" # type 7 with no numbers
    
    numstrings=content.split("/")
    if attribs[1] in HALVES:
        try:
            nums=[pack(">H",int(numstring)) for numstring in numstrings]
            numlen="\x02"
        except:
            raw_input("Invalid short int: "+attribs[1]+"="+numstring+"\r\nValid values: 0<=int<=65536")
            return
    elif attribs[1] in DOUBLES:
        try:
            nums=[pack(">d",float(numstring)) for numstring in numstrings]
            numlen="\x08"
        except:
            raw_input("Invalid double float: "+attribs[1]+"="+numstring)
            return
    elif attribs[1] in HASHES:
        try:
            nums=[pack(">i",int(numstring)) for numstring in numstrings]
            numlen="\x04"
        except: #"Id" can be hash integer but also string, meh
            if attribs[1]=="Id": return prefix+unhexlify("2"+str(numofattribs))+attribbytes+todic(content)
            else:
                raw_input("Invalid int: "+attribs[1]+"="+numstring)
                return
    else:
        nums=[]
        for numstring in numstrings:
            if numstring=="*zero*":
                nums.append("\x00\x00\x00\x00")
                continue
            elif numstring=="*nonzero*":
                nums.append("\xcd\xcd\xcd\xcd")
                continue
   
            try:
                intnum=int(numstring) #int(5.6)->5 BUT int("5.6")->error and int("-5")->-5
                if intnum>>24==0 or intnum>>23 in (255,-1): nums.append(pack(">i",intnum))
                else:
                    raw_input("Invalid integer: "+attribs[1]+"="+numstring+"\r\nValid values: -8388608<=int<=16777215 and 2139095040<=int<=2147483647")
                    return
            except:
                try:
                    floathex=pack(">f",float(numstring))
                    if floathex[0]=="\x00" and floathex!="\x00\x00\x00\x00":
                        raw_input("Float too small: "+attribs[1]+"="+numstring+"\r\n2.351e-38<=float")
                        return
                    else:
                        nums.append(floathex)
                except ValueError: return prefix+unhexlify("2"+str(numofattribs))+attribbytes+todic(content) #the value is no number -> type 2
                except:
                    raw_input("Float too large: "+attribs[1]+"="+numstring)
                    return
        numlen="\x04"
        
    numofnums=write128(len(nums))
    return prefix+"\x71"+attribbytes+numofnums+numlen+"".join(nums)
        
        
    
def todbx(filename):
    if filename[-4:]!=".xml": return
    fi=open(filename,"rb")
    if fi.read(23)!=XMLHEADER:
        fi.close()
        return
    print filename
    global dic
    f=StringIO(fi.read()) # dump the file in memory
    fi.close()
    payload=StringIO() 
    dic=OrderedDict()
    dic[""]="\x00" #this is the same in all dbx files of the game. Occasionally the empty string "" will get not only this entry but another one too (REALLY random).
    for line in f: #iterating over the file object yields the lines; neat
        if len(line)==2: continue #empty line, just \r\n
        towrite=readline(line)
        if towrite==None: return
        payload.write(towrite)

    out=open(filename[:-3]+"dbx","wb")
    strings="\x00".join(dic)+"\x00"
    numofstrings=len(dic)
    reloffset=4*numofstrings+len(strings)
    out.write("{binary}"+pack(">IIII",reloffset+24,0,reloffset,numofstrings))
    offset=0
    for entry in dic:
        out.write(pack(">I",offset))
        offset+=len(entry)+1
    out.write(strings)
    out.write(payload.getvalue())
    
    f.close()
    payload.close()
    out.close()



def lp(path): #long pathnames
    if path[:4]=='\\\\?\\': return path
    elif path=="": return path
    else: return unicode('\\\\?\\' + os.path.normpath(path))

def main():
    inp=[lp(p) for p in sys.argv[1:]]
    mode=""
    for ff in inp:
        if os.path.isfile(ff):
            if ff[-4:]==".xml": todbx(ff)
            elif ff[-4:]==".dbx": toxml(ff)
        else:
            if not mode: mode=raw_input("Convert everything from selected folders to (d)bx or (x)ml\r\n")
            if mode.lower()=="d":
                for dir0,dirs,files in os.walk(ff):
                    for f in files:
                        todbx(dir0+"\\"+f)
            elif mode.lower()=="x":
                for dir0,dirs,files in os.walk(ff):
                    for f in files:
                        toxml(dir0+"\\"+f)

try:  
    main()
except Exception, e:
    raw_input(e)
import xml.etree.ElementTree as ET, base64, struct

tree = ET.parse(r'D:\pico\output\brake_bracket.vtu')
root = tree.getroot()

compressor = root.attrib.get('compressor', 'NONE')
header_type = root.attrib.get('header_type', 'UInt32')
print(f'Compressor: {compressor}')
print(f'HeaderType: {header_type}')

for da in root.iter('DataArray'):
    name = da.attrib.get('Name','')
    if name in ('S_Mises', 'U'):
        fmt  = da.attrib.get('format','')
        enc  = da.attrib.get('encoding','')
        ncomp = da.attrib.get('NumberOfComponents','1')
        text = (da.text or '').strip()
        print(f'\n--- {name} (format={fmt}, encoding={enc}, ncomp={ncomp}) ---')
        b64 = ''.join(c for c in text if c in 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=')
        pad = len(b64) % 4
        if pad:
            b64 += '=' * (4 - pad)
        raw = base64.b64decode(b64)
        print(f'Raw bytes length: {len(raw)}')
        print(f'First 32 bytes (hex): {raw[:32].hex()}')
        u0 = struct.unpack_from('<I', raw, 0)[0]
        u1 = struct.unpack_from('<I', raw, 4)[0]
        u2 = struct.unpack_from('<I', raw, 8)[0]
        u3 = struct.unpack_from('<I', raw, 12)[0]
        print(f'[0..3]  UInt32 = {u0}  (numBlocks?)')
        print(f'[4..7]  UInt32 = {u1}  (blockSize?)')
        print(f'[8..11] UInt32 = {u2}  (lastBlockSize?)')
        print(f'[12..15] UInt32 = {u3} (compressedSize?)')
        for skip in (4, 8, 12, 16, 20):
            vals = [struct.unpack_from('<f', raw, skip + i*4)[0] for i in range(5)]
            print(f'  skip={skip}: floats = {vals}')

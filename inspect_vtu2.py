import xml.etree.ElementTree as ET, base64, struct, zlib

tree = ET.parse(r'D:\pico\output\brake_bracket.vtu')
root = tree.getroot()

compressor = root.attrib.get('compressor', 'NONE')
header_type = root.attrib.get('header_type', 'UInt32')
print(f'Compressor: {compressor}')
print(f'HeaderType: {header_type}')

# Check for AppendedData
appended = root.find('.//{*}AppendedData') or root.find('.//AppendedData')
if appended is not None:
    enc = appended.attrib.get('encoding','raw')
    print(f'\nAppendedData found! encoding={enc}')
    text = (appended.text or '').lstrip('_')
    b64 = ''.join(c for c in text if c in 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=')
    pad = len(b64) % 4
    if pad: b64 += '=' * (4 - pad)
    appended_raw = base64.b64decode(b64)
    print(f'AppendedData raw length: {len(appended_raw)}')
else:
    appended_raw = None
    print('\nNo AppendedData found.')

# Print DataArray metadata
for da in root.iter('DataArray'):
    name = da.attrib.get('Name','')
    if name in ('S_Mises', 'U'):
        fmt  = da.attrib.get('format','')
        enc  = da.attrib.get('encoding','')
        ncomp = da.attrib.get('NumberOfComponents','1')
        dtype = da.attrib.get('type','')
        offset_attr = da.attrib.get('offset','NONE')
        text = (da.text or '').strip()
        print(f'\n--- {name} (format={fmt}, enc={enc}, ncomp={ncomp}, type={dtype}, offset={offset_attr}) ---')
        print(f'  inline text length: {len(text)} chars')

        # Try inline decode
        b64 = ''.join(c for c in text if c in 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=')
        pad = len(b64) % 4
        if pad: b64 += '=' * (4 - pad)
        if b64:
            raw = base64.b64decode(b64)
            print(f'  inline decoded bytes: {len(raw)}')
        else:
            raw = None
            print('  no inline base64 content')

        # Determine which buffer to use
        if appended_raw is not None and offset_attr != 'NONE':
            off = int(offset_attr)
            raw = appended_raw[off:]
            print(f'  Using AppendedData at offset {off}, slice length: {len(raw)}')

        if raw:
            numB  = struct.unpack_from('<I', raw, 0)[0]
            bSize = struct.unpack_from('<I', raw, 4)[0]
            lSize = struct.unpack_from('<I', raw, 8)[0]
            print(f'  Header: numBlocks={numB}, blockSize={bSize}, lastBlockSize={lSize}')
            total_unc = (numB - 1) * bSize + lSize if numB > 0 else 0

            # Read compressed sizes
            csizes = []
            for i in range(numB):
                cs = struct.unpack_from('<I', raw, 12 + i*4)[0]
                csizes.append(cs)
            print(f'  Compressed block sizes: {csizes}')
            hdr_size = (3 + numB) * 4
            print(f'  Header bytes: {hdr_size}, totalUncompressed: {total_unc}')

            # Decompress each block
            out = bytearray()
            off2 = hdr_size
            ok = True
            for i, cs in enumerate(csizes):
                try:
                    chunk = zlib.decompress(raw[off2:off2+cs])
                    out.extend(chunk)
                    off2 += cs
                except Exception as e:
                    print(f'  Block {i} decompress failed: {e}')
                    ok = False
                    break

            if ok:
                print(f'  Decompressed bytes: {len(out)}')
                # Try float32
                n32 = len(out) // 4
                f32 = [struct.unpack_from('<f', out, i*4)[0] for i in range(min(8, n32))]
                print(f'  As float32, first 8: {[round(x,4) for x in f32]}')
                # Try float64
                n64 = len(out) // 8
                f64 = [struct.unpack_from('<d', out, i*8)[0] for i in range(min(8, n64))]
                print(f'  As float64, first 8: {[round(x,4) for x in f64]}')

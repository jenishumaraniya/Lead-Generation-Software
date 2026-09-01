const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

function createPng(width, height) {
  // Simple uncompressed/compressed 32-bit RGBA PNG generator
  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);

  // IHDR
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8; // 8 bit depth
  ihdr[9] = 6; // RGBA color type
  ihdr[10] = 0; // compression
  ihdr[11] = 0; // filter
  ihdr[12] = 0; // interlace

  function makeChunk(type, data) {
    const len = Buffer.alloc(4);
    len.writeUInt32BE(data.length, 0);
    const chunkType = Buffer.from(type, 'ascii');
    const crc = crc32(Buffer.concat([chunkType, data]));
    const crcBuf = Buffer.alloc(4);
    crcBuf.writeUInt32BE(crc, 0);
    return Buffer.concat([len, chunkType, data, crcBuf]);
  }

  // Generate image data (gradient / solid indigo color #6366f1)
  const rawRows = [];
  for (let y = 0; y < height; y++) {
    const row = Buffer.alloc(1 + width * 4);
    row[0] = 0; // Filter: none
    for (let x = 0; x < width; x++) {
      const idx = 1 + x * 4;
      // Indigo icon (#6366f1)
      row[idx] = 99;     // R
      row[idx + 1] = 102; // G
      row[idx + 2] = 241; // B
      row[idx + 3] = 255; // A
    }
    rawRows.push(row);
  }
  const rawData = Buffer.concat(rawRows);
  const idatData = zlib.deflateSync(rawData);

  const ihdrChunk = makeChunk('IHDR', ihdr);
  const idatChunk = makeChunk('IDAT', idatData);
  const iendChunk = makeChunk('IEND', Buffer.alloc(0));

  return Buffer.concat([signature, ihdrChunk, idatChunk, iendChunk]);
}

function crc32(buf) {
  let table = [];
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) {
      if (c & 1) c = 0xedb88320 ^ (c >>> 1);
      else c = c >>> 1;
    }
    table[n] = c;
  }
  let crc = 0 ^ (-1);
  for (let i = 0; i < buf.length; i++) {
    crc = (crc >>> 8) ^ table[(crc ^ buf[i]) & 0xff];
  }
  return (crc ^ (-1)) >>> 0;
}

const dir = path.join(__dirname, 'icons');
if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });

[16, 48, 128].forEach(size => {
  const png = createPng(size, size);
  fs.writeFileSync(path.join(dir, `icon-${size}.png`), png);
  console.log(`Generated icon-${size}.png (${size}x${size})`);
});

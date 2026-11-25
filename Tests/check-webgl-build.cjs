// Validate a produced player without modifying the upload directory.
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const root = path.resolve(process.argv[2] || 'Builds/YandexGames');
const html = fs.readFileSync(path.join(root, 'index.html'), 'utf8');
assert(!html.includes('{{{'), 'Unresolved Unity template variables');
assert(/src=["']\/sdk\.js["']/.test(html), 'Missing Yandex SDK');
assert(html.includes('pyaterka_premium_car'), 'Permanent car purchase bridge missing');
assert(html.includes('PYATERKA_CLOUD_UNAVAILABLE'), 'Cloud failure handling missing');
assert(!html.includes('miner_titan_pickaxe'), 'Wrong game product');
let scripts = 0;
for (const match of html.matchAll(/<script\b[^>]*>([\s\S]*?)<\/script>/gi)) {
  if (match[1].trim()) { new vm.Script(match[1], {filename: `index-inline-${++scripts}.js`}); }
}
const expected = [...html.matchAll(/(?:dataUrl|frameworkUrl|codeUrl):\s*buildUrl\s*\+\s*["']\/([^"']+)["']/g)].map(m => m[1]);
assert.equal(expected.length, 3, 'Missing player data/framework/code references');
let compressed = 0, unpacked = 0;
for (const name of expected) {
  const bytes = fs.readFileSync(path.join(root, 'Build', name));
  let data;
  if (bytes[0] === 0x1f && bytes[1] === 0x8b) data = zlib.gunzipSync(bytes);
  else if (name.endsWith('.unityweb') || name.endsWith('.br')) data = zlib.brotliDecompressSync(bytes);
  else data = bytes;
  if (name.includes('.wasm')) assert(data.subarray(0, 4).equals(Buffer.from([0, 97, 115, 109])), 'Invalid WebAssembly');
  if (name.includes('.framework.js')) new vm.Script(data.toString(), {filename: name});
  compressed += bytes.length; unpacked += data.length;
  console.log(`${name}: ${bytes.length} bytes, decompressed ${data.length}`);
}
assert(fs.readdirSync(path.join(root, 'Build')).some(n => n.endsWith('.loader.js')), 'Missing player loader');
console.log(`PASS: ${scripts} inline scripts parsed; player files valid; ${compressed} compressed / ${unpacked} unpacked bytes`);

// Inspect the actual IL2CPP output, which Editor simulations cannot cover.
const fs = require('node:fs');
const path = require('node:path');
const plugin = path.resolve(__dirname, '../Assets/PluginYourGames');
const cppRoot = path.resolve(process.argv[2] || 'LocalChecks/WebGLProject/Library/Bee/artifacts/WebGL/il2cppOutput/cpp');
function files(root) {
  return fs.readdirSync(root, {withFileTypes:true}).flatMap(entry => {
    const target = path.join(root, entry.name);
    return entry.isDirectory() ? files(target) : [target];
  });
}
const callbacks = files(plugin).filter(file => file.endsWith('.cs')).flatMap(file =>
  [...fs.readFileSync(file, 'utf8').matchAll(/\[(?:InitYG(?:_[012])?|StartYG)\][\s\S]*?private static void (\w+)\(/g)].map(match => match[1]));
const cpp = fs.readdirSync(cppRoot).filter(file => /^Assembly-CSharp.*\.cpp$/.test(file))
  .map(file => fs.readFileSync(path.join(cppRoot, file), 'utf8')).join('\n');
const editorOnly = new Set(['InitWindowGame', 'ResetStaticLB', 'RewardedAdvInit']);
const report = [...new Set(callbacks)].filter(name => !editorOnly.has(name)).sort()
  .map(name => ({name, preserved:cpp.includes('void YG2_' + name + '_m')}));
for (const item of report) console.log((item.preserved ? 'PRESERVED: ' : 'MISSING: ') + item.name);
if (process.argv[3]) fs.writeFileSync(process.argv[3], JSON.stringify(report, null, 2));
if (!report.length || report.some(item => !item.preserved)) process.exitCode = 1;
else console.log('PASS: all ' + report.length + ' SDK reflection callbacks survived managed stripping');

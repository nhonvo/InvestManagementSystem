import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const sourceDir = path.resolve(__dirname, '../InventoryAlert.Wiki/docs');
const targetDir = path.resolve(__dirname, '../artifacts/wiki-export');

function processFile(filePath, rootSourceDir, outDir) {
  let content = fs.readFileSync(filePath, 'utf8');
  let relativePath = path.relative(rootSourceDir, filePath);
  
  if (relativePath === 'index.md') {
    relativePath = 'Home.md';
  }
  
  const targetFilePath = path.join(outDir, relativePath);
  
  // ensure dir exists
  fs.mkdirSync(path.dirname(targetFilePath), { recursive: true });
  
  // 1. Strip frontmatter
  content = content.replace(/^---[\s\S]+?---\n/, '');
  
  // 2. Adjust links if needed (relative paths generally work in GitHub wiki if folders are preserved)
  // E.g., no strict link rewriting needed if keeping the same structure
  
  fs.writeFileSync(targetFilePath, content, 'utf8');
}

function walkDir(dir) {
  const files = fs.readdirSync(dir);
  for (const file of files) {
    const fullPath = path.join(dir, file);
    if (fs.statSync(fullPath).isDirectory()) {
      walkDir(fullPath);
    } else if (fullPath.endsWith('.md')) {
      processFile(fullPath, sourceDir, targetDir);
    }
  }
}

fs.rmSync(targetDir, { recursive: true, force: true });
fs.mkdirSync(targetDir, { recursive: true });
walkDir(sourceDir);
console.log('Wiki export complete. Files written to ' + targetDir);
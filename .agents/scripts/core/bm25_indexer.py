import os
import re
import hashlib
import csv
import sys
import time
from datetime import datetime
from concurrent.futures import ProcessPoolExecutor
from bm25_core import save_index_binary, load_index_csv, save_index_csv
from bm25_core import save_index_binary, load_index_csv, save_index_csv
import json

# Force UTF-8 for Windows compatibility with special characters
if sys.platform == "win32":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# --- Configuration ---
csv.field_size_limit(2147483647)

try:
    with open(os.path.join(os.path.dirname(__file__), 'bm25_config.json'), 'r', encoding='utf-8') as f:
        config = json.load(f)
except Exception:
    config = {}

SYNONYMS = config.get('synonyms', {})
SCAN_FOLDERS = config.get('scan_folders', [])

def get_file_metadata(file_path):
    """Get MD5 hash and last modification time of file."""
    try:
        mtime = os.path.getmtime(file_path)
        with open(file_path, 'rb') as f:
            fhash = hashlib.md5(f.read()).hexdigest()
        return fhash, mtime
    except Exception:
        return "", 0

def expand_synonyms(text):
    expanded = []
    text_lower = text.lower()
    for key, variants in SYNONYMS.items():
        if key in text_lower:
            expanded.extend(variants)
    return " ".join(set(expanded))

def extract_keywords(text, max_keywords=8):
    """
    Extract meaningful keywords from text.
    Filters out common words and returns most significant terms.
    """
    # Remove code blocks and special chars
    clean = re.sub(r'```[\s\S]*?```', '', text)
    clean = re.sub(r'[^\w\s]', ' ', clean)
    
    # Extract words
    words = re.findall(r'\b[a-z]{3,}\b', clean.lower())
    
    # Common stopwords
    stopwords = {'the', 'and', 'for', 'are', 'but', 'not', 'you', 'all', 'can', 'her', 
                 'was', 'one', 'our', 'out', 'day', 'get', 'has', 'him', 'his', 'how',
                 'its', 'may', 'now', 'old', 'see', 'than', 'that', 'this', 'with'}
    
    # Filter and count
    from collections import Counter
    filtered = [w for w in words if w not in stopwords and len(w) > 3]
    common = Counter(filtered).most_common(max_keywords)
    
    return [word for word, count in common]

def generate_summary(text, max_length=150):
    """
    Generate a concise summary from text.
    Takes first meaningful sentence or paragraph.
    """
    # Remove markdown formatting
    clean = re.sub(r'[#*`]', '', text)
    clean = re.sub(r'\n+', ' ', clean).strip()
    
    # Find first sentence
    sentences = re.split(r'[.!?]\s+', clean)
    if sentences:
        summary = sentences[0]
        if len(summary) > max_length:
            summary = summary[:max_length] + "..."
        return summary
    return clean[:max_length] + "..." if len(clean) > max_length else clean

def get_contextual_filename(file_path):
    """
    Extract meaningful context from file path.
    Examples:
    - src/features/dashboard/hooks/useStats.ts -> "dashboard/useStats"
    - scripts/analyst/core/mapper.py -> "analyst/mapper"
    """
    parts = file_path.replace('\\', '/').split('/')
    
    # Get last 2-3 meaningful parts
    meaningful = [p for p in parts if p not in ['src', 'scripts', 'features', 'components', 'hooks', 'utils']]
    
    if len(meaningful) >= 2:
        return '/'.join(meaningful[-2:]).replace('.ts', '').replace('.tsx', '').replace('.py', '')
    elif meaningful:
        return meaningful[-1].replace('.ts', '').replace('.tsx', '').replace('.py', '')
    return os.path.basename(file_path)

def process_file(file_info):
    """
    Dual-mode processing:
    - Documentation: Full content + summary + keywords
    - Code: Filepath reference only + contextual naming
    """
    abs_path, rel_path, existing_chunks = file_info
    current_hash, mtime = get_file_metadata(abs_path)
    
    if existing_chunks and existing_chunks[0].get('file_hash') == current_hash:
        return {'status': 'unchanged', 'chunks': existing_chunks, 'rel_path': rel_path}
    
    try:
        with open(abs_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            
        content = "".join(lines)
        file_ext = os.path.splitext(abs_path)[1].lower()
        is_doc = file_ext == '.md'
        category = "doc" if is_doc else "code"
        
        processed_chunks = []
        
        if is_doc:
            # DOCUMENTATION MODE: Full content with enhanced metadata
            current_chunk_lines = []
            start_line = 1
            current_header = os.path.basename(abs_path)
            
            for i, line in enumerate(lines, 1):
                if line.strip().startswith('# '):
                    if current_chunk_lines:
                        chunk_text = "".join(current_chunk_lines).strip()
                        keywords = extract_keywords(chunk_text)
                        summary = generate_summary(chunk_text)
                        
                        processed_chunks.append({
                            'content': chunk_text,
                            'type': f"{current_header} | {summary[:80]}",
                            'keywords': ', '.join(keywords),
                            'start_line': start_line,
                            'end_line': i - 1
                        })
                    
                    current_header = line.strip()
                    current_chunk_lines = [line]
                    start_line = i
                else:
                    current_chunk_lines.append(line)
            
            if current_chunk_lines:
                chunk_text = "".join(current_chunk_lines).strip()
                keywords = extract_keywords(chunk_text)
                summary = generate_summary(chunk_text)
                
                processed_chunks.append({
                    'content': chunk_text,
                    'type': f"{current_header} | {summary[:80]}",
                    'keywords': ', '.join(keywords),
                    'start_line': start_line,
                    'end_line': len(lines)
                })

        else:
            # CODE MODE: Filepath reference with contextual naming
            current_chunk_lines = []
            context_name = get_contextual_filename(rel_path)
            current_type = f"[MOD] {context_name}"
            start_line = 1
            
            pattern = re.compile(r'^(def |class |export |interface |async function |function )')
            
            for i, line in enumerate(lines, 1):
                if pattern.match(line):
                    if current_chunk_lines:
                        # For code, we store minimal content for BM25 tokenization
                        # but mark it clearly as code reference
                        chunk_text = "".join(current_chunk_lines).strip()
                        processed_chunks.append({
                            'content': chunk_text[:500],  # Truncate for index size
                            'type': current_type,
                            'keywords': context_name,
                            'start_line': start_line,
                            'end_line': i - 1
                        })
                    
                    func_name = line.strip().split('(')[0].replace('def ', '').replace('class ', '').replace('export ', '')
                    current_type = f"[FUNC] {context_name}/{func_name}"
                    current_chunk_lines = [line]
                    start_line = i
                else:
                    current_chunk_lines.append(line)
                    
            if current_chunk_lines:
                chunk_text = "".join(current_chunk_lines).strip()
                processed_chunks.append({
                    'content': chunk_text[:500],
                    'type': current_type,
                    'keywords': context_name,
                    'start_line': start_line,
                    'end_line': len(lines)
                })

        # Finalize with synonyms
        synonyms_str = expand_synonyms(content)
        
        final_chunks = []
        for c in processed_chunks:
            final_chunks.append({
                'file_path': rel_path,
                'type': c['type'],
                'content': c['content'],
                'keywords': c.get('keywords', ''),
                'synonyms': synonyms_str,
                'category': category,
                'file_hash': current_hash,
                'last_modified': mtime,
                'start_line': c['start_line'],
                'end_line': c['end_line'],
                'hits': existing_chunks[0].get('hits', 0) if existing_chunks else 0
            })
            
        return {'status': 'processed', 'chunks': final_chunks, 'rel_path': rel_path}
    except Exception as e:
        print(f"Error processing {rel_path}: {e}")
        return {'status': 'error', 'chunks': [], 'rel_path': rel_path}

def index_project(root_dir, existing_index):
    file_list = []
    processed_rel_paths = set()
    
    for folder in SCAN_FOLDERS:
        folder_path = os.path.join(root_dir, folder)
        if not os.path.exists(folder_path): continue
        for root, _, files in os.walk(folder_path):
            rel_root = os.path.relpath(root, root_dir).replace('\\', '/')
            
            if any(p in root for p in ['__pycache__', 'node_modules', '.git', '.next', 'dist', 'out']): continue
            if 'documentation/archive' in rel_root: continue

            for file in files:
                if file.lower().endswith(('.md', '.ts', '.tsx', '.py', '.json')):
                    abs_path = os.path.join(root, file)
                    rel_path = os.path.relpath(abs_path, root_dir)
                    if rel_path in processed_rel_paths: continue
                    processed_rel_paths.add(rel_path)
                    file_list.append((abs_path, rel_path, existing_index.get(rel_path, [])))
                    
    for item in os.listdir(root_dir):
        if item.lower().endswith(('.md', '.py', '.json')):
            abs_path = os.path.join(root_dir, item)
            if os.path.isfile(abs_path) and item not in processed_rel_paths:
                file_list.append((abs_path, item, existing_index.get(item, [])))

    print(f"Indexing {len(file_list)} files...")
    new_index_data = []
    stats = {'unchanged': 0, 'processed': 0, 'error': 0}
    
    with ProcessPoolExecutor() as executor:
        results = list(executor.map(process_file, file_list))
        
    chunk_id = 1
    for res in results:
        stats[res['status']] += 1
        for chunk in res['chunks']:
            chunk['id'] = chunk_id
            new_index_data.append(chunk)
            chunk_id += 1
            
    print(f"\nStats: {stats['unchanged']} unchanged, {stats['processed']} processed/updated, {stats['error']} errors.")
    return new_index_data

if __name__ == "__main__":
    PROJECT_ROOT = os.getcwd()
    DATA_DIR = os.path.join(PROJECT_ROOT, ".agents/scripts/core/data")
    if not os.path.exists(DATA_DIR): os.makedirs(DATA_DIR)
    
    INDEX_CSV = os.path.join(DATA_DIR, "bm25_index.csv")
    INDEX_BIN = os.path.join(DATA_DIR, "bm25_index.bin")
    
    print(f"--- BM25 Indexer V4.1 (Hybrid: Doc Full + Code Ref) ---")
    start_time = time.time()
    
    raw_existing = load_index_csv(INDEX_CSV)
    existing_map = {}
    for row in raw_existing:
        fpath = row['file_path']
        if fpath not in existing_map: existing_map[fpath] = []
        existing_map[fpath].append(row)
        
    final_data = index_project(PROJECT_ROOT, existing_map)
    
    # Hybrid save: Keep content for docs, strip for code in CSV
    csv_data = []
    for chunk in final_data:
        csv_row = chunk.copy()
        if csv_row.get('category') == 'code' and 'content' in csv_row:
            del csv_row['content']  # Code uses filepath reference only
        csv_data.append(csv_row)
        
    save_index_csv(INDEX_CSV, csv_data)
    save_index_binary(INDEX_BIN, final_data)
    
    duration = time.time() - start_time
    print(f"Successfully indexed {len(final_data)} chunks in {duration:.2f}s")
    print(f"Binary index saved to: {INDEX_BIN}")

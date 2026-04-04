import argparse
import os
import re
import sys
import time
from bm25_core import BM25, load_index_binary, save_index_csv, save_index_binary
import io

# Force UTF-8 for Windows compatibility with special characters
if sys.platform == "win32":
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# --- AESTHETIC TEMPLATE (Clean Minimalist) ---
HEADER_BAR = "-" * 80
DIVIDER = "." * 80
SUCCESS_GLOW = "[DONE]"
ERROR_GLOW = "[ERROR]"
SEARCH_GLOW = "[SEARCH]"
SKILL_GLOW = "[SKILL]"

# --- SKILL MAPPING ENGINE ---
SKILL_MAP = {
    "nextjs": ["nextjs-expert", "typescript-expert", "react-expert"],
    "react": ["react-expert", "nextjs-expert", "typescript-expert"],
    "css": ["css-styling-expert", "ui-ux-pro-max"],
    "tailwind": ["ui-ux-pro-max", "css-styling-expert"],
    "mongodb": ["mongodb-expert", "database-expert"],
    "mongoose": ["mongodb-expert", "database-expert"],
    "schema": ["prisma-expert", "database-expert", "typescript-type-expert"],
    "test": ["testing-expert", "jest-testing-expert", "playwright-expert"],
    "api": ["rest-api-expert", "nextjs-expert", "typescript-expert"],
    "doc": ["documentation-expert"],
    "workflow": ["research-expert", "triage-expert"],
    "skill": ["skill-creator"],
    "ai": ["ai-sdk-expert", "oracle"],
    "dotnet": ["master-dotnet-skill"],
    "docker": ["docker-expert", "devops-expert"],
    "git": ["git-expert"],
    "performance": ["react-performance", "webpack-expert"],
    "state": ["state-management-expert"],
}

def recommend_skills(file_path, content, category):
    """Refined Skill Recommendation based on file context and category."""
    recommended = set()
    
    # Heuristics based on file path/extension
    path_lower = file_path.lower()
    if any(ext in path_lower for ext in [".ts", ".tsx"]):
        recommended.update(["typescript-expert"])
    if "src/features" in path_lower:
        recommended.update(["nextjs-expert", "react-expert"])
    if any(ext in path_lower for ext in [".css", ".scss"]) or "tailwind" in content.lower():
        recommended.update(["css-styling-expert", "ui-ux-pro-max"])
    if "src/server/models" in path_lower or "mongodb" in content.lower():
        recommended.update(["mongodb-expert", "database-expert"])
    if "test" in path_lower or ".spec." in path_lower:
        recommended.update(["testing-expert", "jest-testing-expert"])
    if "docs/" in path_lower or category == "doc":
        recommended.update(["documentation-expert"])
    if ".agent/workflows" in path_lower:
        recommended.update(["research-expert", "triage-expert"])
    if ".agent/skills" in path_lower:
        recommended.update(["skill-creator"])

    # keyword check in content
    content_lower = content.lower()
    for key, skills in SKILL_MAP.items():
        if key in content_lower:
            recommended.update(skills)

    return sorted(list(recommended))[:3] # Return top 3 unique skills

def extract_best_window(content, query, window_size=15):
    """
    Finds up to 2 distinct windows with high keyword density.
    Returns: (list of lines, start_line_offset)
    """
    lines = content.split('\n')
    if len(lines) <= window_size:
        return lines, 0

    query_terms = [t.lower() for t in query.split()]
    
    # Calculate score for every line
    line_scores = []
    for i, line in enumerate(lines):
        score = sum(line.lower().count(t) for t in query_terms)
        line_scores.append(score)
        
    # Calculate window scores
    window_scores = []
    unique_windows = len(lines) - window_size + 1
    
    if unique_windows < 1:
        return lines, 0
        
    for i in range(unique_windows):
        # Score is sum of line scores in window
        # Bonus: bias slightly towards earlier chunks if tie (stable sort)
        current_score = sum(line_scores[i:i+window_size])
        window_scores.append((current_score, i))
        
    # Sort by score descending
    window_scores.sort(key=lambda x: x[0], reverse=True)
    
    # Pick Top 1
    best_windows = []
    if window_scores:
        best_windows.append(window_scores[0][1])
        
    # Pick Top 2 (if distinct enough)
    # We want a second window that doesn't overlap excessively with the first
    if len(window_scores) > 1:
        top_1_start = best_windows[0]
        for score, start in window_scores[1:]:
            # Check overlap
            if abs(start - top_1_start) >= window_size:
                best_windows.append(start)
                break
                
    best_windows.sort() # Ensure file order
    
    final_lines = []
    # If we have 1 window or they are adjacent/close, just return one big block or normal
    # But function signature requires specific return format.
    # Let's flatten to textual representation for the prompt, 
    # but strictly speaking `extract_best_window` returns lines list.
    # We need to adapt the caller or return a combined list with a marker?
    
    # Actually, let's keep it compatible: Return the primary window, 
    # but extend it if the second window is "close enough" OR return a special marker line
    
    # Simpler approach for compatibility: Just return the HIGHEST scoring window for now,
    # BUT with a naive expansion if the second best is valuable.
    
    # Wait, for the Test Case to pass, I NEED both "MongoDB" (Line 18) and "Seeding" (Line 135).
    # These are far apart.
    # If I return lines 18-33, I miss Seeding.
    # If I return lines 135-150, I miss MongoDB.
    # So I MUST return a combined view.
    
    combined_view = []
    last_end = -1
    
    for start in best_windows:
        end = start + window_size
        if last_end != -1 and start > last_end:
             combined_view.append(f"... (skipped {start - last_end} lines) ...")
        
        combined_view.extend(lines[start:end])
        last_end = end
        
    # Return 0 as start offset because we are modifying the text structure itself
    # Caller `format_result_template` just iterates lines.
    return combined_view, best_windows[0] if best_windows else 0

def extract_related_files(content):
    """Finds file paths mentioned in content that actually exist."""
    # Regex for potential file paths (simple heuristic)
    # Looks for words with / or extensions like .ts, .md, .py, .json
    potential_paths = re.findall(r'(?:[\w\-\.]+\/)+[\w\-\.]+\.[a-z]{2,5}|[\w\-\.]+\.[a-z]{2,5}', content)
    
    found = set()
    project_root = os.getcwd()
    
    for p in potential_paths:
        # Filter common noise
        if p.lower() in ['node_modules', '.git', 'package.json', 'tsconfig.json']: continue
        if p.startswith('http'): continue
        
        # Check absolute or relative
        check_path = os.path.abspath(p) if os.path.isabs(p) else os.path.join(project_root, p)
        if os.path.exists(check_path) and os.path.isfile(check_path):
            # Return relative path for brevity
            try:
                rel = os.path.relpath(check_path, project_root)
                found.add(rel)
            except:
                found.add(p)
                
    return sorted(list(found))

def format_result_template(idx, res, query, show_verify=False):
    """Refined compact template with Windowing + Relationships."""
    cat_tag = res['category'].upper()
    reliability = f" (Rel: {res['reliability']:.0%})" if show_verify else ""
    
    # Strip header to ensure no newline leaks
    clean_header = res['header'].strip()
    clean_file = res['file'].strip()
    
    # Header: [1] src/main.ts:50 (CODE) | func: main (Score: 12.5)
    header = f"[{idx}] {clean_file}:{res['start_line']} [{cat_tag}] | {clean_header} (Score: {res['score']:.2f}{reliability})"
    
    output = [header]
    
    # Skills
    skills = recommend_skills(res['file'], res['content'], res['category'])
    if skills:
        output.append(f"   > Skills: {', '.join(skills)}")
        
    # Related Files (New Feature)
    related = extract_related_files(res['content'])
    # Filter out the file itself
    related = [r for r in related if r != clean_file and 'node_modules' not in r]
    if related:
         output.append(f"   > Related: {', '.join(related[:3])}" + (f" (+{len(related)-3} more)" if len(related)>3 else ""))
    
    # Content Windowing
    view_lines, start_offset = extract_best_window(res['content'], query)
    
    # Visual cues for window position
    if start_offset > 0:
        output.append("   | ... (offsets)")
        
    for line in view_lines:
        clean_line = line.strip()
        if clean_line:
            output.append(f"   | {clean_line}")
            
    total_lines = len(res['content'].split('\n'))
    if start_offset + len(view_lines) < total_lines:
        output.append(f"   | ... (+{total_lines - (start_offset + len(view_lines))} more)")
    
    return "\n".join(output)

# --- CORE LOGIC ---

def clean_snippet(text):
    if not text: return ""
    return re.sub(r'\n\n<!-- Context: .*? -->', '', text)

def track_hit(file_path, data):
    for item in data:
        if item.get('file_path') == file_path:
            hits = int(item.get('hits', 0))
            item['hits'] = hits + 1
    return data

def read_source_context(file_path, start_line, end_line, margin=3):
    """Read-on-Demand: Always provides fresh content."""
    if not os.path.exists(file_path):
        return f"File not found: {file_path}"
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            all_lines = f.readlines()
        s = max(0, start_line - 1 - margin)
        e = min(len(all_lines), end_line + margin)
        return "".join(all_lines[s:e]).strip()
    except Exception as e:
        return f"Error reading source: {str(e)}"

def evaluate_relevance(query, content):
    """Heuristic logic for correctness metrics."""
    query_tokens = set(re.findall(r'\w+', query.lower()))
    content_tokens = set(re.findall(r'\w+', content.lower()))
    if not query_tokens: return 1.0
    matches = query_tokens.intersection(content_tokens)
    return len(matches) / len(query_tokens)

def evaluate_citation_coverage(results, check_content):
    """
    Checks if the check_content cites the high-score files 
    provided by bm25_search.py output.
    """
    # Threshold adjusted for normalized BM25 (e.g., > 20) or just take Top 3
    # Use top 60% of results as 'Required' for context
    if not results: return
    
    # Filter for high relevance items (heuristic: score > 20 or top 3)
    high_value_results = [r for r in results if r['score'] > 20]
    if not high_value_results:
        high_value_results = results[:3] # Fallback to top 3
        
    required_files = [res['file'] for res in high_value_results]
    
    # Normalize paths for comparison (forward slashes)
    check_lower = check_content.lower().replace('\\', '/')
    cited_count = 0
    missing = []
    
    for f in required_files:
        # Check filename or full path
        fname = os.path.basename(f).lower()
        fpath = f.lower().replace('\\', '/')
        if fname in check_lower or fpath in check_lower:
            cited_count += 1
        else:
            missing.append(os.path.basename(f))
    
    score = (cited_count / len(required_files) * 100) if required_files else 100
    
    print(f"\n--- 🛡️ Citation Trust Score ---")
    print(f"Trust Score: {score:.1f}%")
    if score < 100:
        print(f"[WARN] Missing Citations for High-Value Context:")
        for m in missing:
            print(f" - {m}")
    else:
        print(f"[PASS] All high-value context files referenced.")

def search(query, top_n=5, min_score=0.1, category=None, folder_filter=None):
    PROJECT_ROOT = os.getcwd()
    DATA_DIR = os.path.join(PROJECT_ROOT, ".agents/scripts/core/data")
    INDEX_BIN = os.path.join(DATA_DIR, "bm25_index.bin")
    INDEX_CSV = os.path.join(DATA_DIR, "bm25_index.csv")
    
    if not os.path.exists(INDEX_BIN):
        return {"error": f"Index missing at {INDEX_BIN}. Run: python .agents/scripts/core/bm25_indexer.py"}
    
    data = load_index_binary(INDEX_BIN)
    if not data: return {"error": "Index empty."}
    
    filtered_data = [item for item in data if not category or item.get('category') == category]
    
    # Filter by folder if specified
    if folder_filter:
        folder_norm = folder_filter.lower().replace('\\', '/')
        filtered_data = [item for item in filtered_data if folder_norm in item.get('file_path', '').lower().replace('\\', '/')]
        
    if not filtered_data: return []

    corpus = [item.get('content', '') for item in filtered_data]
    metadata = [{
        'last_modified': item.get('last_modified', 0),
        'hits': item.get('hits', 0),
        'header': item.get('type', ''),
        'file_path': item.get('file_path', '')
    } for item in filtered_data]
    
    bm25 = BM25()
    bm25.fit(corpus, metadata=metadata)
    
    scores = bm25.get_scores(query)
    
    # --- METADATA BOOSTING ---
    query_terms = [t.lower() for t in query.split()]
    boosted_indices = []
    
    for i, score in enumerate(scores):
        if score < min_score: continue
        
        # Boost logic
        meta = metadata[i]
        boost_factor = 1.0
        
        # Check Header/Type
        header_lower = str(meta.get('header', '')).lower()
        if any(term in header_lower for term in query_terms):
            boost_factor *= 1.2
            
        # Check File Path (stronger signal)
        path_lower = str(meta.get('file_path', '')).lower()
        if any(term in path_lower for term in query_terms):
            boost_factor *= 1.3
        
        # Context Priority: Docs and Source are more relevant than generic skills
        if "docs/" in path_lower or "src/" in path_lower:
            boost_factor *= 1.2
            
        # Check Tags (High signal if indexed)
        tags = filtered_data[i].get('tags', [])
        if isinstance(tags, list):
            # Check if any tag matches any query term
            tag_matches = [tag for tag in tags if any(term in str(tag).lower() for term in query_terms)]
            if tag_matches:
                 boost_factor *= 2.0

        final_score = score * boost_factor
        
        if final_score >= min_score:
            boosted_indices.append((i, final_score))

    boosted_indices.sort(key=lambda x: x[1], reverse=True)
    
    results = []
    for idx, score in boosted_indices[:top_n]:
        res_item = filtered_data[idx]
        file_abs = os.path.join(PROJECT_ROOT, res_item['file_path'])
        
        # Read Fresh Context
        content = read_source_context(file_abs, int(res_item.get('start_line', 1)), int(res_item.get('end_line', 1)))
        
        results.append({
            "score": round(score, 3),
            "file": res_item['file_path'],
            "header": res_item['type'],
            "content": content,
            "start_line": int(res_item.get('start_line', 1)),
            "end_line": int(res_item.get('end_line', 1)),
            "category": res_item.get('category', 'unknown'),
            "reliability": evaluate_relevance(query, content),
            "hits": res_item.get('hits', 0)
        })
        
    if results:
        data = track_hit(results[0]['file'], data)
        save_index_binary(INDEX_BIN, data)
        # Hybrid Save: Keep content for docs in CSV
        csv_data = []
        for d in data:
            c = d.copy()
            if c.get('category') == 'code' and 'content' in c: del c['content']
            csv_data.append(c)
        save_index_csv(INDEX_CSV, csv_data)
            
    return results

def main():
    parser = argparse.ArgumentParser(description="BM25+ Premium Search Engine (v4.6)")
    parser.add_argument("query", help="Search target")
    parser.add_argument("legacy_top", type=int, nargs="?", help="Legacy positional limit (optional)")
    parser.add_argument("-n", "--top", type=int, default=5, help="Number of results")
    parser.add_argument("-m", "--mode", choices=["doc", "code", "all"], default="doc", help="Search focus")
    parser.add_argument("--verify", action="store_true", help="Show reliability score")
    parser.add_argument("--debug", action="store_true", help="Show technical metrics")
    parser.add_argument("--check-file", help="Path to file to check for citations (Trust Score)")
    parser.add_argument("-f", "--folder", help="Limit search to a specific folder")
    
    args = parser.parse_args()
    
    # Support for legacy positional "query N" format
    limit = args.legacy_top if args.legacy_top is not None else args.top

    start_time = time.time()
    start_time = time.time()
    results = search(args.query, top_n=limit, category=None if args.mode == "all" else args.mode, folder_filter=args.folder)
    
    # Doc Fallback
    if not results and args.mode == "doc":
        results = search(args.query, top_n=limit, category="code", folder_filter=args.folder)
        args.mode = "code (fallback)"

    duration = time.time() - start_time
    
    print(f"Search: '{args.query}' (Mode: {args.mode.upper()})")
    
    if isinstance(results, dict) and "error" in results:
        print(f"[ERROR] {results['error']}")
    elif not results:
        print(f"[INFO] No relevant context found.")
    else:
        for i, res in enumerate(results, 1):
            print(format_result_template(i, res, args.query, show_verify=args.verify))
            if args.debug:
                print(f"   [DEBUG] Hits: {res['hits']} | Range: {res['start_line']}-{res['end_line']}")
            print("") # Single newline separator
        
        print(f"[DONE] Found {len(results)} results in {duration:.3f}s")
        
        # Post-Search Audit
        if args.check_file:
            if os.path.exists(args.check_file):
                with open(args.check_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                evaluate_citation_coverage(results, content)
            else:
                print(f"\n[ERROR] Check file not found: {args.check_file}")

if __name__ == "__main__":
    main()

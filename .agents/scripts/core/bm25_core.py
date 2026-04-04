import os
import math
import re
import csv
import pickle
import time
from collections import Counter
from typing import List, Dict, Optional

class BM25Plus:
    """
    Advanced BM25+ implementation with:
    - delta: Prevents lower bound score from being too low for long docs (default: 1.0)
    - Field Weighting: Headers/Function names boost results.
    - Metadata-aware ranking (recency, popularity, filename match).
    """
    def __init__(self, k1=1.5, b=0.75, delta=1.0):
        self.k1 = k1
        self.b = b
        self.delta = delta
        self.corpus_size = 0
        self.avgdl = 0
        self.doc_freqs = []
        self.idf = {}
        self.doc_lengths = []
        self.metadata = [] 

    def tokenize(self, text: str) -> List[str]:
        if not text: return []
        # Case-insensitive but preserve code symbols
        raw_words = re.findall(r'\b\w+\b', text)
        tokens = []
        for word in raw_words:
            low_word = word.lower()
            tokens.append(low_word)
            # Split camel/snake
            if len(word) > 1:
                parts = re.findall(r'[A-Z]?[a-z]+|[A-Z]+(?=[A-Z][a-z]|\b)', word)
                if len(parts) > 1: tokens.extend([p.lower() for p in parts])
            if '_' in word:
                parts = word.split('_')
                tokens.extend([p.lower() for p in parts if p])
        return tokens

    def fit(self, corpus, metadata: Optional[List[Dict]] = None):
        self.corpus_size = len(corpus)
        tokenized_corpus = [self.tokenize(doc) for doc in corpus]
        self.doc_lengths = [len(doc) for doc in tokenized_corpus]
        self.avgdl = sum(self.doc_lengths) / self.corpus_size if self.corpus_size > 0 else 0
        self.metadata = metadata if metadata else [{} for _ in range(self.corpus_size)]
        
        nd = {} 
        self.doc_freqs = []
        for doc in tokenized_corpus:
            self.doc_freqs.append(Counter(doc))
            for word in set(doc):
                nd[word] = nd.get(word, 0) + 1
        
        for word, n_qi in nd.items():
            # Standard IDF
            idf_value = math.log((self.corpus_size - n_qi + 0.5) / (n_qi + 0.5) + 1)
            self.idf[word] = max(idf_value, 0.01)

    def get_scores(self, query: str, header_weight: float = 2.0, filename_weight: float = 3.0) -> List[float]:
        query_tokens = self.tokenize(query)
        scores = []
        now = time.time()
        
        # Folder Priority Weighting (As defined in search_architecture.md)
        FOLDER_BOOSTS = {
            '.agent/rules': 4.0,      # Absolute Truth
            '.agent/workflows': 3.5,  # Operational Logic
            'docs/02_standards': 3.5, # Mandatory Standards (Interception, Style, etc.)
            'docs/00_context': 3.0,   # Deep System Context
            '.agent/skills': 2.5,     # Capabilities
            'docs/06_patterns': 2.5,  # Agent Mandates
            'docs/01_specs': 2.0,     # Feature Blueprints
            'docs/99_archive': 0.1    # Deep Penalty (Avoid legacy)
        }
        
        # Pre-calculate query matches in filename for efficiency
        query_parts = set(query_tokens)
        
        for i in range(self.corpus_size):
            score = 0
            doc_freq = self.doc_freqs[i]
            d_len = self.doc_lengths[i]
            meta = self.metadata[i]
            
            # Header content (the 'type' or function name)
            header_text = meta.get('header', '').lower()
            file_path = meta.get('file_path', '').lower().replace('\\', '/')
            
            # 1. Base BM25+ Score
            for token in query_tokens:
                if token not in self.idf: continue
                f_qi_d = doc_freq.get(token, 0)
                idf = self.idf[token]
                
                # BM25+ formula
                numerator = f_qi_d * (self.k1 + 1)
                denominator = f_qi_d + self.k1 * (1 - self.b + self.b * (d_len / self.avgdl))
                score += idf * (numerator / denominator + self.delta)
            
            # 2. Field Weighting (Header Boost)
            for token in query_tokens:
                if token in header_text:
                    score += (score * 0.25 * header_weight)
            
            # 3. Filename Boost (Strongest signal)
            filename = os.path.basename(file_path)
            for part in query_parts:
                if part in filename:
                    score += (score * 0.4 * filename_weight)

            # 4. Folder Priority Weighting
            for folder, boost in FOLDER_BOOSTS.items():
                if file_path.startswith(folder):
                    score *= boost
                    break

            # 5. Metadata Boosting
            if meta:
                # Recency Decay (Exponential)
                mtime = float(meta.get('last_modified', 0))
                if mtime > 0:
                    age_days = (now - mtime) / 86400
                    # Steeper decay for very old files
                    recency_boost = math.exp(-0.01 * age_days) 
                    score += score * 0.2 * recency_boost
                
                # Popularity
                hits = int(meta.get('hits', 0))
                if hits > 0:
                    score += score * 0.08 * math.log1p(hits)
                    
            scores.append(score)
        return scores

# Utility wrappers for convenience (keeping old names to avoid breaking search.py)
BM25 = BM25Plus

def save_index_binary(bin_path, data):
    with open(bin_path, 'wb') as f:
        pickle.dump(data, f)

def load_index_binary(bin_path):
    with open(bin_path, 'rb') as f:
        return pickle.load(f)

def save_index_csv(csv_path, data):
    if not data: return
    # Collect all possible fieldnames from all entries
    all_fields = set()
    for item in data:
        all_fields.update(item.keys())
    
    # Sort for consistent ordering
    fieldnames = sorted(all_fields)
    
    with open(csv_path, 'w', encoding='utf-8', newline='') as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(data)

def load_index_csv(csv_path):
    if not os.path.exists(csv_path): return []
    with open(csv_path, 'r', encoding='utf-8') as f:
        return list(csv.DictReader(f))

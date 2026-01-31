#!/usr/bin/env python3
"""
Analyzes speedscope.json files to show hierarchical call tree of hot methods.

Displays a pure nested tree view starting from the primary entry point in the target
namespace, excluding benchmark harness and setup code overhead.

Usage:
    ./analyze_speedscope.py <speedscope.json> [options]

Options:
    --namespace <ns>     Root namespace to analyze (default: Atomic.Net.MonoGame)
    --min-pct <pct>      Only show tree nodes above this % threshold (default: 0.5)
    --output <file>      Write report to file instead of stdout
"""

import json
import sys
from collections import defaultdict
from argparse import ArgumentParser

def parse_args():
    """Parse command line arguments."""
    parser = ArgumentParser(description='Analyze speedscope.json with hierarchical tree view')
    parser.add_argument('trace_file', help='Path to speedscope.json file')
    parser.add_argument('--namespace', default='Atomic.Net.MonoGame', 
                       help='Root namespace to analyze (default: Atomic.Net.MonoGame)')
    parser.add_argument('--min-pct', type=float, default=0.5,
                       help='Only show tree nodes above this percent threshold (default: 0.5)')
    parser.add_argument('--output', help='Write report to file instead of stdout')
    return parser.parse_args()

def should_skip_method(method_name):
    """Check if method should be skipped (setup, harness, unmanaged, etc.)."""
    skip_patterns = [
        'UNMANAGED_CODE_TIME',
        'GlobalSetup',
        'GlobalCleanup', 
        'IterationSetup',
        'IterationCleanup',
        'BenchmarkDotNet.',
        'Perfolizer.',
        '..cctor()',
        '.cctor()',
        '.Initialize()',
        'BeforeAnythingElse',
        'AfterAll',
        'BeforeActualRun',
        'AfterActualRun',
        'OverheadJitting',
        'WorkloadJitting',
        'OverheadWarmup',
        'OverheadActual',
        'WorkloadWarmup',
    ]
    return any(pattern in method_name for pattern in skip_patterns)

def build_call_tree(events, frame_names):
    """Build tree with inclusive time and parent-child relationships."""
    tree = defaultdict(lambda: {'time': 0.0, 'children': defaultdict(float)})
    stack = []
    
    for event in events:
        event_type = event['type']
        frame_idx = event['frame']
        timestamp = event['at']
        
        if event_type == 'O':
            stack.append((frame_idx, timestamp))
        elif event_type == 'C':
            if stack:
                opened_frame, start_time = stack.pop()
                if opened_frame == frame_idx:
                    duration = timestamp - start_time
                    tree[frame_idx]['time'] += duration
                    
                    if stack:
                        parent_idx = stack[-1][0]
                        tree[parent_idx]['children'][frame_idx] += duration
    
    return tree

def get_method_name(frame_idx, frame_names):
    """Extract method name from frame."""
    if frame_idx not in frame_names:
        return f"<frame {frame_idx}>"
    name = frame_names[frame_idx]
    return name.split('!', 1)[1] if '!' in name else name

def find_primary_root(tree, frame_names, namespace_filter):
    """
    Find the primary root - the method in our namespace with the most inclusive time
    that isn't a setup/harness method.
    """
    best_root = None
    best_time = 0.0
    
    for frame_idx, data in tree.items():
        if frame_idx not in frame_names:
            continue
        
        name = frame_names[frame_idx]
        if namespace_filter not in name or '!' not in name:
            continue
        
        method_name = get_method_name(frame_idx, frame_names)
        if should_skip_method(method_name):
            continue
        
        # This is a valid namespace method
        inclusive_time = data['time']
        if inclusive_time > best_time:
            best_time = inclusive_time
            best_root = frame_idx
    
    return best_root, best_time

def print_tree(frame_idx, tree, frame_names, total_time, min_pct, depth=0, prefix="", visited=None):
    """Recursively print call tree."""
    if visited is None:
        visited = set()
    
    if frame_idx in visited or frame_idx not in frame_names:
        return []
    
    visited = visited.copy()
    visited.add(frame_idx)
    lines = []
    
    method_name = get_method_name(frame_idx, frame_names)
    
    # Skip unwanted methods
    if should_skip_method(method_name):
        # But traverse children
        data = tree.get(frame_idx, {'children': {}})
        for child_idx in data.get('children', {}).keys():
            lines.extend(print_tree(child_idx, tree, frame_names, total_time, min_pct, depth, prefix, visited))
        return lines
    
    data = tree.get(frame_idx, {'time': 0.0, 'children': {}})
    inclusive_time = data['time']
    pct = (inclusive_time / total_time * 100) if total_time > 0 else 0
    
    if pct < min_pct:
        return lines
    
    # Format line
    if depth == 0:
        line = f"{inclusive_time:12.3f} ({pct:6.2f}%) {method_name}"
    else:
        line = f"{prefix}{inclusive_time:12.3f} ({pct:6.2f}%) {method_name}"
    lines.append(line)
    
    # Process children
    children = data.get('children', {})
    if children:
        # Sort by time
        sorted_children = sorted(children.items(), key=lambda x: x[1], reverse=True)
        
        # Filter by threshold
        significant = [(idx, time) for idx, time in sorted_children
                      if (time / total_time * 100) >= min_pct]
        
        for i, (child_idx, _) in enumerate(significant):
            is_last = (i == len(significant) - 1)
            
            if is_last:
                child_prefix = prefix + "└─ "
                next_prefix = prefix + "   "
            else:
                child_prefix = prefix + "├─ "
                next_prefix = prefix + "│  "
            
            child_lines = print_tree(child_idx, tree, frame_names, total_time, min_pct, depth + 1, next_prefix, visited)
            
            if child_lines:
                # Fix first line prefix
                first = child_lines[0]
                if first.startswith(next_prefix):
                    first = first[len(next_prefix):]
                child_lines[0] = child_prefix + first
                lines.extend(child_lines)
    
    return lines

def main():
    """Main entry point."""
    args = parse_args()
    
    try:
        print(f"Loading {args.trace_file}...")
        with open(args.trace_file) as f:
            data = json.load(f)
        
        frames = data['shared']['frames']
        profiles = data['profiles']
        
        print(f"Found {len(frames)} frames, {len(profiles)} profiles")
        
        frame_names = {i: frame['name'] for i, frame in enumerate(frames)}
        main_profile = max(profiles, key=lambda p: len(p['events']))
        events = main_profile['events']
        
        print(f"Analyzing thread: {main_profile['name']} ({len(events)} events)")
        
        # Build tree
        print("Building call tree...")
        tree = build_call_tree(events, frame_names)
        print(f"Built tree with {len(tree)} nodes")
        
        # Find primary root
        print(f"Finding primary root in {args.namespace}...")
        root_idx, root_time = find_primary_root(tree, frame_names, args.namespace)
        
        if root_idx is None:
            print(f"Error: No methods found in namespace {args.namespace}", file=sys.stderr)
            return 1
        
        root_method = get_method_name(root_idx, frame_names)
        print(f"Root method: {root_method}")
        print(f"Root inclusive time: {root_time:.2f}")
        
        # Generate tree
        lines = []
        lines.append("=" * 100)
        lines.append(f"HIERARCHICAL CALL TREE - {args.namespace}")
        lines.append(f"Root method: {root_method}")
        lines.append(f"Total time: {root_time:.2f}")
        lines.append(f"Percentages relative to root time (benchmark harness excluded)")
        lines.append(f"Minimum threshold: {args.min_pct}%")
        lines.append("=" * 100)
        lines.append("")
        
        tree_lines = print_tree(root_idx, tree, frame_names, root_time, args.min_pct)
        lines.extend(tree_lines)
        
        lines.append("")
        lines.append("=" * 100)
        lines.append("SUMMARY")
        lines.append("=" * 100)
        lines.append(f"Total time analyzed: {root_time:.2f}")
        lines.append(f"Threshold: {args.min_pct}%")
        lines.append("Setup/harness/unmanaged methods excluded from tree")
        
        report = '\n'.join(lines)
        
        if args.output:
            with open(args.output, 'w') as f:
                f.write(report)
            print(f"\nReport written to {args.output}")
        else:
            print("\n" + report)
        
        return 0
    
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        import traceback
        traceback.print_exc()
        return 1

if __name__ == '__main__':
    sys.exit(main())

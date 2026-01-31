#!/usr/bin/env python3
"""
Analyzes speedscope.json files to find hot methods in specified namespaces.
This script processes EventPipe traces to identify where time is being spent.

Usage:
    ./analyze_speedscope.py <speedscope.json> [options]

Options:
    --namespace <ns>     Filter methods by namespace (default: Atomic.Net.MonoGame)
    --top <n>            Show top N methods (default: 50)
    --min-pct <pct>      Only show methods above this % threshold (default: 0.01)
    --exclude-init       Exclude initialization methods (default: True)
    --output <file>      Write report to file instead of stdout
    --call-tree          Show hierarchical call tree for top hot methods
    --tree-depth <n>     Max depth for call tree display (default: 5)
"""

import json
import sys
from collections import defaultdict, Counter
from argparse import ArgumentParser

def parse_args():
    """Parse command line arguments."""
    parser = ArgumentParser(description='Analyze speedscope.json traces for hot methods')
    parser.add_argument('trace_file', help='Path to speedscope.json file')
    parser.add_argument('--namespace', default='Atomic.Net.MonoGame', 
                       help='Filter methods by namespace (default: Atomic.Net.MonoGame)')
    parser.add_argument('--top', type=int, default=50,
                       help='Show top N methods (default: 50)')
    parser.add_argument('--min-pct', type=float, default=0.01,
                       help='Only show methods above this percent threshold (default: 0.01)')
    parser.add_argument('--exclude-init', action='store_true', default=True,
                       help='Exclude initialization methods (default: True)')
    parser.add_argument('--include-init', action='store_false', dest='exclude_init',
                       help='Include initialization methods')
    parser.add_argument('--output', help='Write report to file instead of stdout')
    parser.add_argument('--include-system', action='store_true',
                       help='Include System.* methods in analysis')
    parser.add_argument('--call-tree', action='store_true',
                       help='Show hierarchical call tree for hot methods')
    parser.add_argument('--tree-depth', type=int, default=5,
                       help='Maximum depth for call tree display (default: 5)')
    return parser.parse_args()

def build_call_tree(events, frame_names, filtered_frames):
    """Build hierarchical call tree from events."""
    # Structure: {parent_frame: {child_frame: time_spent}}
    call_relationships = defaultdict(lambda: defaultdict(float))
    stack = []
    
    for event in events:
        event_type = event['type']
        frame_idx = event['frame']
        timestamp = event['at']
        
        if event_type == 'O':  # Open
            # Record parent-child relationship if there's a parent
            if stack:
                parent_idx = stack[-1][0]
                call_relationships[parent_idx][frame_idx] = 0.0  # Initialize
            stack.append((frame_idx, timestamp))
        elif event_type == 'C':  # Close
            if stack:
                opened_frame, start_time = stack.pop()
                if opened_frame == frame_idx:
                    duration = timestamp - start_time
                    
                    # Attribute time to parent->child relationship
                    if stack:  # If there's a parent on the stack
                        parent_idx = stack[-1][0]
                        call_relationships[parent_idx][frame_idx] += duration
    
    return call_relationships

def get_method_name(frame_idx, frame_names, filtered_frames):
    """Get clean method name for a frame."""
    if frame_idx in filtered_frames:
        return filtered_frames[frame_idx]
    elif frame_idx in frame_names:
        name = frame_names[frame_idx]
        if '!' in name:
            return name.split('!')[1]
        return name
    return f"<unknown frame {frame_idx}>"

def print_call_tree(parent_idx, call_relationships, frame_names, filtered_frames, 
                   total_time, depth=0, max_depth=5, min_pct=0.5, visited=None):
    """Recursively print call tree."""
    if depth >= max_depth:
        return []
    
    if visited is None:
        visited = set()
    
    if parent_idx in visited:
        return []  # Prevent infinite recursion
    
    visited.add(parent_idx)
    lines = []
    
    children = call_relationships.get(parent_idx, {})
    if not children:
        return lines
    
    # Sort children by time spent
    sorted_children = sorted(children.items(), key=lambda x: x[1], reverse=True)
    
    for child_idx, time_spent in sorted_children:
        pct = (time_spent / total_time * 100) if total_time > 0 else 0
        
        # Skip very small contributors
        if pct < min_pct and depth > 0:
            continue
        
        indent = "  " * depth
        method_name = get_method_name(child_idx, frame_names, filtered_frames)
        
        # Truncate long method names
        if len(method_name) > 100:
            method_name = method_name[:97] + "..."
        
        lines.append(f"{indent}├─ {time_spent:10.3f} ({pct:5.2f}%) {method_name}")
        
        # Recurse into children
        child_lines = print_call_tree(
            child_idx, call_relationships, frame_names, filtered_frames,
            total_time, depth + 1, max_depth, min_pct, visited.copy()
        )
        lines.extend(child_lines)
    
    return lines

def analyze_speedscope(filename, namespace_filter='Atomic.Net.MonoGame', 
                       exclude_init=True, include_system=False, build_tree=False):
    """Parse speedscope JSON and extract hot methods."""
    print(f"Loading {filename}...")
    with open(filename, 'r') as f:
        data = json.load(f)
    
    frames = data['shared']['frames']
    profiles = data['profiles']
    
    print(f"Found {len(frames)} unique frames")
    print(f"Found {len(profiles)} profiles (threads)")
    
    # Build frame lookup
    frame_names = {i: frame['name'] for i, frame in enumerate(frames)}
    
    # Find the main thread (one with most events)
    main_profile = max(profiles, key=lambda p: len(p['events']))
    events = main_profile['events']
    print(f"\nAnalyzing main thread: {main_profile['name']}")
    print(f"  Events: {len(events)}")
    
    # Track frame open/close times
    stack = []
    frame_times = defaultdict(float)  # exclusive time
    frame_samples = Counter()  # inclusive samples
    
    for event in events:
        event_type = event['type']
        frame_idx = event['frame']
        timestamp = event['at']
        
        if event_type == 'O':  # Open
            stack.append((frame_idx, timestamp))
            frame_samples[frame_idx] += 1
        elif event_type == 'C':  # Close
            if stack:
                opened_frame, start_time = stack.pop()
                if opened_frame == frame_idx:
                    duration = timestamp - start_time
                    frame_times[frame_idx] += duration
    
    print(f"\nProcessed {len(events)} events")
    print(f"Unique frames with time: {len(frame_times)}")
    
    # Build call tree if requested
    call_tree = None
    if build_tree:
        print("Building call tree...")
        call_tree = build_call_tree(events, frame_names, {})
        print(f"Call tree built with {len(call_tree)} parent nodes")
    
    # Filter for target namespace methods
    filtered_frames = {}
    for frame_idx, name in frame_names.items():
        # Check if this is a method from our target namespace
        if namespace_filter in name and '!' in name:
            # Extract method part (after '!')
            parts = name.split('!')
            if len(parts) > 1:
                method = parts[1]
                
                # Apply filters
                if exclude_init:
                    # Skip common initialization patterns
                    skip_patterns = [
                        '.Initialize()',
                        '..cctor()',
                        '..ctor()',
                        'GlobalSetup',
                        'GlobalCleanup',
                        'IterationSetup',
                        'IterationCleanup'
                    ]
                    if any(pattern in method for pattern in skip_patterns):
                        continue
                
                filtered_frames[frame_idx] = method
        
        # Optionally include System.* methods for comparison
        elif include_system and name.startswith('System.') and '!' in name:
            parts = name.split('!')
            if len(parts) > 1:
                filtered_frames[frame_idx] = parts[1]
    
    print(f"\nFound {len(filtered_frames)} filtered method frames")
    
    # Sort by exclusive time
    sorted_by_time = [(idx, frame_times.get(idx, 0), filtered_frames[idx]) 
                      for idx in filtered_frames.keys()]
    sorted_by_time.sort(key=lambda x: x[1], reverse=True)
    
    # Sort by inclusive samples
    sorted_by_samples = [(idx, frame_samples.get(idx, 0), filtered_frames[idx])
                         for idx in filtered_frames.keys()]
    sorted_by_samples.sort(key=lambda x: x[1], reverse=True)
    
    return {
        'sorted_by_time': sorted_by_time,
        'sorted_by_samples': sorted_by_samples,
        'total_time': sum(frame_times.values()),
        'total_samples': sum(frame_samples.values()),
        'frame_times': frame_times,
        'frame_samples': frame_samples,
        'call_tree': call_tree,
        'frame_names': frame_names,
        'filtered_frames': filtered_frames
    }

def format_report(results, top_n=50, min_pct=0.01, namespace='Atomic.Net.MonoGame',
                 show_call_tree=False, tree_depth=5):
    """Format analysis results as a report."""
    lines = []
    
    sorted_by_time = results['sorted_by_time']
    sorted_by_samples = results['sorted_by_samples']
    total_time = results['total_time']
    total_samples = results['total_samples']
    
    lines.append("=" * 100)
    lines.append(f"TOP {top_n} METHODS BY EXCLUSIVE TIME (actual work done)")
    lines.append(f"Namespace: {namespace}")
    lines.append("=" * 100)
    
    count = 0
    for i, (idx, time_val, method) in enumerate(sorted_by_time, 1):
        pct = (time_val / total_time * 100) if total_time > 0 else 0
        if pct < min_pct and i > 10:  # Always show top 10
            continue
        lines.append(f"{i:3d}. {time_val:12.6f} ({pct:6.2f}%) - {method}")
        count += 1
        if count >= top_n:
            break
    
    lines.append("\n" + "=" * 100)
    lines.append(f"TOP {top_n} METHODS BY INCLUSIVE SAMPLES (on call stack)")
    lines.append(f"Namespace: {namespace}")
    lines.append("=" * 100)
    
    count = 0
    for i, (idx, samples, method) in enumerate(sorted_by_samples, 1):
        pct = (samples / total_samples * 100) if total_samples > 0 else 0
        if pct < min_pct and i > 10:  # Always show top 10
            continue
        lines.append(f"{i:3d}. {samples:8d} samples ({pct:5.2f}%) - {method}")
        count += 1
        if count >= top_n:
            break
    
    # Add call tree analysis if requested
    if show_call_tree and results['call_tree']:
        lines.append("\n" + "=" * 100)
        lines.append(f"CALL TREE ANALYSIS (Top 10 Hot Methods)")
        lines.append(f"Shows hierarchical breakdown of where time is spent within each method")
        lines.append(f"Max depth: {tree_depth}, Min % to show: 0.5%")
        lines.append("=" * 100)
        
        call_tree = results['call_tree']
        frame_names = results['frame_names']
        filtered_frames = results['filtered_frames']
        
        # Show call tree for top 10 hot methods
        for i, (idx, time_val, method) in enumerate(sorted_by_time[:10], 1):
            pct = (time_val / total_time * 100) if total_time > 0 else 0
            lines.append(f"\n{i}. {method}")
            lines.append(f"   Exclusive time: {time_val:.3f} ({pct:.2f}%)")
            lines.append(f"   Children breakdown:")
            
            tree_lines = print_call_tree(
                idx, call_tree, frame_names, filtered_frames,
                total_time, depth=0, max_depth=tree_depth, min_pct=0.5
            )
            
            if tree_lines:
                lines.extend(["   " + line for line in tree_lines])
            else:
                lines.append("   └─ (no significant children or leaf node)")
    
    lines.append("\n" + "=" * 100)
    lines.append("SUMMARY")
    lines.append("=" * 100)
    lines.append(f"Total execution time (all methods): {total_time:.2f} time units")
    lines.append(f"Total samples (all methods): {total_samples:,}")
    lines.append(f"Methods analyzed: {len(results['sorted_by_time'])}")
    lines.append(f"Showing methods with >= {min_pct}% of total time")
    
    return '\n'.join(lines)

def main():
    """Main entry point."""
    args = parse_args()
    
    try:
        results = analyze_speedscope(
            args.trace_file,
            namespace_filter=args.namespace,
            exclude_init=args.exclude_init,
            include_system=args.include_system,
            build_tree=args.call_tree
        )
        
        report = format_report(
            results,
            top_n=args.top,
            min_pct=args.min_pct,
            namespace=args.namespace,
            show_call_tree=args.call_tree,
            tree_depth=args.tree_depth
        )
        
        if args.output:
            with open(args.output, 'w') as f:
                f.write(report)
            print(f"\nReport written to {args.output}")
        else:
            print("\n" + report)
        
        return 0
    
    except FileNotFoundError:
        print(f"Error: File not found: {args.trace_file}", file=sys.stderr)
        return 1
    except json.JSONDecodeError as e:
        print(f"Error: Invalid JSON in {args.trace_file}: {e}", file=sys.stderr)
        return 1
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        import traceback
        traceback.print_exc()
        return 1

if __name__ == '__main__':
    sys.exit(main())

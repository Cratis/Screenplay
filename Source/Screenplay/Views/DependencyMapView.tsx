// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useId, useMemo, useState, type KeyboardEvent } from 'react';
import { dependencyKinds, type DependencyKind } from '@cratis/screenplay-compiler';
import { layoutDependencyMap, type DependencyMap, type DependencyMapEdge, type DependencyMapPosition } from '@cratis/screenplay-event-models';
import { DependencyMapLevel } from './DependencyMapLevel';
import { dependencyKindLabels } from './dependencyKindLabels';
import { visibleDependencyEdges } from './visibleDependencyEdges';
import { groupDependencyEvidence } from './groupDependencyEvidence';

export interface DependencyMapViewProps {
    readonly map: DependencyMap;
    readonly selectedEdgeId?: string;
    /** Identifies which model the map shows (for example Current or Proposed). A refresh of the same model keeps the selection; a different key clears it. */
    readonly modelKey?: unknown;
    readonly onShowSource?: (line: number, path?: string) => void;
}

const orderingKinds: readonly DependencyKind[] = ['usesFactsFrom', 'reactsTo', 'decidesFrom'];
const nameOf = (node: DependencyMapPosition) => node.kind === 'feature' ? node.scope.join(' / ') : node.scope[0];

/** The same serializable map is drawn in both hosts, without a canvas or a graph-library runtime. */
export const DependencyMapView = ({ map, selectedEdgeId, modelKey, onShowSource }: DependencyMapViewProps) => {
    const [level, setLevel] = useState(DependencyMapLevel.Module);
    const [kinds, setKinds] = useState<readonly DependencyKind[]>(orderingKinds);
    const [selected, setSelected] = useState({ modelKey, id: selectedEdgeId });
    const [focusedKey, setFocusedKey] = useState<string | undefined>();
    const chosen = selected.modelKey === modelKey ? selected.id : undefined;
    const setSelection = (id: string | undefined) => setSelected({ modelKey, id });
    // Changing Current/Proposed clears only selection, not the level or kind filters. A refresh of the same
    // model keeps the selection for as long as the selected node or edge still exists.
    if (selected.modelKey !== modelKey) setSelected({ modelKey, id: undefined });
    const markerId = useId().replaceAll(':', '');
    const edges = useMemo(() => visibleDependencyEdges(map, level, kinds), [map, level, kinds]);
    const layout = useMemo(() => layoutDependencyMap({ ...map, edges }), [map, edges]);
    const positions = useMemo(() => new Map(layout.nodes.map(node => [node.key, node])), [layout]);
    const routes = useMemo(() => new Map(layout.edges.map(edge => [edge.id, edge])), [layout]);
    const focusableKeys = [...edges.map(edge => edge.id), ...layout.nodes.map(node => node.key)];
    const tabStop = focusedKey && focusableKeys.includes(focusedKey) ? focusedKey : focusableKeys[0];
    // Hiding an item through the filters is not removing it: validate against the unfiltered map.
    const exists = chosen !== undefined && (map.edges.some(edge => edge.id === chosen) || map.nodes.some(node => node.key === chosen));
    const selection = exists ? chosen : undefined;
    if (chosen !== undefined && selection === undefined) setSelected({ modelKey, id: undefined });
    const selectedEdge = edges.find(edge => edge.id === selection);
    const selectedNode = positions.get(selection ?? '');
    const shownSelection = selectedEdge || selectedNode ? selection : undefined;
    const labelOf = (edge: DependencyMapEdge, withUnits = false) => dependencyKinds.flatMap(kind => {
        const count = edge.byKind[kind] ?? 0;
        return count > 0 ? [`${dependencyKindLabels[kind]} ${count}${withUnits ? ` ${count === 1 ? 'reference' : 'references'}` : ''}`] : [];
    }).join(' · ');
    const accessibleLabelOf = (edge: DependencyMapEdge) => `${nameOf(positions.get(edge.source)!)} ${dependencyKinds.filter(kind => (edge.byKind[kind] ?? 0) > 0).map(kind => dependencyKindLabels[kind]).join(', ')} ${nameOf(positions.get(edge.target)!)}, ${edge.sliceEdges} ${edge.sliceEdges === 1 ? 'slice pair' : 'slice pairs'}`;
    const selectWithKeyboard = (event: KeyboardEvent<SVGGElement>, key: string) => {
        if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setSelection(key); }
        if (event.key.startsWith('Arrow')) {
            event.preventDefault();
            const elements = [...event.currentTarget.ownerSVGElement!.querySelectorAll<SVGGElement>('[role="button"]')];
            const direction = event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? -1 : 1;
            elements[(elements.indexOf(event.currentTarget) + direction + elements.length) % elements.length].focus();
        }
    };

    return (
        <section className='screenplay-dependency-map' aria-label='Dependency map' onKeyDown={event => { if (event.key === 'Escape') setSelection(undefined); }}>
            <div className='screenplay-dependency-map__controls'>
                <label>Edges between <select value={level} onChange={event => { setLevel(event.target.value as DependencyMapLevel); setSelection(undefined); }}>
                    <option value={DependencyMapLevel.Module}>Modules</option><option value={DependencyMapLevel.Feature}>Features</option>
                </select></label>
                <fieldset><legend>Dependency kinds</legend>{dependencyKinds.map(kind => (
                    <label key={kind}><input type='checkbox' value={kind} checked={kinds.includes(kind)} onChange={event => {
                        setKinds(event.target.checked ? [...kinds, kind] : kinds.filter(selected => selected !== kind));
                    }} />{dependencyKindLabels[kind]}</label>
                ))}</fieldset>
                <span>Consumer → producer. Dashed edges cross modules or leave the model.</span>
            </div>
            <div className='screenplay-dependency-map__drawing'>
                <svg width={layout.width} height={layout.height} viewBox={`0 0 ${layout.width} ${layout.height}`} aria-label='Modules, features and bounded contexts'>
                    <defs><marker id={markerId} viewBox='0 0 10 10' refX='9' refY='5' markerWidth='7' markerHeight='7' orient='auto-start-reverse'><path d='M 0 0 L 10 5 L 0 10 z' fill='currentColor' /></marker></defs>
                    {map.modules.map(key => {
                        const node = positions.get(key)!;
                        const features = layout.nodes.filter(feature => feature.kind === 'feature' && feature.scope[0] === node.scope[0]);
                        const bottom = Math.max(node.y + node.height, ...features.map(feature => feature.y + feature.height));
                        return <rect key={key} className='screenplay-dependency-map__column' x={node.x - 12} y={node.y - 12} width={node.width + 24} height={bottom - node.y + 24} rx='8' />;
                    })}
                    {map.contexts.length > 0 && <text x={positions.get(map.contexts[0])!.x} y={24}>Other bounded contexts</text>}
                    {edges.map(edge => {
                        const route = routes.get(edge.id)!;
                        const selected = edge.id === selection;
                        const count = String(edge.sliceEdges);
                        return <g key={edge.id} role='button' aria-label={accessibleLabelOf(edge)} tabIndex={tabStop === edge.id ? 0 : -1} aria-pressed={selected}
                            className={`screenplay-dependency-map__edge${selected ? ' is-selected' : ''}`} onFocus={() => setFocusedKey(edge.id)}
                            onClick={event => { event.currentTarget.focus(); setSelection(edge.id); }} onKeyDown={event => selectWithKeyboard(event, edge.id)}>
                            <path d={route.path} className='screenplay-dependency-map__edge-hit' />
                            <path d={route.path} fill='none' strokeDasharray={edge.crossing ? '8 4' : undefined} markerEnd={`url(#${markerId})`} />
                            <title>{`${edge.sliceEdges} ${edge.sliceEdges === 1 ? 'slice pair' : 'slice pairs'} · ${labelOf(edge, true)}`}</title>
                            <text x={route.labelX} y={route.labelY} textAnchor='middle' textLength={Math.min(route.labelWidth, count.length * 7)} lengthAdjust='spacingAndGlyphs'>{count}</text>
                        </g>;
                    })}
                    {layout.nodes.map(node => <g key={node.key} role='button' aria-label={`${node.kind === 'module' ? 'Module' : node.kind === 'feature' ? 'Feature' : 'Bounded context'} ${nameOf(node)}`} tabIndex={tabStop === node.key ? 0 : -1} aria-pressed={selection === node.key}
                        className={`screenplay-dependency-map__node${selection === node.key ? ' is-selected' : ''}`} onFocus={() => setFocusedKey(node.key)}
                        onClick={event => { event.currentTarget.focus(); setSelection(node.key); }} onKeyDown={event => selectWithKeyboard(event, node.key)}>
                        <rect x={node.x} y={node.y} width={node.width} height={node.height} rx='6' strokeDasharray={node.kind === 'context' ? '4 4' : undefined} />
                        <text x={node.x + 12} y={node.y + 28}>{nameOf(node).length > 30 ? `${nameOf(node).slice(0, 27)}…` : nameOf(node)}</text>
                        <text className='screenplay-dependency-map__node-kind' x={node.x + 12} y={node.y + 48}>{node.kind === 'context' ? 'bounded context' : node.kind}</text>
                    </g>)}
                </svg>
                {map.modules.length === 0 && <p>No modules in this model.</p>}
            </div>
            <p className='screenplay-dependency-map__accessible' role='status' aria-live='polite' aria-atomic='true'>{selectedEdge ? `${accessibleLabelOf(selectedEdge)} selected` : selectedNode ? `${nameOf(selectedNode)} selected` : 'No dependency selected'}</p>
            <aside className='screenplay-dependency-map__details' aria-label='Dependency details'>
                {selectedEdge ? <>
                    <h2>{accessibleLabelOf(selectedEdge)}</h2>
                    <ul>{groupDependencyEvidence(map, selectedEdge).map(group => <li key={group.key} className='screenplay-dependency-map__slice-pair'>
                        <span>{group.consumer} → {group.producer}</span>
                        <ul>{group.references.map((item, index) => <li key={index}>
                            {dependencyKindLabels[item.kind]}: {item.name}{' '}
                            {onShowSource ? <button type='button' onClick={() => onShowSource(item.location.line, item.location.path)}>{item.location.path ?? 'document'}:{item.location.line}:{item.location.column}</button> : <span>({item.location.path ?? 'document'}:{item.location.line}:{item.location.column})</span>}
                            {item.ambiguous && <span> — ambiguous; alternatives: {item.alternatives.join(', ')}</span>}
                        </li>)}</ul>
                    </li>)}</ul>
                </> : selectedNode ? <><h2>{nameOf(selectedNode)}</h2><p>Select an edge to see its consumer and producer slices.</p></> : <p>Select an edge to see the slices behind it. Press Escape to clear the selection.</p>}
                {shownSelection && <button type='button' onClick={() => setSelection(undefined)}>Clear selection</button>}
            </aside>
            <table className='screenplay-dependency-map__accessible'><caption>All dependencies</caption>
                <thead><tr><th>Consumer</th><th>Producer</th><th>Kinds and references</th><th>Slice pairs</th></tr></thead>
                <tbody>{map.edges.map(edge => <tr key={edge.id}><td>{nameOf(positions.get(edge.source)!)}</td><td>{nameOf(positions.get(edge.target)!)}</td><td>{labelOf(edge)}</td><td>{edge.sliceEdges}</td></tr>)}</tbody>
            </table>
        </section>
    );
};

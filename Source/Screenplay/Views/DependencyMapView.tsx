// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useId, useMemo, useState, type KeyboardEvent } from 'react';
import { dependencyKinds, type DependencyKind } from '@cratis/screenplay-compiler';
import { layoutDependencyMap, type DependencyMap, type DependencyMapEdge, type DependencyMapPosition } from '@cratis/screenplay-event-models';
import { DependencyMapLevel } from './DependencyMapLevel';
import { dependencyKindLabels } from './dependencyKindLabels';
import { visibleDependencyEdges } from './visibleDependencyEdges';

export interface DependencyMapViewProps {
    readonly map: DependencyMap;
    readonly selectedEdgeId?: string;
    readonly onShowSource?: (line: number, path?: string) => void;
}

const orderingKinds: readonly DependencyKind[] = ['usesFactsFrom', 'reactsTo', 'decidesFrom'];
const nameOf = (node: DependencyMapPosition) => node.kind === 'feature' ? node.scope.join(' / ') : node.scope[0];

/** The same serializable map is drawn in both hosts, without a canvas or a graph-library runtime. */
export const DependencyMapView = ({ map, selectedEdgeId, onShowSource }: DependencyMapViewProps) => {
    const [level, setLevel] = useState(DependencyMapLevel.Module);
    const [kinds, setKinds] = useState<readonly DependencyKind[]>(orderingKinds);
    const [selection, setSelection] = useState<string | undefined>(selectedEdgeId);
    const markerId = useId().replaceAll(':', '');
    const edges = useMemo(() => visibleDependencyEdges(map, level, kinds), [map, level, kinds]);
    const layout = useMemo(() => layoutDependencyMap({ ...map, edges }), [map, edges]);
    const positions = useMemo(() => new Map(layout.nodes.map(node => [node.key, node])), [layout]);
    const selectedEdge = edges.find(edge => edge.id === selection);
    const selectedNode = positions.get(selection ?? '');
    const labelOf = (edge: DependencyMapEdge) => dependencyKinds.flatMap(kind => {
        const count = edge.evidence.filter(item => item.kind === kind).length;
        return count > 0 ? [`${dependencyKindLabels[kind]} ${count}`] : [];
    }).join(' · ');
    const accessibleLabelOf = (edge: DependencyMapEdge) => `${nameOf(positions.get(edge.source)!)} ${dependencyKinds.filter(kind => edge.evidence.some(item => item.kind === kind)).map(kind => dependencyKindLabels[kind]).join(', ')} ${nameOf(positions.get(edge.target)!)}, ${edge.sliceEdges} ${edge.sliceEdges === 1 ? 'slice' : 'slices'}`;
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
                    {edges.map((edge, index) => {
                        const source = positions.get(edge.source)!;
                        const target = positions.get(edge.target)!;
                        const within = source.x === target.x;
                        const sourceX = source.x + (within || source.x < target.x ? source.width : 0);
                        const targetX = target.x + (within || source.x > target.x ? target.width : 0);
                        const sourceY = source.y + source.height / 2;
                        const targetY = target.y + target.height / 2;
                        const lane = 48 + index * 32;
                        const sourceGutter = sourceX + (source.x < target.x || within ? 24 : -24);
                        const targetGutter = targetX + (source.x > target.x || within ? 24 : -24);
                        const path = within
                            ? `M ${sourceX} ${sourceY} C ${sourceX + 64} ${sourceY}, ${targetX + 64} ${targetY}, ${targetX} ${targetY}`
                            : `M ${sourceX} ${sourceY} H ${sourceGutter} V ${lane} H ${targetGutter} V ${targetY} H ${targetX}`;
                        const selected = edge.id === selection;
                        return <g key={edge.id} role='button' aria-label={accessibleLabelOf(edge)} tabIndex={0} aria-pressed={selected}
                            className={`screenplay-dependency-map__edge${selected ? ' is-selected' : ''}`} onClick={() => setSelection(edge.id)} onKeyDown={event => selectWithKeyboard(event, edge.id)}>
                            <path d={path} className='screenplay-dependency-map__edge-hit' />
                            <path d={path} fill='none' strokeDasharray={edge.crossing ? '8 4' : undefined} markerEnd={`url(#${markerId})`} />
                            <title>{labelOf(edge)}</title>
                            <text x={within ? sourceX + 32 : (sourceX + targetX) / 2} y={within ? (sourceY + targetY) / 2 : lane - 6} textAnchor={within ? 'start' : 'middle'}>{labelOf(edge)}</text>
                        </g>;
                    })}
                    {layout.nodes.map(node => <g key={node.key} role='button' aria-label={`${node.kind === 'module' ? 'Module' : node.kind === 'feature' ? 'Feature' : 'Bounded context'} ${nameOf(node)}`} tabIndex={0} aria-pressed={selection === node.key}
                        className={`screenplay-dependency-map__node${selection === node.key ? ' is-selected' : ''}`} onClick={() => setSelection(node.key)} onKeyDown={event => selectWithKeyboard(event, node.key)}>
                        <rect x={node.x} y={node.y} width={node.width} height={node.height} rx='6' strokeDasharray={node.kind === 'context' ? '4 4' : undefined} />
                        <text x={node.x + 12} y={node.y + 28}>{nameOf(node).length > 30 ? `${nameOf(node).slice(0, 27)}…` : nameOf(node)}</text>
                        <text className='screenplay-dependency-map__node-kind' x={node.x + 12} y={node.y + 48}>{node.kind === 'context' ? 'bounded context' : node.kind}</text>
                    </g>)}
                </svg>
                {map.modules.length === 0 && <p>No modules in this model.</p>}
            </div>
            <aside className='screenplay-dependency-map__details' aria-label='Dependency details' aria-live='polite' aria-atomic='true'>
                {selectedEdge ? <>
                    <h2>{accessibleLabelOf(selectedEdge)}</h2>
                    <ul>{selectedEdge.evidence.map((item, index) => <li key={index}>
                        {onShowSource ? <button type='button' onClick={() => onShowSource(item.location.line, item.location.path)}>{item.consumer.address} → {item.producer.address}</button> : <span>{item.consumer.address} → {item.producer.address}</span>}
                        {' — '}{dependencyKindLabels[item.kind]}: {item.name} ({item.location.path ?? 'document'}:{item.location.line}:{item.location.column})
                        {item.ambiguous && <span> — ambiguous; alternatives: {item.alternatives.map(node => node.address).join(', ')}</span>}
                    </li>)}</ul>
                </> : selectedNode ? <><h2>{nameOf(selectedNode)}</h2><p>Select an edge to see its consumer and producer slices.</p></> : <p>Select an edge to see the slices behind it. Press Escape to clear the selection.</p>}
                {selection && <button type='button' onClick={() => setSelection(undefined)}>Clear selection</button>}
            </aside>
            <table className='screenplay-dependency-map__accessible'><caption>All dependencies</caption>
                <thead><tr><th>Consumer</th><th>Producer</th><th>Kinds and references</th><th>Slice pairs</th></tr></thead>
                <tbody>{map.edges.map(edge => <tr key={edge.id}><td>{nameOf(positions.get(edge.source)!)}</td><td>{nameOf(positions.get(edge.target)!)}</td><td>{labelOf(edge)}</td><td>{edge.sliceEdges}</td></tr>)}</tbody>
            </table>
        </section>
    );
};

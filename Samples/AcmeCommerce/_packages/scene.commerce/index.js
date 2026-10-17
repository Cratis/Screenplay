import { createElement } from 'react';

// @acme/scene.commerce - a Scene runtime package. `manifest` is its declaration (what a ui profile can name it as);
// `components` are the registered components, keyed `<package>:<component>`. React stays external: the host's own React is used.

const text = (properties, name, fallback) => (typeof properties?.[name] === 'string' && properties[name].length > 0 ? properties[name] : fallback);
const number = (properties, name, fallback) => (typeof properties?.[name] === 'number' ? properties[name] : fallback);

/** A product as a card: picture, name, SKU and price. */
function ProductCard({ element }) {
    const properties = element.properties;
    const price = number(properties, 'price', undefined);
    return createElement('article', { className: 'acme-product-card', 'data-acme-product-card': element.id },
        createElement('div', { className: 'acme-product-card__picture', role: 'img', 'aria-label': text(properties, 'name', 'Product') }),
        createElement('h3', { className: 'acme-product-card__name' }, text(properties, 'name', 'Product')),
        createElement('p', { className: 'acme-product-card__sku' }, text(properties, 'sku', 'ACM-0000')),
        price === undefined ? null : createElement('p', { className: 'acme-product-card__price' }, `${price} ${text(properties, 'currency', 'NOK')}`));
}

/** Stock at a glance: in stock, running low, or out - decided from on hand against the reorder point. */
function StockBadge({ element }) {
    const properties = element.properties;
    const onHand = number(properties, 'onHand', 0);
    const reorderPoint = number(properties, 'reorderPoint', 10);
    const level = onHand <= 0 ? 'out' : onHand < reorderPoint ? 'low' : 'ok';
    const label = level === 'out' ? 'Out of stock' : level === 'low' ? `Low: ${onHand}` : `In stock: ${onHand}`;
    return createElement('span', { className: `acme-stock-badge acme-stock-badge--${level}`, 'data-acme-stock-badge': level, title: text(properties, 'sku', '') }, label);
}

/** The story of one order: each status it went through, oldest first, the last one marked as current. */
function OrderTimeline({ element }) {
    const properties = element.properties;
    const steps = Array.isArray(properties?.steps) ? properties.steps : [];
    return createElement('ol', { className: 'acme-order-timeline', 'data-acme-order-timeline': element.id, 'aria-label': text(properties, 'title', 'Order timeline') },
        steps.length === 0
            ? createElement('li', { className: 'acme-order-timeline__empty' }, 'Nothing has happened to this order yet')
            : steps.map((step, index) => createElement('li', {
                key: index,
                className: 'acme-order-timeline__step' + (index === steps.length - 1 ? ' acme-order-timeline__step--current' : ''),
            }, createElement('strong', null, String(step.status ?? '')), step.at ? createElement('time', null, String(step.at)) : null)));
}

export const acmeCommerceRuntimePackage = {
    manifest: {
        name: 'Acme.Commerce',
        version: '1.0.0',
        kind: 'ComponentLibrary',
        dependencies: [],
        components: ['productCard', 'stockBadge', 'orderTimeline'],
        layouts: [],
        screenTemplates: [],
        dialogTemplates: [],
        themes: [],
        module: '@acme/scene.commerce',
    },
    components: {
        'Acme.Commerce:productCard': ProductCard,
        'Acme.Commerce:stockBadge': StockBadge,
        'Acme.Commerce:orderTimeline': OrderTimeline,
    },
};
